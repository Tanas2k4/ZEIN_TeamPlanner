using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class TaskItem
    {
        public int TaskItemId { get; set; }
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";
        [StringLength(1000)]
        public string Description { get; set; } = "";
        public enum TaskStatus { ToDo, InProgress, Done, Blocked }
        public TaskStatus Status { get; set; } = TaskStatus.ToDo;
        public DateTime CreatedAt { get; set; }
        public DateTime? Deadline { get; set; }

        public string? AssignedToUserId { get; set; }

        public ApplicationUser? AssignedToUser { get; set; }

        public int GroupId { get; set; }
        public Group Group { get; set; }

        public int? PriorityId { get; set; } // FK to Priority model
        public Priority? Priority { get; set; }
        public string? Tags { get; set; } // Comma-separated or JSON for simple tagging
        public DateTime? CompletedAt { get; set; } // When task is marked Done
    }
}