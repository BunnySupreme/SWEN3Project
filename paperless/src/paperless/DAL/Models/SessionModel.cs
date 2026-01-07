using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Paperless.DAL.Models
{
    [Table("sessions")]
    [PrimaryKey(nameof(Id))]
    [Index(nameof(Token), IsUnique = true)]
    public class SessionModel
    {
        #region Constructors
        public SessionModel()
        {
            Id = Guid.NewGuid();
            Token = string.Empty;
            CreatedAt = DateTimeOffset.UtcNow;
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7);
        }
        #endregion

        #region Properties
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("userid")]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(256)]
        [Column("token")]
        public string Token { get; set; } = default!;

        [Required]
        [Column("createdat")]
        public DateTimeOffset CreatedAt { get; set; } = default!;

        [Required]
        [Column("expiresat")]
        public DateTimeOffset ExpiresAt { get; set; } = default!;

        [Column("revokedat")]
        public DateTimeOffset? RevokedAt { get; set; }
        #endregion

        #region Methods
        public void Revoke(DateTimeOffset now) => RevokedAt = now;
        #endregion
    }
}
