using Microsoft.EntityFrameworkCore;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Infrastructure.Data
{
    public class TicketDbContext(DbContextOptions<TicketDbContext> options) : DbContext(options)
    {

        //Core Entities
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<TicketTag> TicketTags => Set<TicketTag>();
        public DbSet<OperatorTag> OperatorTags => Set<OperatorTag>();
        public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
        public DbSet<TicketAuditLog> TicketAuditLogs => Set<TicketAuditLog>();

        //Replicated Read-Models
        public DbSet<UserSnapshot> UserSnapshots => Set<UserSnapshot>();
        public DbSet<OrganizationSnapshot> OrganizationSnapshots => Set<OrganizationSnapshot>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Configure enums as strings
            modelBuilder
                .Entity<UserSnapshot>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder
                .Entity<TicketTag>()
                .Property(u => u.AssignedBy)
                .HasConversion<string>();

            modelBuilder
                .Entity<Ticket>()
                .Property(u => u.Status)
                .HasConversion<string>();

            //Ticket Configuration
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(t => t.Id);
                
                entity.HasIndex(t => t.AssignedOperatorId);
                entity.HasIndex(t => t.CreatorId);
                entity.HasIndex(t => t.Status);

                entity.Property(t => t.Subject).HasMaxLength(255).IsRequired();
                entity.Property(t => t.Status).HasMaxLength(50).IsRequired();
            });

            //Tag Configuration
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.HasIndex(t => t.OrganizationId);
                
                entity.Property(t => t.Name).HasMaxLength(100).IsRequired();
            });

            //TicketTag Configuration
            modelBuilder.Entity<TicketTag>(entity =>
            {
                // Composite Primary Key
                entity.HasKey(tt => new { tt.TicketId, tt.TagId });

                entity.HasOne(tt => tt.Ticket)
                      .WithMany(t => t.TicketTags)
                      .HasForeignKey(tt => tt.TicketId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tt => tt.Tag)
                      .WithMany(t => t.TicketTags)
                      .HasForeignKey(tt => tt.TagId)
                      .OnDelete(DeleteBehavior.Restrict); //Prevent deleting a tag if it's used on a ticket
            });

            //OperatorTag Configuration
            modelBuilder.Entity<OperatorTag>(entity =>
            {
                entity.HasKey(ot => new { ot.OperatorId, ot.TagId });

                entity.HasOne(ot => ot.Tag)
                      .WithMany(t => t.OperatorTags)
                      .HasForeignKey(ot => ot.TagId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            //TicketMessage Configuration
            modelBuilder.Entity<TicketMessage>(entity =>
            {
                entity.HasKey(tm => tm.Id);

                entity.HasIndex(tm => tm.TicketId);
                entity.HasIndex(tm => tm.SenderId);

                entity.HasOne(tm => tm.Ticket)
                      .WithMany(t => t.Messages)
                      .HasForeignKey(tm => tm.TicketId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            //TicketAuditLog Configuration
            modelBuilder.Entity<TicketAuditLog>(entity =>
            {
                entity.HasKey(al => al.Id);

                entity.HasIndex(al => al.TicketId);
                entity.HasIndex(al => al.ChangedById);

                entity.HasOne(al => al.Ticket)
                      .WithMany(t => t.AuditLogs)
                      .HasForeignKey(al => al.TicketId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            //UserSnapshot Configuration
            modelBuilder.Entity<UserSnapshot>(entity =>
            {
                entity.HasKey(us => us.Id);

                entity.HasIndex(us => us.OrganizationId);

                entity.HasMany(us => us.CreatedTickets)
                      .WithOne(t => t.Creator)
                      .HasForeignKey(t => t.CreatorId)
                      .OnDelete(DeleteBehavior.Restrict); //Prevent deleting a user if they created tickets

                entity.HasMany(us => us.AssignedTickets)
                        .WithOne(t => t.AssignedOperator)
                        .HasForeignKey(t => t.AssignedOperatorId)
                        .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(us => us.TicketAuditLogs)
                      .WithOne(al => al.ChangedBy)
                      .HasForeignKey(al => al.ChangedById)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            //OrganizationSnapshot Configuration
            modelBuilder.Entity<OrganizationSnapshot>(entity =>
            {
                entity.HasKey(os => os.Id);

                entity.HasMany(os => os.Users)
                      .WithOne(u => u.Organization)
                      .HasForeignKey(u => u.OrganizationId)
                      .OnDelete(DeleteBehavior.Restrict); //Prevent deleting an organization if it has users
            });
        }
    }
}