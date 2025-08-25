using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Server.Models;
using System.IO;

namespace Server.Migrations
{
    public class ThreadfolioContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Floss> Floss { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Material> Materials { get; set; }

        public string DbPath { get; }

        public ThreadfolioContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = Path.Join(path, "threadfolio.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>().Property(e => e.Completed).HasConversion<int>();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }
}
