using fork_back.Models;
using Microsoft.EntityFrameworkCore;

namespace fork_back.DataContext
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; } = null!;

        public DbSet<Project> Projects { get; set; } = null!;

        public DbSet<Epic> Epics { get; set; } = null!;

        public DbSet<Ticket> Tickets { get; set; } = null!;

        public DatabaseContext(DbContextOptions options)
          : base(options)
        {
        }
    }
}
