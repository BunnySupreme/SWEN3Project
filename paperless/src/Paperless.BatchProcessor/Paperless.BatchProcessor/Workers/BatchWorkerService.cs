using log4net;
using Paperless.BatchProcessor.Services;

namespace Paperless.BatchProcessor.Workers
{
    public class BatchWorkerService : BackgroundService
    {
        #region Fields
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILog _logger;
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

                    // Define folders
                    var baseDir = AppContext.BaseDirectory;
                    var inputDir = Path.Combine(baseDir, "input");
                    var archiveDir = Path.Combine(baseDir, "archive");
                    var errorDir = Path.Combine(baseDir, "error");
                    var filePattern = "*.xml";

                    // Process files
                    await xmlProcessorService.RunOnceAsync(inputDir, archiveDir, errorDir, filePattern);
                }

                // Wait until next batch processing
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.Info($"Batch Worker execution stopped at: {DateTimeOffset.Now}");
        }
        #endregion
    }
}
