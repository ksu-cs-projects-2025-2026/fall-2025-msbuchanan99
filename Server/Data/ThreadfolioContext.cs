using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Server.Models;
using System.IO;

namespace Server.Data
{
    public class ThreadfolioContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Floss> Floss { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectFloss> ProjectFloss { get; set; }
        public DbSet<UserFloss> UserFloss { get; set; }
        public DbSet<UserProjects> UserProjects { get; set; }


        public ThreadfolioContext(DbContextOptions<ThreadfolioContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>().Property(e => e.IsCompleted).HasConversion<int>();
        }
    }
}
