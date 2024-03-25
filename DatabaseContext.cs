using NihongoHelperBot.DBContext;
using System.Data.Entity;
 
namespace NihongoHelperBot
{
    class DatabaseContext : DbContext
    {
        public DatabaseContext()
            : base("DbConnection")
        { }

        public DbSet<User> Users { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
    }
}