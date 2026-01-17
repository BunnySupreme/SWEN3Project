using log4net;
using log4net.Config;
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
