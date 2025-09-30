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
            modelBuilder.Entity<Project>(entity =>
            {
                entity.Property(e => e.IsCompleted).HasConversion<int>();
            });
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Role).HasConversion<string>();
            });
            modelBuilder.Entity<UserProjects>(entity =>
            {
                entity.HasKey(up => new { up.ProjectId, up.UserId });
                entity.HasOne(up => up.User).WithMany(up => up.UserProjects).HasForeignKey(up => up.UserId);
                entity.HasOne(up => up.Project).WithMany(up => up.UserProjects).HasForeignKey(up => up.ProjectId);
            });
                
            modelBuilder.Entity<UserFloss>(entity =>
            {
                entity.HasKey(uf => new { uf.FlossId, uf.UserId });
                entity.HasOne(uf => uf.User).WithMany(uf => uf.UserFloss).HasForeignKey(uf => uf.UserId);
                entity.HasOne(uf => uf.Floss).WithMany(uf => uf.UserFloss).HasForeignKey(uf => uf.FlossId);
            });
            modelBuilder.Entity<ProjectFloss>(entity =>
            {
                entity.HasKey(pf => new { pf.ProjectId, pf.FlossId });
                entity.HasOne(pf => pf.Project).WithMany(pf => pf.ProjectFloss).HasForeignKey(pf => pf.ProjectId);
                entity.HasOne(pf => pf.Floss).WithMany(pf => pf.ProjectFloss).HasForeignKey(pf => pf.FlossId);
            });
            base.OnModelCreating(modelBuilder);
        }
    }
}
