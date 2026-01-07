using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Paperless.DAL.Models
{
    [Table("users")]
    [PrimaryKey(nameof(Id))]
    [Index(nameof(Username), IsUnique = true)]
    public class UserModel
    {
        #region Constructors
        public UserModel()
        {
            Id = Guid.NewGuid();
            Username = string.Empty;
            PasswordHash = string.Empty;
            CreatedAt = DateTimeOffset.UtcNow;
        }
        #endregion

        #region Properties
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(64)]
        [Column("username")]
        public string Username { get; set; } = default!;

        [Required]
        [MaxLength(512)]
        [Column("passwordhash")]
        public string PasswordHash { get; set; } = default!;

        [Required]
        [Column("createdat")]
        public DateTimeOffset CreatedAt { get; set; } = default!;
        #endregion
    }
}
