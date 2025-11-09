namespace Paperless.DAL.Models
{
    public class MessageModel
    {
        public Guid DocumentId { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public DateTimeOffset QueuedAt { get; set; }
    }
}
