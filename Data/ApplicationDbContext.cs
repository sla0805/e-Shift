using eShift.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace eShift.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Load> Loads { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Assistant> Assistants { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<TransportAssignment> TransportAssignments { get; set; }
       

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            //1-to-1 relationship
            builder.Entity<Customer>()
                .HasOne(c => c.IdentityUser)
                .WithOne()
                .HasForeignKey<Customer>(c => c.IdentityUserId);

        }
    }
}
// protected override void OnModelCreating(ModelBuilder modelBuilder)
// {
//     // Example for one-to-one or one-to-zero-or-one between Job and TransportAssignment
//     modelBuilder.Entity<TransportAssignment>()
//         .HasOne(ta => ta.Job)
//         .WithOne(j => j.TransportAssignment) // Assuming Job has a TransportAssignment navigation prop
//         .HasForeignKey<TransportAssignment>(ta => ta.JobId); // JobId is FK for TransportAssignment

//     // Example for other one-to-many relationships (Driver to TransportAssignments)
//     modelBuilder.Entity<TransportAssignment>()
//         .HasOne(ta => ta.Driver)
//         .WithMany(d => d.TransportAssignments) // Assuming Driver has ICollection<TransportAssignment>
//         .HasForeignKey(ta => ta.DriverId);
// }
