using FluentValidation;
using FluentValidation.AspNetCore;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using paperless.Api;
using Paperless.DAL;
using Paperless.DAL.Repositories;
using Paperless.Services;
using System.Text.Json;

// ------------------------------------------------------------
// CONFIGURE LOG4NET & CREATE LOGGER
// ------------------------------------------------------------

XmlConfigurator.Configure(new FileInfo("log4net.config"));
var programLogger = LogManager.GetLogger(typeof(Program));

// ------------------------------------------------------------
// BUILD THE APP
// ------------------------------------------------------------

programLogger.Info("=== Paperless Application Building ===");

// Builder
var builder = WebApplication.CreateBuilder(args);

// AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
}, typeof(Program).Assembly);

// Controller
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

// Validatiors
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

// OpenAPI / Swagger
builder.Services.AddOpenApi();

// DB Configuration
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseNpgsql(Configuration.PostgresConnectionString);
});

// RabbitMQ Services
string rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "paperless-rabbitmq";
int rabbitPort = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672");
string rabbitUsername = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
string rabbitPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
string rabbitInputQueue = Environment.GetEnvironmentVariable("RABBITMQ_INPUTQUEUE") ?? "paperless.ocr.input";
string rabbitResultsQueue = Environment.GetEnvironmentVariable("RABBITMQ_RESULTSQUEUE") ?? "paperless.ocr.results";
builder.Services.AddSingleton<IRabbitProducerService>(sp =>
{
    return new RabbitProducerService(
        host: rabbitHost,
        port: rabbitPort,
        username: rabbitUsername,
        password: rabbitPassword,
        queue: rabbitInputQueue);
});
builder.Services.AddHostedService<RabbitConsumerService>(sp =>
{
    return new RabbitConsumerService(
        sp,
        host: rabbitHost,
        port: rabbitPort,
        username: rabbitUsername,
        password: rabbitPassword,
        queue: rabbitResultsQueue);
});

// Document Service
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Document Repository
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();

// Build
var app = builder.Build();

// ------------------------------------------------------------
// EXCEPTION HANDLING MIDDLEWARE (FOR UNHANDLED EXCEPTIONS)
// ------------------------------------------------------------

programLogger.Info("=== Exception Handler Middleware Starting ===");
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context => // Executes when any exception bubbles up to this middleware
    {
        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>(); // Caught exception is stored in Features collection, IExceptionHandlerFeature gives access to it and where it occurred and which endpoint was executing
        var exception = exceptionHandlerFeature?.Error; // Get the actual exception object

        var errorAppLogger = LogManager.GetLogger(typeof(Program)); // Create separate logger for middleware
        errorAppLogger.Error($"Unhandled exception on {context.Request.Method} {context.Request.Path}", exception); // Log unhandled exception

        context.Response.StatusCode = 500; // Internal Server Error
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new // Send error response
        {
            Error = "An unexpected error occurred."
        });
    });
});

// ------------------------------------------------------------
// APPLY DATABASE MIGRATIONS
// ------------------------------------------------------------

programLogger.Info("=== Database Migration Applying ===");
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        programLogger.Info("Applying database migrations...");
        var dataContext = services.GetRequiredService<DataContext>();
        dataContext.Database.Migrate();
        programLogger.Info("Database migrations completed successfully.");
    }
    catch (Exception ex)
    {
        programLogger.Fatal("Could not apply migrations", ex);
        throw;
    }
}

// ------------------------------------------------------------
// CONFIGURE THE HTTP REQUEST PIPELINE
// ------------------------------------------------------------

programLogger.Info("=== HTTP Request Pipeline Configuring ===");
app.MapOpenApi();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/openapi/v1.json", "Paperless v1 API");
});

// ------------------------------------------------------------
// MAP CONTROLLERS
// ------------------------------------------------------------

programLogger.Info("=== Controllers Mapping ===");
app.MapControllers();

// ------------------------------------------------------------
// RUN THE APP
// ------------------------------------------------------------

programLogger.Info("=== Paperless Application Running ===");
app.Run();