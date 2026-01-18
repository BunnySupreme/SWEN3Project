using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Paperless.BatchProcessor.DAL.Models
{
    [Table("documents")][PrimaryKey("Id")]
    public class DocumentModel
    {
        #region Constructors
        public DocumentModel()
        {
            Id = Guid.NewGuid();
            Title = string.Empty;
            Summary = string.Empty;
            Tags = string.Empty;
            UploadedAt = DateTimeOffset.UtcNow;
        }
        #endregion

        #region Properties
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("title")]
        public string Title { get; set; } = default!; // default! to suppress nullable warning (default value setting occurs in constructor)
        
        [Required]
        [Column("summary")]
        public string Summary { get; set; } = default!;
        
        [Column("tags")]
        public string Tags { get; set; }
        
        [Required]
        [Column("uploadedat")]
        public DateTimeOffset UploadedAt { get; set; } = default!;
        
        [Required]
        [Column("userid")]
        public Guid UserId { get; set; }
        #endregion

        [Required]
        [Column("accesscount")]
        public int AccessCount { get; set; }

        #region Methods
        public void Update(string title, string summary, string tags)
        {
            Title = title;
            Summary = summary;
            Tags = tags;
        }
        #endregion
    }
}
