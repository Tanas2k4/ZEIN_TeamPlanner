using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class CalendarEvent
    {
        public int CalendarEventId { get; set; }
        [Required(ErrorMessage = "* Không được để trống tiêu đề sự kiện")]
        
        [StringLength(200)]
        public string Title { get; set; } = "";
        [StringLength(1000)]
        public string Description { get; set; } = "";
        [Required]
        public DateTime StartTime { get; set; }
        [Required]
        public DateTime? EndTime { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; }

        public bool IsAllDay { get; set; } = false; // For all-day events
        public string? RecurrenceRule { get; set; } // iCal RRULE format for recurring events
        public enum EventType { Meeting, Deadline, Reminder }
        public EventType Type { get; set; } = EventType.Meeting;
    }
}
