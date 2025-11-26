using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Paperless.OcrWorker;
using log4net;
using log4net.Config;
using System.Reflection;

var logRepository = LogManager.GetRepository(typeof(Program).Assembly);
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<OcrWorkerService>();
        services.AddSingleton<IOcrEngine, TesseractOcrEngine>();
    })
    .Build()
    .Run();
