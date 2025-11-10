using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Paperless.OcrWorker;
using log4net;
using log4net.Config;
using System.Reflection;

var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<OcrWorkerService>();
    })
    .Build()
    .Run();
