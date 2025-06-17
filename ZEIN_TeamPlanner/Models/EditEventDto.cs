using System.ComponentModel.DataAnnotations;
using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.Models
{
    public class EditEventDto
    {
        public int CalendarEventId { get; set; }

        [Required(ErrorMessage = "* Không được để trống tiêu đề sự kiện")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Title { get; set; } = "";

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "* Vui lòng chọn thời gian bắt đầu")]
        [DataType(DataType.DateTime)]
        public DateTimeOffset StartTime { get; set; }

        [DataType(DataType.DateTime)]
        public DateTimeOffset? EndTime { get; set; }

        [Required(ErrorMessage = "* Vui lòng chọn múi giờ")]
        [StringLength(100)]
        public string TimeZoneId { get; set; } = "UTC";

        [Required]
        public int GroupId { get; set; }

        public bool IsAllDay { get; set; } = false;

        [StringLength(500, ErrorMessage = "Quy tắc lặp không được vượt quá 500 ký tự")]
        public string? RecurrenceRule { get; set; }

        [Required(ErrorMessage = "* Vui lòng chọn loại sự kiện")]
        public CalendarEvent.EventType Type { get; set; } = CalendarEvent.EventType.Meeting;
    }
}