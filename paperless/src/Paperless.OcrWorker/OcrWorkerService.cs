using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Paperless.OcrWorker
{
    public sealed class OcrWorkerService : BackgroundService
    {
        #region Fields

        private readonly ILogger<OcrWorkerService> _logger;

        private IConnection? _connection;
        private IChannel? _channel;
        private BasicProperties? _resultProperties;

        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string _inputQueue;
        private readonly string _resultQueue;

        private bool _initialized;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        #endregion

        #region Ctor

        public OcrWorkerService(ILogger<OcrWorkerService> logger)
        {
            _logger = logger;

            _host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "paperless-rabbitmq";

            var portStr = Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672";
            _port = int.TryParse(portStr, out var p) ? p : 5672;

            _username = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "paperless";
            _password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "paperless";

            _inputQueue = Environment.GetEnvironmentVariable("RABBITMQ_INPUTQUEUE") ?? "paperless.ocr.input";
            _resultQueue = Environment.GetEnvironmentVariable("RABBITMQ_RESULTSQUEUE") ?? "paperless.ocr.results";
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

                _logger.LogInformation("Initializing OCR worker RabbitMQ connection...");

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

                _logger.LogInformation("OCR worker RabbitMQ initialization complete.");
            }
            finally
            {
                _initLock.Release();
            }
        }

        #endregion

        #region Execute

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting OCR worker...");

            const int maxRetries = 10;
            var retryCount = 0;

            while (!stoppingToken.IsCancellationRequested && retryCount < maxRetries)
            {
                try
                {
                    _logger.LogInformation(
                        "OCR worker attempting to connect to RabbitMQ (attempt {Attempt}/{MaxAttempts})",
                        retryCount + 1, maxRetries);

                    await InitAsync();

                    _logger.LogInformation("OCR worker connected to RabbitMQ.");
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning(ex,
                        "OCR worker failed to connect to RabbitMQ (attempt {Attempt}/{MaxAttempts}).",
                        retryCount, maxRetries);

                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError("OCR worker max retry attempts reached. Failing startup.");
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            if (_channel == null)
            {
                _logger.LogError("OCR worker channel not initialized.");
                throw new InvalidOperationException("OCR worker is not initialized.");
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            _logger.LogInformation(
                "OCR worker listening on input queue {InputQueue}, publishing to {ResultQueue}.",
                _inputQueue, _resultQueue);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    Guid docId = Guid.Empty;
                    string? title = null;

                    try
                    {
                        using var doc = JsonDocument.Parse(message);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("DocumentId", out var idProp))
                        {
                            if (idProp.ValueKind == JsonValueKind.String &&
                                Guid.TryParse(idProp.GetString(), out var parsed))
                            {
                                docId = parsed;
                            }
                            else if (idProp.ValueKind == JsonValueKind.Undefined ||
                                     idProp.ValueKind == JsonValueKind.Null)
                            {
                            }
                        }

                        if (root.TryGetProperty("DocumentTitle", out var titleProp))
                        {
                            title = titleProp.GetString();
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex,
                            "OCR worker received non-JSON message: {Message}", message);
                    }

                    var summary = docId != Guid.Empty
                        ? $"[FAKE OCR] Document {docId} ('{title ?? "(no title)"}') processed. This is a simulated OCR result."
                        : $"[FAKE OCR] Received message: {message}";

                    _logger.LogInformation("OCR result created for document {DocumentId}", docId);

                    var resultPayload = new
                    {
                        DocumentId = docId == Guid.Empty ? Guid.NewGuid() : docId,
                        Summary = summary,
                        ProcessedAt = DateTimeOffset.UtcNow
                    };

                    if (_resultProperties == null)
                    {
                        _logger.LogError("Result message properties not initialized.");
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
                        cancellationToken: stoppingToken);

                    _logger.LogInformation(
                        "OCR worker published fake OCR result for document {DocumentId} to {ResultQueue}.",
                        resultPayload.DocumentId, _resultQueue);

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OCR worker error while processing message.");

                    if (_channel != null)
                    {
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                }
            };

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            await _channel.BasicConsumeAsync(
                queue: _inputQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("OCR worker stopping due to cancellation.");
            }
        }

        #endregion

        #region Stop / Dispose

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping OCR worker...");

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

            _logger.LogInformation("OCR worker stopped.");
        }

        #endregion
    }
}
