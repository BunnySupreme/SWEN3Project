using Paperless.DAL.Models;

namespace Paperless.Services
{
    public interface IRabbitProducerService
    {
        Task RunAsync(MessageModel message, CancellationToken ct = default);
    }
}