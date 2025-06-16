using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZEIN_TeamPlanner.Models;

namespace TeamPlanner.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure composite key for GroupMember
            builder.Entity<GroupMember>()
                .HasKey(gm => new { gm.GroupId, gm.UserId });

            // Unique index is redundant if using composite key, but can be kept for clarity
            builder.Entity<GroupMember>()
                .HasIndex(gm => new { gm.UserId, gm.GroupId })
                .IsUnique();

            // Group - GroupMember (1-n)
            builder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // GroupMember - User (n-1)
            builder.Entity<GroupMember>()
                .HasOne(gm => gm.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // TaskItem - Group
            builder.Entity<TaskItem>()
                .HasOne(t => t.Group)
                .WithMany(g => g.Tasks)
                .HasForeignKey(t => t.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // TaskItem - AssignedToUser
            builder.Entity<TaskItem>()
                .HasOne(t => t.AssignedToUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // CalendarEvent - Group
            builder.Entity<CalendarEvent>()
                .HasOne(e => e.Group)
                .WithMany(g => g.Events)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Group - CreatedByUser
            builder.Entity<Group>()
                .HasOne(g => g.CreatedByUser)
                .WithMany()
                .HasForeignKey(g => g.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // User constraints
            builder.Entity<ApplicationUser>()
                .Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(100);

            // Additional constraint for Group
            builder.Entity<Group>()
                .HasIndex(g => g.GroupName)
                .IsUnique();
        }
    }
}