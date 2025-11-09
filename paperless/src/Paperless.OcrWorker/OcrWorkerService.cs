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
    public class OcrWorkerService : BackgroundService
    {
        private readonly ILogger<OcrWorkerService> _logger;
        private IConnection? _connection;
        private IModel? _channel;

        public OcrWorkerService(ILogger<OcrWorkerService> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "paperless-rabbitmq";
            var portStr = Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672";
            var user = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "paperless";
            var password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "paperless";
            var queue = Environment.GetEnvironmentVariable("RABBITMQ_INPUTQUEUE") ?? "paperless.ocr.input";

            var port = int.TryParse(portStr, out var p) ? p : 5672;

            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = user,
                Password = password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                string fakeSummary;
                try
                {
                    using var doc = JsonDocument.Parse(message);
                    var id = doc.RootElement.GetProperty("DocumentId").GetGuid();
                    var title = doc.RootElement.TryGetProperty("DocumentTitle", out var t)
                        ? t.GetString()
                        : "(no title)";

                    fakeSummary = $"[FAKE OCR] Document {id} ('{title}') processed. This is a simulated OCR result.";
                }
                catch
                {
                    fakeSummary = $"[FAKE OCR] Received message: {message}";
                }

                _logger.LogInformation(fakeSummary);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            };

            _channel.BasicConsume(
                queue: queue,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("OCR worker started. Listening on queue {Queue}", queue);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
