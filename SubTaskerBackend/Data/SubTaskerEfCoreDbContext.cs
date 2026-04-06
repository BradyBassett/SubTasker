using Microsoft.EntityFrameworkCore;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Data
{
    public class SubTaskerEfCoreDbContext : DbContext
    {
        public SubTaskerEfCoreDbContext(DbContextOptions<SubTaskerEfCoreDbContext> options) : base(options)
        {
        }

        public DbSet<TaskItems> TaskItems { get; set; } = null!;
        public DbSet<Categories> Categories { get; set; } = null!;
        public DbSet<Tags> Tags { get; set; } = null!;
        public DbSet<Users> Users { get; set; } = null!;
    }
}