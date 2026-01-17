using Microsoft.EntityFrameworkCore;
using Paperless.DAL.Models;

namespace Paperless.BatchProcessor.DAL
{
    public class DataContext : DbContext
    {
        #region Constructors
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        #endregion

        #region DbSets
        public DbSet<DocumentModel> Documents { get; set; }
        #endregion

        #region Builders
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DocumentModel>()
                .ToTable("Documents"); // Use table set up by REST server (which is our single point of truth)
        }
        #endregion
    }
}
