using System.Reflection.Metadata;
using RabbitMQ.Client;

namespace paperless.Services
{
    public sealed class RabbitProducerService
    {
        #region Constructors
        public RabbitProducerService(string host, int port, string username, string password, string queue)
        {
            Host = host;
            Port = port;
            Username = username;
            Password = password;
            Queue = queue;
        }
        #endregion

        #region Fields
        private IConnection? _connection;
        private IChannel? _channel;
        private BasicProperties? _properties;
        #endregion

        #region Properties
        private string Host { get; }
        private int Port { get; }
        private string Username { get; }
        private string Password { get; }
        private string Queue { get; }
        #endregion

        #region Methods
        public async Task InitAsync()
        {
            var factory = new ConnectionFactory()
            {
                HostName = Host,
                Port = Port,
                UserName = Username,
                Password = Password
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: Queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _properties = new BasicProperties
            {
                ContentType = "application/json",
                Persistent = true
            };
        }

        public async Task RunAsync(CancellationToken ct, Document doc)
        {
            if (_channel == null || _properties == null)
            {
                throw new InvalidOperationException("RabbitProducerService is not initialized. Call InitAsync() before RunAsync().");
            }

            while (!ct.IsCancellationRequested)
            {
                // Parse document to JSON
                // Send to queue
            }
        }

        public async Task CloseAsync()
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
        }
        #endregion
    }
}
