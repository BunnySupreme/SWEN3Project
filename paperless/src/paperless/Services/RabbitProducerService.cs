using System.Text;
using System.Text.Json;
using Paperless.DAL.Models;
using RabbitMQ.Client;

namespace Paperless.Services
{
    public sealed class RabbitProducerService : IRabbitProducerService, IAsyncDisposable
    {
        #region Fields
        private IConnection? _connection;
        private IChannel? _channel;
        private BasicProperties? _properties;
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string _queue;
        private bool _initialized;
        private readonly SemaphoreSlim _initLock = new(1,1); // For thread-safe initialization, only one thread can initialize at a time, prevents race conditions
        #endregion

        #region Constructors
        public RabbitProducerService(string host, int port, string username, string password, string queue)
        {
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
            if (_initialized) return; // Prevents unnecessary locking if already initialized

            await _initLock.WaitAsync();
            try
            {
                if (_initialized) return; // Safety net

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

                _properties = new BasicProperties
                {
                    ContentType = "application/json",
                    Persistent = true
                };

                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task RunAsync(MessageModel message, CancellationToken ct = default)
        {
            await InitAsync();

            if (_channel == null || _properties == null)
            {
                throw new InvalidOperationException("RabbitProducerService is not initialized.");
            }

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _queue,
                mandatory: false,
                basicProperties: _properties,
                body: body,
                cancellationToken: ct);
        }

        public async ValueTask DisposeAsync()
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

            _initLock.Dispose();
        }
        #endregion
    }
}
