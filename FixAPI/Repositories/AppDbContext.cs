using FixAPI.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace FixAPI.Repositories
{
    public class AppDbContext : DbContext
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<ExecutionReport> ExecutionReports { get; internal set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
        }
    }
}
