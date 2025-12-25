using AiAgents.MathTutorAgent.Application.Runners;
using AiAgents.MathTutorAgent.Application.Services;
using AiAgents.MathTutorAgent.Application.Validators;
using AiAgents.MathTutorAgent.Infrastructure;
using AiAgents.MathTutorAgent.ML.Implementations;
using AiAgents.MathTutorAgent.ML.Interfaces;
using AiAgents.MathTutorAgent.Web.BackgroundServices;
using AiAgents.MathTutorAgent.Web.Hubs;
using AiAgents.MathTutorAgent.Web.Middleware;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/mathtutor-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Controllers
builder.Services.AddControllers();

// SignalR
builder.Services.AddSignalR();

// Radzen
builder.Services.AddRadzenComponents();

// Database
builder.Services.AddDbContext<MathTutorDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));

// ML Services
builder.Services.AddSingleton<IEmbeddingService, StubEmbeddingService>();
builder.Services.AddSingleton<IVectorSearch, InMemoryVectorSearch>();
builder.Services.AddSingleton<IImageTextExtractor, StubImageTextExtractor>();

// Application Services
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
// Agent Runner
builder.Services.AddScoped<MathTutoringAgentRunner>();

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<SubmitAnswerValidator>();

// Background Worker
builder.Services.AddHostedService<AgentBackgroundService>();

var app = builder.Build();

// Database seeding
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<MathTutorDbContext>();
        
        Log.Information("Applying database migrations...");
        await context.Database.MigrateAsync();
        
        Log.Information("Seeding database...");
        await DatabaseSeeder.SeedAsync(context);
        
        Log.Information("Database ready!");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error during database initialization");
        throw;
    }
}

// Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Routing
app.MapHub<AgentHub>("/agenthub");
app.MapControllers();

// MUST BE LAST!
app.MapRazorComponents<AiAgents.MathTutorAgent.Web.Components.App>()
    .AddInteractiveServerRenderMode();

Log.Information("🚀 MathTutor AI Agent starting...");
Log.Information("📍 Dashboard: https://localhost:7152/");

app.Run();