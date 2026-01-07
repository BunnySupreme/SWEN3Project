using Microsoft.EntityFrameworkCore;
using Paperless.DAL.Models;

namespace Paperless.DAL
{
    public class DataContext : DbContext
    {
        #region Constructors
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        #endregion

        #region DbSets
        public DbSet<DocumentModel> Documents { get; set; }
        public DbSet<UserModel> Users => Set<UserModel>();
        public DbSet<SessionModel> Sessions => Set<SessionModel>();
        #endregion

        #region Builders
        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseNpgsql(Configuration.PostgresConnectionString);
        //    base.OnConfiguring(optionsBuilder);
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DocumentModel>()
                .HasOne<UserModel>()
                .WithMany()
                .HasForeignKey(document => document.UserId) // FK: UserId
                .OnDelete(DeleteBehavior.Cascade);          // Cascade: Deleting a User will delete all its Documents
            modelBuilder.Entity<SessionModel>()
                .HasOne<UserModel>()
                .WithMany()
                .HasForeignKey(session => session.UserId)   // FK: UserId
                .OnDelete(DeleteBehavior.Cascade);          // Cascade: Deleting a User will delete all its Sessions
            base.OnModelCreating(modelBuilder);
        }
        #endregion
    }
}
