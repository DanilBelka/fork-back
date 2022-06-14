using fork_back.Models;
using Microsoft.EntityFrameworkCore;

namespace fork_back.DataContext
{
    public class MySqlDatabaseContext : DatabaseContext
    {
        string ConnectionString { get; init; }

        public MySqlDatabaseContext(DbContextOptions<MySqlDatabaseContext> options, IConfiguration config)
            : base(options)
        {
            ConnectionString = config.GetConnectionString("MySqlConnection");

            Database.EnsureCreated();
        }

        public override void RecreateDatabase()
        {
            Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>().HasData(
                    new Account { Id = 1, Login = "admin@admin.com", FirstName = "Administrator" }
            );
        }
    }
}
