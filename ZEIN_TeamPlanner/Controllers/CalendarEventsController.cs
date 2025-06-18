using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.Security.Claims;
using TeamPlanner.Data;
using ZEIN_TeamPlanner.Models;
using ZEIN_TeamPlanner.Services;

namespace ZEIN_TeamPlanner.Controllers
{
    [Authorize]
    public class CalendarEventsController : Controller
    {
        private readonly IEventService _eventService;
        private readonly IGroupService _groupService;
        private readonly ApplicationDbContext _context;
        public CalendarEventsController(IEventService eventService, IGroupService groupService, ApplicationDbContext context)
        {
            _eventService = eventService;
            _groupService = groupService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.CanAccessGroupAsync(groupId, userId))
                return Forbid();

            var isMember = !await _groupService.IsUserAdminAsync(groupId, userId);
            var events = await _context.CalendarEvents
                .Where(e => e.GroupId == groupId)
                .Include(e => e.Group)
                .ToListAsync();

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = (await _context.Groups.FindAsync(groupId))?.GroupName;
            ViewBag.IsMember = isMember; // Dùng để ẩn nút Create/Edit/Delete với Member
            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> Calendar(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.CanAccessGroupAsync(groupId, userId))
                return Forbid();

            var group = await _context.Groups.FindAsync(groupId);
            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group?.GroupName;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GlobalCalendar()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var groups = await _context.Groups
                .Include(g => g.Members)
                .Where(g => g.Members.Any(m => m.UserId == userId && m.LeftAt == null) || g.CreatedByUserId == userId)
                .ToListAsync();

            if (!groups.Any())
            {
                ViewBag.Message = "Bạn chưa tham gia nhóm nào. Hãy tham gia hoặc tạo một nhóm để xem sự kiện.";
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetEvents(int groupId, DateTimeOffset start, DateTimeOffset end)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.CanAccessGroupAsync(groupId, userId))
                return Forbid();

            var events = await _context.CalendarEvents
                .Where(e => e.GroupId == groupId && e.StartTime >= start && (e.EndTime == null || e.EndTime <= end))
                .Include(e => e.Group)
                .Select(e => new
                {
                    id = e.CalendarEventId,
                    title = e.Title,
                    start = e.StartTime.ToString("o"),
                    end = e.EndTime.HasValue ? e.EndTime.Value.ToString("o") : null,
                    allDay = e.IsAllDay,
                    rrule = e.RecurrenceRule,
                    extendedProps = new
                    {
                        type = e.Type.ToString(),
                        description = e.Description,
                        timeZone = e.TimeZoneId,
                        groupName = e.Group.GroupName
                    }
                })
                .ToListAsync();

            return Json(events);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEvents(DateTimeOffset start, DateTimeOffset end)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var groupIds = await _context.GroupMembers
                .Where(gm => gm.UserId == userId && gm.LeftAt == null)
                .Select(gm => gm.GroupId)
                .Union(_context.Groups.Where(g => g.CreatedByUserId == userId).Select(g => g.GroupId))
                .ToListAsync();

            var events = await _context.CalendarEvents
                .Include(e => e.Group)
                .Where(e => groupIds.Contains(e.GroupId) && e.StartTime >= start && (e.EndTime == null || e.EndTime <= end))
                .Select(e => new
                {
                    id = e.CalendarEventId,
                    title = $"{e.Title} ({e.Group.GroupName})",
                    start = e.StartTime.ToString("o"),
                    end = e.EndTime.HasValue ? e.EndTime.Value.ToString("o") : null,
                    allDay = e.IsAllDay,
                    rrule = e.RecurrenceRule,
                    extendedProps = new
                    {
                        type = e.Type.ToString(),
                        description = e.Description,
                        timeZone = e.TimeZoneId,
                        groupName = e.Group.GroupName
                    }
                })
                .ToListAsync();

            return Json(events);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEventTime(int id, DateTimeOffset start, DateTimeOffset? end)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var calendarEvent = await _context.CalendarEvents
                .Include(e => e.Group).ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(e => e.CalendarEventId == id);

            if (calendarEvent == null)
                return NotFound();

            var isAdmin = await _groupService.IsUserAdminAsync(calendarEvent.GroupId, userId);
            if (!isAdmin)
                return Forbid();

            if (end.HasValue && end <= start)
                return BadRequest("Thời gian kết thúc phải sau thời gian bắt đầu.");

            calendarEvent.StartTime = start;
            calendarEvent.EndTime = end;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Create(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.IsUserAdminAsync(groupId, userId))
                return Forbid();

            var group = await _context.Groups.FindAsync(groupId);
            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group?.GroupName;
            ViewBag.TimeZones = GetTimeZoneSelectList();
            return View(new CreateEventDto { GroupId = groupId, StartTime = DateTimeOffset.Now, TimeZoneId = "Asia/Ho_Chi_Minh" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEventDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.IsUserAdminAsync(dto.GroupId, userId))
                return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.GroupId = dto.GroupId;
                var group = await _context.Groups.FindAsync(dto.GroupId);
                ViewBag.GroupName = group?.GroupName;
                ViewBag.TimeZones = GetTimeZoneSelectList();
                return View(dto);
            }

            try
            {
                await _eventService.CreateEventAsync(dto, userId);
                return RedirectToAction("Index", new { groupId = dto.GroupId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.GroupId = dto.GroupId;
                var group = await _context.Groups.FindAsync(dto.GroupId);
                ViewBag.GroupName = group?.GroupName;
                ViewBag.TimeZones = GetTimeZoneSelectList();
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var calendarEvent = await _context.CalendarEvents
                .Include(e => e.Group)
                .FirstOrDefaultAsync(e => e.CalendarEventId == id);

            if (calendarEvent == null || !await _eventService.CanAccessEventAsync(id, userId))
                return Forbid();

            return View(calendarEvent);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var calendarEvent = await _context.CalendarEvents
                .Include(e => e.Group)
                .FirstOrDefaultAsync(e => e.CalendarEventId == id);

            if (calendarEvent == null)
                return NotFound();

            if (!await _groupService.IsUserAdminAsync(calendarEvent.GroupId, userId))
                return Forbid();

            var dto = new EditEventDto
            {
                CalendarEventId = calendarEvent.CalendarEventId,
                Title = calendarEvent.Title,
                Description = calendarEvent.Description,
                StartTime = calendarEvent.StartTime,
                EndTime = calendarEvent.EndTime,
                TimeZoneId = calendarEvent.TimeZoneId,
                GroupId = calendarEvent.GroupId,
                IsAllDay = calendarEvent.IsAllDay,
                RecurrenceRule = calendarEvent.RecurrenceRule,
                Type = calendarEvent.Type
            };

            ViewBag.GroupId = calendarEvent.GroupId;
            ViewBag.GroupName = calendarEvent.Group.GroupName;
            ViewBag.TimeZones = GetTimeZoneSelectList();
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditEventDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.IsUserAdminAsync(dto.GroupId, userId))
                return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.GroupId = dto.GroupId;
                var group = await _context.Groups.FindAsync(dto.GroupId);
                ViewBag.GroupName = group?.GroupName;
                ViewBag.TimeZones = GetTimeZoneSelectList();
                return View(dto);
            }

            try
            {
                await _eventService.UpdateEventAsync(dto, userId);
                return RedirectToAction("Index", new { groupId = dto.GroupId });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.GroupId = dto.GroupId;
                var group = await _context.Groups.FindAsync(dto.GroupId);
                ViewBag.GroupName = group?.GroupName;
                ViewBag.TimeZones = GetTimeZoneSelectList();
                return View(dto);
            }
        }

        private SelectList GetTimeZoneSelectList()
        {
            var timeZones = NodaTime.DateTimeZoneProviders.Tzdb.Ids
                .Select(id => new { Id = id, DisplayName = id })
                .ToList();
            return new SelectList(timeZones, "Id", "DisplayName");
        }
    }
}