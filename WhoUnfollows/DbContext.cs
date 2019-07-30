using InstagramApiSharp.Classes.Models;
using Microsoft.EntityFrameworkCore;

namespace WhoUnfollows
{
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Manipulate the posts table
        /// </summary>
        /// <value>The property that allows to access the Posts table</value>

        public DbSet<InstaUserShort> TakipEtmeyenler { get; set; }


        private string DatabasePath { get; set; }

        public ApplicationDbContext()
        {

        }

        public ApplicationDbContext(string databasePath)
        {
            DatabasePath = databasePath;
        }

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

