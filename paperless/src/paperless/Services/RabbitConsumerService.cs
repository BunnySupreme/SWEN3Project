using System.Text;
using System.Text.Json;
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
        }
        #endregion

        #region Methods
        private async Task InitAsync()
        {
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
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            await InitAsync();

            if (_channel == null)
            {
                throw new InvalidOperationException("RabbitConsumerService is not initialized.");
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (sender, eventArgs) =>
            {
                try
                {
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
                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
                }
            };

            await _channel.BasicQosAsync(0, 1, false);
            await _channel.BasicConsumeAsync(
                queue: _queue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: ct);

            await Task.Delay(Timeout.Infinite, ct);
        }

        private async Task ProcessSummaryAsync(ResultModel result)
        {
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
                    summary: document.Summary,
                    tags: document.Tags);

                await documentRepo.CreateOrUpdateAsync(document);
                await db.SaveChangesAsync();
            }
        }

        public override async Task StopAsync(CancellationToken ct)
        {
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
        }
        #endregion
    }
}
