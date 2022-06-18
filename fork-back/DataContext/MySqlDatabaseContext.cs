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
            modelBuilder.Entity<Account>(b =>
            {
                var account = new Account()
                {
                    Id = 1,
                    Login = "admin@admin.com",
                    Role = AccountRole.Administrator,
                    FirstName = "Administrator",
                };
                var accountSeсurity = AccountSeсurity.Build("forkAdmin");

                b.HasData(account);
                b.OwnsOne(a => a.Seсurity)
                 .HasData(new
                 {
                     AccountId = account.Id,
                     Hash = accountSeсurity.Hash,
                     HashType = accountSeсurity.HashType,
                     Salt = accountSeсurity.Salt,
                 });
            });
        }
    }
}
