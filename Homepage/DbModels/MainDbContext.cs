using Homepage.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Homepage.DbModels
{
    public class MainDbContext : DbContext
    {   
        public DbSet<DbUser> Users { get; set; }
        public DbSet<DbComment> Comments { get; set; }
        
        public MainDbContext(DbContextOptions<MainDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DbUser>().HasIndex(u => u.FacebookNameId);
        }
    }
}
