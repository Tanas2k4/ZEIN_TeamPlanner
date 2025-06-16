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

        // DbSet cho các entity
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<Invitation> Invitations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Ràng buộc duy nhất: mỗi user chỉ là thành viên 1 lần trong mỗi nhóm
            builder.Entity<GroupMember>()
                .HasIndex(gm => new { gm.UserId, gm.GroupId })
                .IsUnique();

            // Quan hệ: Group - GroupMember (1-n)
            builder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ: GroupMember - User (n-1)
            builder.Entity<GroupMember>()
                .HasOne(gm => gm.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ: TaskItem - Group
            builder.Entity<TaskItem>()
                .HasOne(t => t.Group)
                .WithMany(g => g.Tasks)
                .HasForeignKey(t => t.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ: TaskItem - AssignedToUser
            builder.Entity<TaskItem>()
                .HasOne(t => t.AssignedToUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Quan hệ: CalendarEvent - Group
            builder.Entity<CalendarEvent>()
                .HasOne(e => e.Group)
                .WithMany(g => g.Events)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ: Group - CreatedByUser
            builder.Entity<Group>()
                .HasOne(g => g.CreatedByUser)
                .WithMany() // Không có thuộc tính điều hướng ngược
                .HasForeignKey(g => g.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull); // Đặt thành null nếu user bị xóa

            // Quan hệ: Invitation - Group
            builder.Entity<Invitation>()
                .HasOne(i => i.Group)
                .WithMany()
                .HasForeignKey(i => i.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ràng buộc dữ liệu User
            builder.Entity<ApplicationUser>()
                .Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(100);
        }
    }
}