using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class Group
    {
        public int GroupId { get; set; }
        [Required(ErrorMessage = "* Không được để trống tên Group")]
        public string GroupName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public ICollection<CalendarEvent> Events { get; set; } = new List<CalendarEvent>();
    }
}