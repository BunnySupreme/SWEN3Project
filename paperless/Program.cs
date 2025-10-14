using Microsoft.EntityFrameworkCore;
using paperless.DAL;
using Paperless.Services;

// Build the app
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<DataContext>(options => { options.UseNpgsql(Configuration.PostgresConnectionString); });
builder.Services.AddSingleton<IDocumentService, DocumentService>();
var app = builder.Build();

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
        // Add logging here
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
