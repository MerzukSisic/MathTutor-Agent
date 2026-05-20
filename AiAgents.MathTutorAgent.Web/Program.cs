using AiAgents.MathTutorAgent.Application.Runners;
using AiAgents.MathTutorAgent.Application.Services;
using AiAgents.MathTutorAgent.Application.Validators;
using AiAgents.MathTutorAgent.Infrastructure;
using AiAgents.MathTutorAgent.ML.Implementations;
using AiAgents.MathTutorAgent.ML.Interfaces;
using AiAgents.MathTutorAgent.ML.Services;
using AiAgents.MathTutorAgent.Web.BackgroundServices;
using AiAgents.MathTutorAgent.Web.Hubs;
using AiAgents.MathTutorAgent.Web.Middleware;
using AiAgents.MathTutorAgent.Web.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Radzen;
using Serilog;
using Serilog.Events;
using System.Threading.RateLimiting;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ========== LOGGING ==========
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.File("logs/mathtutor-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

builder.Host.UseSerilog();

// ========== WEB COMPONENTS ==========
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddRadzenComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "MathTutor.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var dataProtectionBuilder = builder.Services
    .AddDataProtection()
    .SetApplicationName("MathTutorAgent");

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH");
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    var resolvedPath = dataProtectionKeysPath.Trim();
    Directory.CreateDirectory(resolvedPath);
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(resolvedPath));
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });

    options.AddFixedWindowLimiter("admin-write", limiter =>
    {
        limiter.PermitLimit = 80;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });

    options.AddFixedWindowLimiter("agent-ops", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});

builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddScoped<UiPreferencesService>();

// ========== HTTP CLIENT (for Blazor Server API calls) ==========
builder.Services.AddHttpClient();
builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var antiforgery = sp.GetRequiredService<IAntiforgery>();
    var client = new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
    var httpContext = httpContextAccessor.HttpContext;

    // Forward the browser's auth cookie so server-side HttpClient calls pass auth checks.
    var cookieHeader = httpContext?.Request.Headers.Cookie.ToString();
    if (!string.IsNullOrEmpty(cookieHeader))
    {
        client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
    }

    // Add antiforgery header for unsafe API verbs (POST/PUT/DELETE) used by server-side components.
    if (httpContext is not null)
    {
        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        if (!string.IsNullOrWhiteSpace(tokens.RequestToken))
        {
            client.DefaultRequestHeaders.Add("RequestVerificationToken", tokens.RequestToken);
        }
    }

    return client;
});

// ========== DATABASE ==========
builder.Services.AddDbContext<MathTutorDbContext>(options =>
{
    var rawPostgresConnectionString = builder.Configuration.GetConnectionString("PostgresConnection")
        ?? throw new InvalidOperationException("PostgreSQL connection string not found. Configure ConnectionStrings:PostgresConnection.");
    var postgresConnectionString = NormalizePostgresConnectionString(rawPostgresConnectionString);

    options.UseNpgsql(postgresConnectionString, pgOptions =>
    {
        pgOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        pgOptions.CommandTimeout(60);
    });

    // Log SQL queries in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ========== ML SERVICES (Singleton - loaded once) ==========
builder.Services.AddSingleton<IEmbeddingService, StubEmbeddingService>();
builder.Services.AddSingleton<IVectorSearch, InMemoryVectorSearch>();
builder.Services.AddSingleton<IImageTextExtractor, StubImageTextExtractor>();
builder.Services.AddSingleton<KnowledgeTracingMlService>();
builder.Services.AddSingleton<TopicClassifierMlService>();
builder.Services.AddSingleton<MlModelTrainer>();

// ========== APPLICATION SERVICES (Scoped - per request) ==========
builder.Services.AddScoped<WorkQueueService>();
builder.Services.AddScoped<CurriculumService>();
builder.Services.AddScoped<AssessmentService>();
builder.Services.AddScoped<QuestionGenerationService>();
builder.Services.AddScoped<QuestionDifficultyAdvisorService>();
builder.Services.AddScoped<QuestionSelectionService>();
builder.Services.AddScoped<QuestionTimeLimitService>();
builder.Services.AddScoped<KnowledgeTracingService>();
builder.Services.AddScoped<RevisionService>();
builder.Services.AddScoped<ExplanationService>();
builder.Services.AddScoped<CrossMathMilestoneService>();
builder.Services.AddScoped<ImageIngestionService>();
builder.Services.AddScoped<StudentProfileService>();
builder.Services.AddScoped<StudentInsightsCalculatorService>();
builder.Services.AddScoped<PdfExportService>();
builder.Services.AddSingleton<MathContentLocalizationService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<MlTrainingDatasetBuilderService>();
builder.Services.AddScoped<MlTrainingService>();
builder.Services.AddScoped<PasswordHashingService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.Configure<AgentBackgroundOptions>(builder.Configuration.GetSection("AgentBackground"));

// ========== AGENT RUNNER ==========
builder.Services.AddScoped<MathTutoringAgentRunner>();

// ========== VALIDATION ==========
builder.Services.AddValidatorsFromAssemblyContaining<SubmitAnswerValidator>();

// ========== BACKGROUND WORKER ==========
builder.Services.AddHostedService<AgentBackgroundService>();

// ========== CORS (optional - if needed for external API calls) ==========
builder.Services.AddCors(options =>
{
    var allowedOrigins = ResolveAllowedCorsOrigins(builder.Configuration);

    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ========== BUILD APP ==========
var app = builder.Build();

// ========== ML MODELS INITIALIZATION ==========
Log.Information("🤖 Loading ML models...");
await using (var scope = app.Services.CreateAsyncScope())
{
    try
    {
        var ktMl = scope.ServiceProvider.GetRequiredService<KnowledgeTracingMlService>();
        var topicMl = scope.ServiceProvider.GetRequiredService<TopicClassifierMlService>();

        await ktMl.LoadModelAsync("MLModels/knowledge-tracing.zip");
        await topicMl.LoadModelAsync("MLModels/topic-classifier.zip");

        Log.Information("✅ ML models loaded successfully");
    }
    catch (FileNotFoundException)
    {
        Log.Warning("⚠️ ML models not found - using fallback predictions. Train models via /api/admin/train_ml_models");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "⚠️ ML models failed to load - using fallback predictions");
    }
}

// ========== DATABASE INITIALIZATION ==========
Log.Information("🗄️ Initializing database...");
await using (var scope = app.Services.CreateAsyncScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<MathTutorDbContext>();

        Log.Information("📦 Applying database migrations...");
        await context.Database.MigrateAsync();

        Log.Information("🌱 Seeding database...");
        await DatabaseSeeder.SeedAsync(context);

        Log.Information("🎲 Generating Bogus synthetic dataset (if enabled)...");
        await BogusTrainingDataSeeder.SeedAsync(context);

        Log.Information("✅ Database ready!");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ Error during database initialization");
        throw;
    }
}

// ========== MIDDLEWARE ==========
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(); // If CORS is needed
app.UseAntiforgery();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ========== ROUTING ==========
app.MapHub<AgentHub>("/agenthub");
app.MapControllers();

// Razor Components - MUST BE LAST!
app.MapRazorComponents<AiAgents.MathTutorAgent.Web.Components.App>()
    .AddInteractiveServerRenderMode();

// ========== START APPLICATION ==========
Log.Information("═══════════════════════════════════════════");
Log.Information("🚀 MathTutor AI Agent Started Successfully!");
Log.Information("═══════════════════════════════════════════");
Log.Information("📍 HTTP profile:  http://localhost:5297/");
Log.Information("📍 HTTPS profile: https://localhost:7152/");
Log.Information("📍 Admin Panel:   /admin");
Log.Information("📍 API Docs:      /swagger (if enabled)");
Log.Information("📍 SignalR Hub:   /agenthub");
Log.Information("═══════════════════════════════════════════");
Log.Information("💡 Train ML Models: POST /api/admin/train_ml_models");
Log.Information("💡 Environment: {Environment}", app.Environment.EnvironmentName);
Log.Information("═══════════════════════════════════════════");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static string NormalizePostgresConnectionString(string rawConnectionString)
{
    var connectionString = rawConnectionString.Trim().Trim('"', '\'');
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:PostgresConnection is empty. Provide a valid PostgreSQL connection string.");
    }

    if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return connectionString;
    }

    if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:PostgresConnection is not a valid postgres:// URL.");
    }

    var userInfoParts = uri.UserInfo.Split(':', 2, StringSplitOptions.None);
    if (userInfoParts.Length != 2 || string.IsNullOrWhiteSpace(userInfoParts[0]))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:PostgresConnection URL is missing username/password.");
    }

    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = uri.AbsolutePath.Trim('/'),
        Username = Uri.UnescapeDataString(userInfoParts[0]),
        Password = Uri.UnescapeDataString(userInfoParts[1]),
        Pooling = true
    };

    if (string.IsNullOrWhiteSpace(builder.Database))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:PostgresConnection URL must include a database name in the path.");
    }

    var query = uri.Query.TrimStart('?');
    foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
        var parts = pair.Split('=', 2, StringSplitOptions.None);
        var key = Uri.UnescapeDataString(parts[0]).Trim().ToLowerInvariant();
        var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]).Trim() : string.Empty;

        if (key is "sslmode" && Enum.TryParse<SslMode>(value, ignoreCase: true, out var sslMode))
        {
            builder.SslMode = sslMode;
        }
        else if (key is "pooling" && bool.TryParse(value, out var pooling))
        {
            builder.Pooling = pooling;
        }
        else if (key is "maxpoolsize" or "maximum pool size" &&
                 int.TryParse(value, out var maxPoolSize) &&
                 maxPoolSize > 0)
        {
            builder.MaxPoolSize = maxPoolSize;
        }
    }

    return builder.ToString();
}

static string[] ResolveAllowedCorsOrigins(IConfiguration configuration)
{
    var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "http://localhost:5297",
        "https://localhost:7152"
    };

    var configuredOrigins = configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    foreach (var item in configuredOrigins)
    {
        AddOriginIfValid(origins, item);
    }

    var rawOrigins = configuration["Cors:AllowedOrigins"];
    if (!string.IsNullOrWhiteSpace(rawOrigins))
    {
        foreach (var item in rawOrigins.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            AddOriginIfValid(origins, item);
        }
    }

    var appBaseUrl = configuration["App:PublicBaseUrl"];
    AddOriginIfValid(origins, appBaseUrl);

    return origins.ToArray();
}

static void AddOriginIfValid(ISet<string> target, string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return;
    }

    if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
    {
        return;
    }

    if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    target.Add($"{uri.Scheme}://{uri.Authority}");
}
