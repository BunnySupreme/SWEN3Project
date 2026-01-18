using log4net;
using log4net.Config;
using Microsoft.EntityFrameworkCore;
using Paperless.BatchProcessor.DAL;
using Paperless.BatchProcessor.DAL.Repositories;
using Paperless.BatchProcessor.Services;
using Paperless.BatchProcessor.Workers;

// ------------------------------------------------------------
// CONFIGURE LOG4NET & CREATE LOGGER
// ------------------------------------------------------------

XmlConfigurator.Configure(new FileInfo("log4net.config"));
var programLogger = LogManager.GetLogger(typeof(Program));

// ------------------------------------------------------------
// BUILD THE APP
// ------------------------------------------------------------

programLogger.Info("=== Batch Processor Application Building ===");

// Builder
var builder = Host.CreateApplicationBuilder(args);

// DB Configuration
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseNpgsql(Configuration.PostgresConnectionString);
});

// Repositories
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

// XML Processor Service
builder.Services.AddScoped<IXmlProcessorService, XmlProcessorService>();

// Batch Worker
builder.Services.AddHostedService<BatchWorkerService>(); // Framework takes care of scopeFactory

// Build
var host = builder.Build();

// ------------------------------------------------------------
// RUN THE APP
// ------------------------------------------------------------

programLogger.Info("=== Batch Processor Application Running ===");

// Run
host.Run();
