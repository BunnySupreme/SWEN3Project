using Paperless.OcrWorker;
using log4net;
using log4net.Config;

var logRepository = LogManager.GetRepository(typeof(Program).Assembly);
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<OcrWorkerService>();
        services.AddSingleton<IOcrEngine, TesseractOcrEngine>();
        services.AddHttpClient<IGeminiSummarizerClient, GeminiSummarizerClient>(client =>
        {
            client.BaseAddress = new Uri("http://paperless-geminisummarizer:8090/api/gemini/summarize");
            client.Timeout = TimeSpan.FromMinutes(2);
        });
    })
    .Build()
    .Run();
