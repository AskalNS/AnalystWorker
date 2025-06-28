using ClientService.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace ClientService.EF
{
    public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)

        {

        }

        public DbSet<Code> Codes { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<HalykCredential> HalykCredentials { get; set; }

        public DbSet<IntegratedUsers> IntegratedUsers { get; set; }

        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<ProductPoint> ProductPoints { get; set; }
        public DbSet<AdditionalUserInfo> AdditionalUserInfo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductPoint>()
                .HasIndex(p => new { p.MerchantProductCode, p.CityCode, p.PointCode })
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }


    }

}
