using log4net;
using Paperless.BatchProcessor.Services;

namespace Paperless.BatchProcessor.Workers
{
    public class BatchWorkerService : BackgroundService
    {
        #region Fields
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILog _logger;
        private readonly string _accessLogInputDir = Environment.GetEnvironmentVariable("ACCESSLOG_INPUTDIR") ?? "/app/xml_data/input";
        private readonly string _accessLogArchiveDir = Environment.GetEnvironmentVariable("ACCESSLOG_ARCHIVEDIR") ?? "/app/xml_data/archive";
        private readonly string _accessLogErrorDir = Environment.GetEnvironmentVariable("ACCESSLOG_ERRORDIR") ?? "/app/xml_data/error";
        private readonly string _filePattern = "*.xml";
        #endregion

        #region Constructors
        public BatchWorkerService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _logger = LogManager.GetLogger(typeof(BatchWorkerService));
        }
        #endregion

        #region Methods
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Info($"Batch Worker executed at: {DateTimeOffset.Now}");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Batch Worker started
                _logger.Info($"Checking for new files at: {DateTimeOffset.Now}");

                using (var scope = _scopeFactory.CreateScope())
                {
                    // Scope XML Processor Service
                    var xmlProcessorService = scope.ServiceProvider.GetRequiredService<IXmlProcessorService>();

                    // Process files
                    await xmlProcessorService.RunOnceAsync(_accessLogInputDir, _accessLogArchiveDir, _accessLogErrorDir, _filePattern);
                }

                // Wait until next batch processing
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.Info($"Batch Worker execution stopped at: {DateTimeOffset.Now}");
        }
        #endregion
    }
}
