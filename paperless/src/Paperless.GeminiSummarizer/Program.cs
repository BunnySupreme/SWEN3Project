using Paperless.GeminiSummarizer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<GeminiService>();

var app = builder.Build();


app.UseRouting();
app.MapControllers();

app.Run();
