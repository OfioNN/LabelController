using LabelController.Model;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace LabelController.Data
{
    class AppDbContext : DbContext
    {
        public DbSet<ProductLabel> ProductLabels { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            string dbPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Labels.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");

        }

    }
}
