using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeamPlanner.Data;
using ZEIN_TeamPlanner.Models;
using ZEIN_TeamPlanner.Services;

namespace ZEIN_TeamPlanner.Controllers
{
    [Authorize]
    public class TaskItemsController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public TaskItemsController(ITaskService taskService, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _taskService = taskService;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null || !group.Members.Any(m => m.UserId == userId && m.LeftAt == null) && group.CreatedByUserId != userId)
                return Forbid();

            var tasks = await _context.TaskItems
                .Include(t => t.AssignedToUser)
                .Include(t => t.Priority)
                .Where(t => t.GroupId == groupId)
                .ToListAsync();

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group.GroupName;
            return View(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null || !group.Members.Any(m => m.UserId == userId && m.LeftAt == null))
                return Forbid();

            var members = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId && gm.LeftAt == null)
                .Include(gm => gm.User)
                .Select(gm => new { gm.UserId, gm.User.FullName })
                .ToListAsync();
            var priorities = await _context.Priorities
                .Select(p => new { p.PriorityId, p.Name })
                .ToListAsync();

            ViewBag.Members = members;
            ViewBag.Priorities = priorities;
            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group.GroupName;

            return View(new CreateTaskDto { GroupId = groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTaskDto dto)
        {
            if (!ModelState.IsValid)
            {
                var members = await _context.GroupMembers
                    .Where(gm => gm.GroupId == dto.GroupId && gm.LeftAt == null)
                    .Include(gm => gm.User)
                    .Select(gm => new { gm.UserId, gm.User.FullName })
                    .ToListAsync();
                var priorities = await _context.Priorities
                    .Select(p => new { p.PriorityId, p.Name })
                    .ToListAsync();
                ViewBag.Members = members;
                ViewBag.Priorities = priorities;
                ViewBag.GroupId = dto.GroupId;
                var group = await _context.Groups.FindAsync(dto.GroupId);
                ViewBag.GroupName = group?.GroupName;
                return View(dto);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var task = await _taskService.CreateTaskAsync(dto, userId);
                return RedirectToAction("Index", new { groupId = dto.GroupId });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var members = await _context.GroupMembers
                    .Where(gm => gm.GroupId == dto.GroupId && gm.LeftAt == null)
                    .Include(gm => gm.User)
                    .Select(gm => new { gm.UserId, gm.User.FullName })
                    .ToListAsync();
                var priorities = await _context.Priorities
                    .Select(p => new { p.PriorityId, p.Name })
                    .ToListAsync();
                ViewBag.Members = members;
                ViewBag.Priorities = priorities;
                ViewBag.GroupId = dto.GroupId;
                var group = await _context.Groups.FindAsync(dto.GroupId);
                ViewBag.GroupName = group?.GroupName;
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.TaskItems
                .Include(t => t.Group).ThenInclude(g => g.Members)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Priority)
                .FirstOrDefaultAsync(t => t.TaskItemId == id);

            if (task == null || !await _taskService.CanAccessTaskAsync(id, userId))
                return Forbid();

            return View(task);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.TaskItems
                .Include(t => t.Group).ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(t => t.TaskItemId == id);

            if (task == null)
                return NotFound();

            var isAdmin = task.Group.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Admin);
            if (task.AssignedToUserId != userId && !isAdmin)
                return Forbid();

            var dto = new EditTaskDto
            {
                TaskItemId = task.TaskItemId,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Deadline = task.Deadline,
                AssignedToUserId = task.AssignedToUserId,
                GroupId = task.GroupId,
                PriorityId = task.PriorityId,
                Tags = task.Tags,
                CompletedAt = task.CompletedAt
            };

            var members = await _context.GroupMembers
                .Where(gm => gm.GroupId == task.GroupId && gm.LeftAt == null)
                .Include(gm => gm.User)
                .Select(gm => new { gm.UserId, gm.User.FullName })
                .ToListAsync();
            var priorities = await _context.Priorities
                .Select(p => new { p.PriorityId, p.Name })
                .ToListAsync();

            ViewBag.Members = members;
            ViewBag.Priorities = priorities;
            ViewBag.GroupId = task.GroupId;
            ViewBag.GroupName = task.Group.GroupName;

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditTaskDto dto)
        {
            if (!ModelState.IsValid)
            {
                var members = await _context.GroupMembers
                    .Where(gm => gm.GroupId == dto.GroupId && gm.LeftAt == null)
                    .Include(gm => gm.User)
                    .Select(gm => new { gm.UserId, gm.User.FullName })
                    .ToListAsync();
                var priorities = await _context.Priorities
                    .Select(p => new { p.PriorityId, p.Name })
                    .ToListAsync();
                ViewBag.Members = members;
                ViewBag.Priorities = priorities;
                ViewBag.GroupId = dto.GroupId;
                var group = await _context.Groups.FindAsync(dto.GroupId);
                ViewBag.GroupName = group?.GroupName;
                return View(dto);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var task = await _taskService.UpdateTaskAsync(dto, userId);
                return RedirectToAction("Index", new { groupId = dto.GroupId });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var members = await _context.GroupMembers
                    .Where(gm => gm.GroupId == dto.GroupId && gm.LeftAt == null)
                    .Include(gm => gm.User)
                    .Select(gm => new { gm.UserId, gm.User.FullName })
                    .ToListAsync();
                var priorities = await _context.Priorities
                    .Select(p => new { p.PriorityId, p.Name })
                    .ToListAsync();
                ViewBag.Members = members;
                ViewBag.Priorities = priorities;
                ViewBag.GroupId = dto.GroupId;
                var group = await _context.Groups.FindAsync(dto.GroupId);
                ViewBag.GroupName = group?.GroupName;
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GlobalTasks()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var groupIds = await _context.GroupMembers
                .Where(gm => gm.UserId == userId && gm.LeftAt == null)
                .Select(gm => gm.GroupId)
                .Union(_context.Groups.Where(g => g.CreatedByUserId == userId).Select(g => g.GroupId))
                .ToListAsync();

            var tasks = await _context.TaskItems
                .Include(t => t.Group).ThenInclude(g => g.Members)
                .Include(t => t.Priority)
                .Include(t => t.AssignedToUser)
                .Where(t => groupIds.Contains(t.GroupId))
                .ToListAsync();

            if (!tasks.Any())
            {
                ViewBag.Message = "Bạn chưa có nhiệm vụ nào. Hãy tham gia hoặc tạo một nhóm để bắt đầu.";
            }

            return View(tasks);
        }
    }
}