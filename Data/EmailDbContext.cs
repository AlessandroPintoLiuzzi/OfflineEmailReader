using Microsoft.EntityFrameworkCore;
using OfflineEmailManager.Model;

namespace OfflineEmailManager.Data;

// This class manages the connection and interaction with the SQLite database.
public class EmailDbContext : DbContext
{
    // This represents the "Emails" table in our database.
    public DbSet<Email> Emails { get; set; }

    // This method configures the context to use a SQLite database file
    // named "local_emails.db" in the same directory as the application.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=local_emails.db");
    }
}