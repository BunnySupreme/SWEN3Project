using Microsoft.EntityFrameworkCore;
using paperless.DAL.Models;

namespace paperless.DAL
{
    public class DataContext : DbContext
    {
        #region Constructors
        public DataContext(DbContextOptions<DataContext> options) : base(options) { } // Allows Dependency Injection in Program.cs
        #endregion

        #region DbSets
        public DbSet<Document> Documents { get; set; }
        #endregion

        #region Builders
        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseNpgsql(Configuration.PostgresConnectionString);
        //    base.OnConfiguring(optionsBuilder);
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        #endregion
    }
}
