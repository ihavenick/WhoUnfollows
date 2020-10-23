using InstagramApiSharp.Classes.Models;
using Microsoft.EntityFrameworkCore;

namespace WhoUnfollows
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(string databasePath)
        {
            DatabasePath = databasePath;
        }

        public DbSet<InstaUserShort> TakipEtmeyenler { get; set; }


        private string DatabasePath { get; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Filename={DatabasePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InstaUserShort>()
                .HasKey(o => o.Pk);
        }
    }
}