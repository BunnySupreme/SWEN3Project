using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Paperless.DAL.Models
{
    [Table("documents")][PrimaryKey("Id")]
    public class DocumentModel
    {
        #region Constructors
        public DocumentModel()
        {
            Id = Guid.NewGuid();
            Title = string.Empty;
            Content = string.Empty;
            Summary = string.Empty;
            Tags = string.Empty;
            UploadedAt = DateTimeOffset.UtcNow;
        }
        #endregion

        #region Properties
        [Key][Column("id")]
        public Guid Id { get; set; }
        [Required][MaxLength(255)][Column("title")]
        public string Title { get; internal set; } = default!; // Internal setters for AutoMapper
        [Required][Column("content")]
        public string Content { get; internal set; } = default!; // default! to suppress nullable warning (default value setting occurs in constructor)
        [Required][Column("summary")]
        public string Summary { get; internal set; } = default!;
        [Column("tags")]
        public string Tags { get; internal set; }
        [Required][Column("uploadedat")] // Rename this column eventually
        public DateTimeOffset UploadedAt { get; internal set; } = default!;
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
