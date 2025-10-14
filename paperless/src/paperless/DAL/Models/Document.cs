using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace paperless.DAL.Models
{
    [Table("documents")][PrimaryKey("Id")]
    public class Document
    {
        #region Constructors
        public Document()
        {
            Id = Guid.NewGuid();
            Title = string.Empty;
            Content = string.Empty;
            Summary = string.Empty;
            Tags = string.Empty;
            CreationDate = DateTimeOffset.UtcNow;
        }
        #endregion

        #region Properties
        [Key][Column("id")]
        public Guid Id { get; set; }
        [Required][Column("title")]
        public string Title { get; private set; } = default!;
        [Required][MaxLength(255)][Column("content")]
        public string Content { get; private set; } = default!;
        [Required][Column("summary")]
        public string Summary { get; private set; } = default!;
        [Column("tags")]
        public string Tags { get; private set; }
        [Required][Column("creationdate")]
        public DateTimeOffset CreationDate { get; private set; } = default!;
        #endregion

        #region Methods
        public void Update(string title, string content, string summary, string tags)
        {
            Title = title;
            Content = content;
            Summary = summary;
            Tags = tags;
        }
        #endregion
    }
}
