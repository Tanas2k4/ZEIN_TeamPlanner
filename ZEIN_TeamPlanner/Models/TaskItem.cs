using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class TaskItem
    {
        public int TaskItemId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? Deadline { get; set; }

        public string? AssignedToUserId { get; set; }

        public ApplicationUser? AssignedToUser { get; set; }

        public int GroupId { get; set; }
        public Group Group { get; set; }
    }
}
