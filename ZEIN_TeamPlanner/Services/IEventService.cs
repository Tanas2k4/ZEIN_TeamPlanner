using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.Services
{
    public interface IEventService
    {
        Task<CalendarEvent> CreateEventAsync(CreateEventDto dto, string userId);
        Task<CalendarEvent> UpdateEventAsync(EditEventDto dto, string userId);
        Task<bool> CanAccessEventAsync(int eventId, string userId);
    }
}