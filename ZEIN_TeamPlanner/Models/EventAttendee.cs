namespace ZEIN_TeamPlanner.Models
{
    public class EventAttendee
    {
        public int Id { get; set; }
        public int CalendarEventId { get; set; }
        public CalendarEvent CalendarEvent { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string Response { get; set; } = "Pending"; // "Accepted", "Declined", "Tentative"
    }
}
