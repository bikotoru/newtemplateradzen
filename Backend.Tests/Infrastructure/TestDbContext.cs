using Microsoft.EntityFrameworkCore;
using Backend.Utils.Data;
using Shared.Models.Entities;

namespace Backend.Tests.Infrastructure
{
    public class TestDbContext : AppDbContext
    {
        public TestDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configuraciones específicas para testing si son necesarias
        }
    }
}