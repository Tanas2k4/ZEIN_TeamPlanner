using Ical.Net;
using Ical.Net.DataTypes;
using Microsoft.EntityFrameworkCore;
using TeamPlanner.Data;
using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.Services
{
    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _context;

        public EventService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CalendarEvent> CreateEventAsync(CreateEventDto dto, string userId)
        {
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == dto.GroupId && gm.UserId == userId && gm.LeftAt == null);
            if (!isMember)
                throw new UnauthorizedAccessException("Bạn không phải là thành viên của group này.");

            if (dto.StartTime <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Thời gian bắt đầu phải lớn hơn thời điểm hiện tại.");

            if (dto.EndTime.HasValue && dto.EndTime <= dto.StartTime)
                throw new InvalidOperationException("Thời gian kết thúc phải sau thời gian bắt đầu.");

            if (!string.IsNullOrEmpty(dto.RecurrenceRule))
            {
                try
                {
                    var icalEvent = new Ical.Net.CalendarComponents.CalendarEvent();
                    icalEvent.RecurrenceRules.Add(new RecurrencePattern(dto.RecurrenceRule));
                }
                catch
                {
                    throw new InvalidOperationException("Quy tắc lặp không hợp lệ (phải tuân theo định dạng iCal RRULE).");
                }
            }

            if (!TimeZoneInfo.GetSystemTimeZones().Any(tz => tz.Id == dto.TimeZoneId) &&
                !NodaTime.DateTimeZoneProviders.Tzdb.Ids.Contains(dto.TimeZoneId))
                throw new InvalidOperationException("Múi giờ không hợp lệ.");

            var calendarEvent = new CalendarEvent
            {
                Title = dto.Title,
                Description = dto.Description,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                TimeZoneId = dto.TimeZoneId,
                GroupId = dto.GroupId,
                IsAllDay = dto.IsAllDay,
                RecurrenceRule = dto.RecurrenceRule,
                Type = dto.Type
            };

            _context.CalendarEvents.Add(calendarEvent);
            await _context.SaveChangesAsync();
            return calendarEvent;
        }

        public async Task<CalendarEvent> UpdateEventAsync(EditEventDto dto, string userId)
        {
            var calendarEvent = await _context.CalendarEvents
                .Include(e => e.Group).ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(e => e.CalendarEventId == dto.CalendarEventId);

            if (calendarEvent == null)
                throw new KeyNotFoundException("Sự kiện không tồn tại.");

            var isAdmin = calendarEvent.Group.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Admin);
            if (!isAdmin)
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa sự kiện này.");

            //Check time constraints
            if (dto.StartTime <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Thời gian bắt đầu phải lớn hơn thời điểm hiện tại.");

            if (dto.EndTime.HasValue && dto.EndTime <= dto.StartTime)
                throw new InvalidOperationException("Thời gian kết thúc phải sau thời gian bắt đầu.");

            if (!string.IsNullOrEmpty(dto.RecurrenceRule))
            {
                try
                {
                    var icalEvent = new Ical.Net.CalendarComponents.CalendarEvent();
                    icalEvent.RecurrenceRules.Add(new RecurrencePattern(dto.RecurrenceRule));
                }
                catch
                {
                    throw new InvalidOperationException("Quy tắc lặp không hợp lệ (phải tuân theo định dạng iCal RRULE).");
                }
            }

            if (!TimeZoneInfo.GetSystemTimeZones().Any(tz => tz.Id == dto.TimeZoneId) &&
                !NodaTime.DateTimeZoneProviders.Tzdb.Ids.Contains(dto.TimeZoneId))
                throw new InvalidOperationException("Múi giờ không hợp lệ.");

            calendarEvent.Title = dto.Title;
            calendarEvent.Description = dto.Description;
            calendarEvent.StartTime = dto.StartTime;
            calendarEvent.EndTime = dto.EndTime;
            calendarEvent.TimeZoneId = dto.TimeZoneId;
            calendarEvent.IsAllDay = dto.IsAllDay;
            calendarEvent.RecurrenceRule = dto.RecurrenceRule;
            calendarEvent.Type = dto.Type;

            await _context.SaveChangesAsync();
            return calendarEvent;
        }

        public async Task<bool> CanAccessEventAsync(int eventId, string userId)
        {
            var calendarEvent = await _context.CalendarEvents
                .Include(e => e.Group).ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(e => e.CalendarEventId == eventId);

            if (calendarEvent == null)
                return false;

            return calendarEvent.Group.Members.Any(m => m.UserId == userId && m.LeftAt == null) ||
                   calendarEvent.Group.CreatedByUserId == userId;
        }
    }
}