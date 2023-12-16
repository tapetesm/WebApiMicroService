using Microsoft.EntityFrameworkCore;

namespace WebApiMicroService
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<AcademicSubject> AcademicSubjects { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=appdb;Username=pguser;Password=pgpassword;");
            }
        }
    }
}
