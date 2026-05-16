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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var configuredDbProvider = builder.Configuration["DatabaseProvider"];
var databaseProvider = string.IsNullOrWhiteSpace(configuredDbProvider)
    ? (OperatingSystem.IsWindows() ? "SqlServer" : "Sqlite")
    : configuredDbProvider.Trim();

// ========== LOGGING ==========
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/mathtutor-.log", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
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
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
    var client = new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };

    // Forward the browser's auth cookie so server-side HttpClient calls pass auth checks.
    var cookieHeader = httpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();
    if (!string.IsNullOrEmpty(cookieHeader))
    {
        client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
    }

    return client;
});

// ========== DATABASE ==========
builder.Services.AddDbContext<MathTutorDbContext>(options =>
{
    if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        var sqlServerConnectionString = builder.Configuration.GetConnectionString("SqlServerConnection")
            ?? builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("SQL Server connection string not found. Configure ConnectionStrings:SqlServerConnection or ConnectionStrings:DefaultConnection.");

        options.UseSqlServer(
            sqlServerConnectionString,
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(60);
            });
    }
    else if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        var sqliteConnectionString = builder.Configuration.GetConnectionString("SqliteConnection")
            ?? "Data Source=mathtutor.db";
        options.UseSqlite(sqliteConnectionString);
    }
    else
    {
        throw new InvalidOperationException($"Unsupported DatabaseProvider '{databaseProvider}'. Use 'SqlServer' or 'Sqlite'.");
    }
    
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

// ========== AGENT RUNNER ==========
builder.Services.AddScoped<MathTutoringAgentRunner>();

// ========== VALIDATION ==========
builder.Services.AddValidatorsFromAssemblyContaining<SubmitAnswerValidator>();

// ========== BACKGROUND WORKER ==========
builder.Services.AddHostedService<AgentBackgroundService>();

// ========== CORS (optional - if needed for external API calls) ==========
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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

        if (context.Database.IsSqlite())
        {
            Log.Information("📦 Creating SQLite database schema...");
            await context.Database.EnsureCreatedAsync();
            await context.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "UserAccounts" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_UserAccounts" PRIMARY KEY AUTOINCREMENT,
                    "FullName" TEXT NOT NULL,
                    "Email" TEXT NOT NULL,
                    "PasswordHash" TEXT NOT NULL,
                    "Role" TEXT NOT NULL,
                    "EmailConfirmed" INTEGER NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "StudentId" INTEGER NULL,
                    CONSTRAINT "FK_UserAccounts_Students_StudentId"
                        FOREIGN KEY ("StudentId") REFERENCES "Students" ("Id")
                        ON DELETE SET NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserAccounts_Email" ON "UserAccounts" ("Email");
                CREATE INDEX IF NOT EXISTS "IX_UserAccounts_StudentId" ON "UserAccounts" ("StudentId");
                CREATE TABLE IF NOT EXISTS "AuthTokens" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_AuthTokens" PRIMARY KEY AUTOINCREMENT,
                    "UserAccountId" INTEGER NOT NULL,
                    "Purpose" TEXT NOT NULL,
                    "TokenHash" TEXT NOT NULL,
                    "CreatedAtUtc" TEXT NOT NULL,
                    "ExpiresAtUtc" TEXT NOT NULL,
                    "ConsumedAtUtc" TEXT NULL,
                    CONSTRAINT "FK_AuthTokens_UserAccounts_UserAccountId"
                        FOREIGN KEY ("UserAccountId") REFERENCES "UserAccounts" ("Id")
                        ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AuthTokens_TokenHash" ON "AuthTokens" ("TokenHash");
                CREATE INDEX IF NOT EXISTS "IX_AuthTokens_UserAccountId" ON "AuthTokens" ("UserAccountId");
                CREATE TABLE IF NOT EXISTS "StudentChallengeProgress" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_StudentChallengeProgress" PRIMARY KEY AUTOINCREMENT,
                    "StudentId" INTEGER NOT NULL,
                    "ChallengeKey" TEXT NOT NULL,
                    "CompletedAtUtc" TEXT NOT NULL,
                    CONSTRAINT "FK_StudentChallengeProgress_Students_StudentId"
                        FOREIGN KEY ("StudentId") REFERENCES "Students" ("Id")
                        ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_StudentChallengeProgress_StudentId_ChallengeKey"
                    ON "StudentChallengeProgress" ("StudentId", "ChallengeKey");
                CREATE INDEX IF NOT EXISTS "IX_StudentChallengeProgress_StudentId"
                    ON "StudentChallengeProgress" ("StudentId");
                """);
        }
        else
        {
            Log.Information("📦 Applying database migrations...");
            await context.Database.MigrateAsync();
        }
        
        Log.Information("🌱 Seeding database...");
        await DatabaseSeeder.SeedAsync(context);
        
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
app.UseAntiforgery();
app.UseCors(); // If CORS is needed
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
