using log4net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using paperless.DAL;
using paperless.DAL.Repositories;
using Paperless.Services;

// Build the app
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<DataContext>(options => { options.UseNpgsql(Configuration.PostgresConnectionString); });
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
var app = builder.Build();

// Exception handling middleware (for unhandled exceptions)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context => // Executes when any exception bubbles up to this middleware
    {
        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>(); // Caught exception is stored in Features collection, IExceptionHandlerFeature gives access to it and where it occurred and which endpoint was executing
        var exception = exceptionHandlerFeature?.Error; // Get the actual exception object

        var logger = LogManager.GetLogger(typeof(Program)); // Create logger for this class
        logger.Error($"Unhandled exception on {context.Request.Method} {context.Request.Path}", exception); // Log unhandled exception

        context.Response.StatusCode = 500; // Internal Server Error
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new // Send error response
        {
            Error = "An unexpected error occurred."
        });
    });
});

// Apply database migrations automatically
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dataContext = services.GetRequiredService<DataContext>();
        dataContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        throw new Exception("ERROR - Could not apply migrations:", ex);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "Paperless v1 API");
    });
}

// Map controllers
app.MapControllers();

// Run the app
app.Run();
