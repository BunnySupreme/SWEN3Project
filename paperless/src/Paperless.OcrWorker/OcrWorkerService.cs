using System.Text;
using System.Text.Json;
using log4net;
using Minio;
using Minio.DataModel.Args;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Paperless.OcrWorker
{
    public sealed class OcrWorkerService : BackgroundService
    {
        #region Fields

        private readonly ILog _logger;

        private IConnection? _connection;
        private IChannel? _channel;
        private BasicProperties? _resultProperties;

        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string _inputQueue;
        private readonly string _resultQueue;

        private readonly OcrJobHandler _jobHandler;
        private readonly IMinioClient _minioClient;
        private readonly IOcrEngine _ocr;

        private bool _initialized;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        #endregion

        #region Ctor
        public OcrWorkerService(IOcrEngine ocr)
        {
            _logger = LogManager.GetLogger(typeof(OcrWorkerService));

            _host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "paperless-rabbitmq";

            var portStr = Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672";
            _port = int.TryParse(portStr, out var p) ? p : 5672;

            _username = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "paperless";
            _password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "paperless";

            _inputQueue = Environment.GetEnvironmentVariable("RABBITMQ_INPUTQUEUE") ?? "paperless.ocr.input";
            _resultQueue = Environment.GetEnvironmentVariable("RABBITMQ_RESULTSQUEUE") ?? "paperless.ocr.results";

            var minioClient = new MinioClient()
                .WithEndpoint("paperless-minio", 9000)
                .WithCredentials("paperless", Configuration.MinioPassword)
                .WithSSL(false)
                .Build();

            var store = new MinioObjectStore(minioClient);

            _ocr = ocr;
            _minioClient = minioClient;
            _jobHandler = new OcrJobHandler(store, _ocr, _logger);
        }
        #endregion

        #region Init / Connection
        private async Task InitAsync()
        {
            if (_initialized)
                return;

            await _initLock.WaitAsync();
            try
            {
                if (_initialized)
                    return;

                _logger.Info("Initializing OCR worker RabbitMQ connection...");

                var factory = new ConnectionFactory
                {
                    HostName = _host,
                    Port = _port,
                    UserName = _username,
                    Password = _password
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.QueueDeclareAsync(
                    queue: _inputQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                await _channel.QueueDeclareAsync(
                    queue: _resultQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _resultProperties = new BasicProperties
                {
                    ContentType = "application/json",
                    Persistent = true
                };

                _initialized = true;

                _logger.Info("OCR worker RabbitMQ initialization complete.");
            }
            finally
            {
                _initLock.Release();
            }
        }
        #endregion

        #region Execute
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.Info("Starting OCR worker...");

            const int maxRetries = 10;
            var retryCount = 0;

            while (!ct.IsCancellationRequested && retryCount < maxRetries)
            {
                try
                {
                    _logger.Info($"OCR worker attempting to connect to RabbitMQ (attempt {retryCount + 1}/{maxRetries})");

                    await InitAsync();

                    _logger.Info("OCR worker connected to RabbitMQ.");
                    break;
                }
                catch (Exception)
                {
                    retryCount++;
                    _logger.Warn($"OCR worker failed to connect to RabbitMQ (attempt {retryCount + 1}/{maxRetries})");

                    if (retryCount >= maxRetries)
                    {
                        _logger.Error("OCR worker max retry attempts reached. Failing startup.");
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
            }

            if (_channel == null)
            {
                _logger.Error("OCR worker channel not initialized.");
                throw new InvalidOperationException("OCR worker is not initialized.");
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            _logger.Info($"OCR worker listening on input queue {_inputQueue}, publishing to {_resultQueue}.");

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    var (docId, title) = OcrMessageParser.Parse(message, _logger);

                    var (resultDocId, summary) = await _jobHandler.HandleAsync(docId, title, ct);

                    // REMOVE LATER - Transitional code until proper summary generation is implemented - REMOVE LATER //
                    summary = summary.Substring(0, 255);
                    // REMOVE LATER - Transitional code until proper summary generation is implemented - REMOVE LATER //

                    var resultPayload = new
                    {
                        DocumentId = resultDocId == Guid.Empty ? Guid.NewGuid() : resultDocId,
                        Summary = summary,
                        ProcessedAt = DateTimeOffset.UtcNow
                    };

                    if (_resultProperties == null)
                    {
                        _logger.Error("Result message properties not initialized.");
                        throw new InvalidOperationException("Result properties not initialized.");
                    }

                    var resultJson = JsonSerializer.Serialize(resultPayload);
                    var resultBody = Encoding.UTF8.GetBytes(resultJson);

                    await _channel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: _resultQueue,
                        mandatory: false,
                        basicProperties: _resultProperties,
                        body: resultBody,
                        cancellationToken: ct);

                    _logger.Info(
                        $"OCR worker published OCR result for document {resultPayload.DocumentId} to {_resultQueue}: {summary}");

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.Error("OCR worker error while processing message.", ex);
                    if (_channel != null)
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };


            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            await _channel.BasicConsumeAsync(
                queue: _inputQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: ct);

            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (TaskCanceledException)
            {
                _logger.Info("OCR worker stopping due to cancellation.");
            }
        }
        #endregion

        #region Stop / Dispose
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Stopping OCR worker...");

            if (_channel != null)
            {
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
            }

            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }

            _initLock.Dispose();

            await base.StopAsync(cancellationToken);

            _logger.Info("OCR worker stopped.");
        }
        #endregion
    }
}
