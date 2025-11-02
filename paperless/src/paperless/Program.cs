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

// Configure log4net & create logger
XmlConfigurator.Configure(new FileInfo("log4net.config"));
var programLogger = LogManager.GetLogger(typeof(Program));

// Build the app
programLogger.Info("=== Paperless Application Building ===");
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
}, typeof(Program).Assembly);
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseNpgsql(Configuration.PostgresConnectionString);
});
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
var app = builder.Build();

// Exception handling middleware (for unhandled exceptions)
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

// Apply database migrations automatically
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

// Configure the HTTP request pipeline.
programLogger.Info("=== HTTP Request Pipeline Configuring ===");
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "Paperless v1 API");
    });
}

// Map controllers
programLogger.Info("=== Controllers Mapping ===");
app.MapControllers();

// Run the app
programLogger.Info("=== Paperless Application Running ===");
app.Run();