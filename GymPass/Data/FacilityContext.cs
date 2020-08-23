using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GymPass.Models;
using System.Threading;

namespace GymPass.Data
{
    public class FacilityContext : DbContext
    {
        public FacilityContext(DbContextOptions<FacilityContext> options)
            : base(options)
        {
        }

        public DbSet<Facility> Facilities { get; set; }
        public DbSet<UsersInGymDetail> UsersInGymDetails { get; set; }
        public DbSet<UsersOutOfGymDetails> UsersOutofGymDetails { get;  set; }
        public DbSet<ImageStore> ImageStore { get; set; }
        public DbSet<Error> Errors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Facility>().ToTable("Facility");
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            // When the timer is created it starts at 10 TODO: Use this area for timer if required
            //var AddedEntitiesProd = ChangeTracker.Entries<Facility>().Where(E => E.State == EntityState.Added).ToList();

            //AddedEntitiesProd.ForEach(E =>
            //{
            //    E.Entity.DoorCloseTimer = TimeSpan.FromSeconds(5);
            //});

            //var EditedEntitiesProd = ChangeTracker.Entries<Facility>().Where(E => E.State == EntityState.Modified).ToList();

            //EditedEntitiesProd.ForEach(E =>
            //{
            //    if (E.Entity.IsOpenDoorRequested)
            //    {
            //        // Possibly implement countdown timer here
            //        E.Entity.DoorCloseTimer = TimeSpan.FromSeconds(5);
            //    }
            //});
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        }
    }
}
