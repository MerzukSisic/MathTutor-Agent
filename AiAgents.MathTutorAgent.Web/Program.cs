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
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddRadzenComponents();

builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

// ========== HTTP CLIENT (for Blazor Server API calls) ==========
builder.Services.AddHttpClient();
builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HttpClient
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };
});

// ========== DATABASE ==========
builder.Services.AddDbContext<MathTutorDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(60);
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
builder.Services.AddScoped<KnowledgeTracingService>();
builder.Services.AddScoped<RevisionService>();
builder.Services.AddScoped<ExplanationService>();
builder.Services.AddScoped<ImageIngestionService>();
builder.Services.AddScoped<StudentProfileService>();
builder.Services.AddScoped<PdfExportService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<MlTrainingService>();

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
        Log.Warning("⚠️ ML models not found - using fallback predictions. Train models via /api/admin/train-ml-models");
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
Log.Information("📍 Dashboard:    https://localhost:7152/");
Log.Information("📍 Admin Panel:  https://localhost:7152/admin");
Log.Information("📍 API Docs:     https://localhost:7152/swagger (if enabled)");
Log.Information("📍 SignalR Hub:  https://localhost:7152/agenthub");
Log.Information("═══════════════════════════════════════════");
Log.Information("💡 Train ML Models: POST /api/admin/train-ml-models");
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