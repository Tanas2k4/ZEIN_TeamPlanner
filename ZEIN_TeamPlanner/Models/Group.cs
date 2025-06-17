using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class Group
    {
        public int GroupId { get; set; }

        [Required(ErrorMessage = "* Không được để trống tên Group")]
        [StringLength(100)]
        public string GroupName { get; set; } = string.Empty;
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        public string? CreatedByUserId { get; set; }
        public ApplicationUser CreatedByUser { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public string? GroupAvatarUrl { get; set; } = "/images/default-group.png"; // Group icon
        public string Visibility { get; set; } = "Private"; // "Public", "Private", "Hidden"
        public bool IsArchived { get; set; } = false; // For soft deletion
        public string? Category { get; set; } // E.g., "Engineering", "Marketing"
        public ICollection<GroupMember> Members { get; set; }
        public ICollection<TaskItem> Tasks { get; set; }
        public ICollection<CalendarEvent> Events { get; set; }

    }
}