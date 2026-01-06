namespace Paperless.Search.Models
{
    public class DocumentSearchModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OcrText { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public DateTimeOffset UploadedAt { get; set; }
        public string UserId { get; set; }
    }
}
