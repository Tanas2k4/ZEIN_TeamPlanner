using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class CalendarEvent
    {
        public int CalendarEventId { get; set; }
        [Required(ErrorMessage = "* Không được để trống tiêu đề sự kiện")]
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; }
        [Required]
        public DateTime StartTime { get; set; }
        [Required]
        public DateTime EndTime { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; }
    }
}
