namespace Paperless.DAL.Models
{
    public class ResultModel
    {
        public Guid DocumentId { get; set; }
        public string Summary { get; set; } = string.Empty;
        public DateTimeOffset ProcessedAt { get; set; }
    }
}
