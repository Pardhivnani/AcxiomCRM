using AcxiomCRM.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<Opportunity> Opportunities => Set<Opportunity>();
        public DbSet<FollowUp> FollowUps => Set<FollowUp>();
        public DbSet<Activity> Activities => Set<Activity>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customer
            builder.Entity<Customer>(entity =>
            {
                entity.HasKey(c => c.CustomerId);
                entity.HasIndex(c => c.CustomerCode).IsUnique();
                entity.HasIndex(c => c.Email).IsUnique();
                entity.HasIndex(c => c.Phone).IsUnique();

                entity.HasOne(c => c.CreatedBy)
                    .WithMany()
                    .HasForeignKey(c => c.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.AssignedTo)
                    .WithMany(u => u.AssignedCustomers)
                    .HasForeignKey(c => c.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Lead
            builder.Entity<Lead>(entity =>
            {
                entity.HasKey(l => l.LeadId);
                entity.HasIndex(l => l.LeadCode).IsUnique();
                entity.Property(l => l.ExpectedValue).HasPrecision(18, 2);

                entity.HasOne(l => l.AssignedTo)
                    .WithMany(u => u.AssignedLeads)
                    .HasForeignKey(l => l.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.Customer)
                    .WithMany(c => c.Leads)
                    .HasForeignKey(l => l.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Opportunity
            builder.Entity<Opportunity>(entity =>
            {
                entity.HasKey(o => o.OpportunityId);
                entity.Property(o => o.Amount).HasPrecision(18, 2);

                entity.HasOne(o => o.Customer)
                    .WithMany(c => c.Opportunities)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Lead)
                    .WithMany(l => l.Opportunities)
                    .HasForeignKey(o => o.LeadId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(o => o.AssignedTo)
                    .WithMany(u => u.AssignedOpportunities)
                    .HasForeignKey(o => o.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // FollowUp
            builder.Entity<FollowUp>(entity =>
            {
                entity.HasKey(f => f.FollowUpId);

                entity.HasOne(f => f.Customer)
                    .WithMany(c => c.FollowUps)
                    .HasForeignKey(f => f.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(f => f.Lead)
                    .WithMany(l => l.FollowUps)
                    .HasForeignKey(f => f.LeadId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(f => f.Opportunity)
                    .WithMany(o => o.FollowUps)
                    .HasForeignKey(f => f.OpportunityId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(f => f.AssignedTo)
                    .WithMany(u => u.AssignedFollowUps)
                    .HasForeignKey(f => f.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Activity
            builder.Entity<Activity>(entity =>
            {
                entity.HasKey(a => a.ActivityId);

                entity.HasOne(a => a.Customer)
                    .WithMany(c => c.Activities)
                    .HasForeignKey(a => a.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.Lead)
                    .WithMany(l => l.Activities)
                    .HasForeignKey(a => a.LeadId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.Opportunity)
                    .WithMany(o => o.Activities)
                    .HasForeignKey(a => a.OpportunityId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.AssignedTo)
                    .WithMany(u => u.AssignedActivities)
                    .HasForeignKey(a => a.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // AuditLog
            builder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.AuditLogId);
                entity.HasIndex(a => a.CreatedDate);
                entity.HasIndex(a => a.EntityName);
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => a.Action);
            });

            // User Self-Reference for Manager Hierarchy
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(u => u.Manager)
                    .WithMany(u => u.DirectReports)
                    .HasForeignKey(u => u.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}

