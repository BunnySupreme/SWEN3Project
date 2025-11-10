using System.Text;
using System.Text.Json;
using log4net;
using Paperless.DAL;
using Paperless.DAL.Models;
using Paperless.DAL.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Paperless.Services
{
    public sealed class RabbitConsumerService : BackgroundService
    {
        #region Fields
        private readonly IServiceProvider _serviceProvider;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string _queue;
        private readonly ILog _logger;
        #endregion

        #region Constructors
        public RabbitConsumerService(IServiceProvider serviceProvider, string host, int port, string username, string password, string queue)
        {
            _serviceProvider = serviceProvider;
            _host = host;
            _port = port;
            _username = username;
            _password = password;
            _queue = queue;
            _logger = LogManager.GetLogger(typeof(RabbitConsumerService));
        }
        #endregion

        #region Methods
        private async Task InitAsync()
        {
            _logger.Info("Initializing RabbitConsumerService...");

            var factory = new ConnectionFactory()
            {
                HostName = _host,
                Port = _port,
                UserName = _username,
                Password = _password
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: _queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.Info("RabbitConsumerService initialized.");
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.Info("Starting RabbitConsumerService...");

            // Retry connection maxRetries times
            int retryCount = 0;
            int maxRetries = 10;

            while(!ct.IsCancellationRequested && retryCount < maxRetries)
            {
                try
                {
                    _logger.Info($"Attempting to connect to RabbitMQ (Attempt {retryCount + 1}/{maxRetries})");
                    await InitAsync();
                    _logger.Info("Connected to RabbitMQ successfully.");
                    break; // Exit loop on successful connection
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.Warn($"Failed to connect to RabbitMQ (Attempt {retryCount}/{maxRetries}).", ex);
                    if (retryCount >= maxRetries)
                    {
                        _logger.Error("Max retry attempts reached. Unable to connect to RabbitMQ.");
                        throw;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(5), ct); // Wait before retrying
                }
            }

            if (_channel == null)
            {
                _logger.Error("RabbitConsumerService is not initialized.");
                throw new InvalidOperationException("RabbitConsumerService is not initialized.");
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            _logger.Info("Waiting for messages...");

            consumer.ReceivedAsync += async (sender, eventArgs) =>
            {
                try
                {
                    _logger.Info("Message received from RabbitMQ.");

                    var body = eventArgs.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var result = JsonSerializer.Deserialize<ResultModel>(json);

                    if (result != null)
                    {
                        await ProcessSummaryAsync(result);
                        await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error processing message.", ex);
                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
                }
            };

            await _channel.BasicQosAsync(0, 1, false);
            await _channel.BasicConsumeAsync(
                queue: _queue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: ct);

            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (TaskCanceledException)
            {
                _logger.Info("Consumer stopping...");
            }
        }

        private async Task ProcessSummaryAsync(ResultModel result)
        {
            _logger.Info($"Processing summary for Document ID: {result.DocumentId}");

            // New scope and required services
            using var scope = _serviceProvider.CreateScope();
            var documentRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            // Update document summary in database
            var document = await documentRepo.ReadByIdAsync(result.DocumentId);
            if (document != null)
            {
                document.Update(
                    title: document.Title,
                    summary: result.Summary, // Only summary needs an update
                    tags: document.Tags);

                await documentRepo.CreateOrUpdateAsync(document);
                await db.SaveChangesAsync();
                _logger.Info($"Document with ID: {result.DocumentId} updated successfully.");
            }
            else
            {
                _logger.Warn($"Document with ID: {result.DocumentId} not found.");
            }
        }

        public override async Task StopAsync(CancellationToken ct)
        {
            _logger.Info("Stopping RabbitConsumerService...");

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

            await base.StopAsync(ct);

            _logger.Info("RabbitConsumerService stopped.");
        }
        #endregion
    }
}
