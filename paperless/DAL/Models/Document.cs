using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace paperless.DAL.Models
{
    public class Document
    {
        #region Constructors
        public Document(string title, string content, string summary, List<string> tags)
        {
            Id = Guid.NewGuid();
            Title = title;
            Content = content;
            Summary = summary;
            Tags = string.Join(",", tags);
            CreationDate = DateTime.UtcNow;
        }
        #endregion

        #region Properties
        [Key][Column("id")]
        public Guid Id { get; set; }
        [Column("title")]
        public string Title { get; set; }
        [Column("content")]
        public string Content { get; set; }
        [Column("summary")]
        public string Summary { get; set; }
        [Column("tags")]
        public string Tags { get; set; }
        [Column("creationdate")]
        public DateTime CreationDate { get; set; }
        #endregion
    }
}
