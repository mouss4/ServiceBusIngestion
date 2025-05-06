using CustomerJob.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerJob.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<CustomerBrand> CustomerBrands { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>().HasKey(c => c.CustomerId);
            modelBuilder.Entity<Brand>().HasKey(b => b.BrandId);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.CustomerId)
                .IsUnique();

            modelBuilder.Entity<Brand>()
                .HasIndex(b => b.BrandId)
                .IsUnique();

            modelBuilder.Entity<CustomerBrand>()
                .HasKey(cb => new { cb.CustomerId, cb.BrandId });

            modelBuilder.Entity<CustomerBrand>()
                .HasOne(cb => cb.Customer)
                .WithMany(c => c.CustomerBrands)
                .HasForeignKey(cb => cb.CustomerId);

            modelBuilder.Entity<CustomerBrand>()
                .HasOne(cb => cb.Brand)
                .WithMany(b => b.CustomerBrands)
                .HasForeignKey(cb => cb.BrandId);
        }
    }
}
