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
        private readonly IGroupService _groupService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public TaskItemsController(ITaskService taskService, IGroupService groupService, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _taskService = taskService;
            _groupService = groupService;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.CanAccessGroupAsync(groupId, userId))
                return Forbid();

            var isMember = !await _groupService.IsUserAdminAsync(groupId, userId);
            var tasks = await _context.TaskItems
                .Include(t => t.AssignedToUser)
                .Include(t => t.Priority)
                .Where(t => t.GroupId == groupId)
                .OrderBy(t => t.Deadline.HasValue ? 0 : 1)
                .ThenBy(t => t.Deadline)
                .ToListAsync();

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = (await _context.Groups.FindAsync(groupId))?.GroupName;
            ViewBag.IsMember = isMember;
            return View(tasks);
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
                .OrderBy(t => t.Deadline.HasValue ? 0 : 1)
                .ThenBy(t => t.Deadline)
                .ToListAsync();

            if (!tasks.Any())
            {
                ViewBag.Message = "Bạn chưa có nhiệm vụ nào. Hãy tham gia hoặc tạo một nhóm để bắt đầu.";
            }

            return View(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.TaskItems
                .Include(t => t.Group)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Priority)
                .FirstOrDefaultAsync(t => t.TaskItemId == id);

            if (task == null || !await _taskService.CanAccessTaskAsync(id, userId))
                return Forbid();

            ViewBag.IsMember = !await _groupService.IsUserAdminAsync(task.GroupId, userId);

            // Fetch attachments
            var attachments = await _context.FileAttachments
                .Where(f => f.EntityType == "TaskItem" && f.EntityId == id)
                .ToListAsync();
            ViewBag.Attachments = attachments;

            return View(task);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.IsUserAdminAsync(groupId, userId))
                return Forbid();

            var members = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId && gm.LeftAt == null)
                .Include(gm => gm.User)
                .Select(gm => new { gm.UserId, gm.User.FullName })
                .ToListAsync();

            var group = await _context.Groups
                .Include(g => g.CreatedByUser)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null)
                return NotFound();

            if (!members.Any(m => m.UserId == group.CreatedByUserId))
            {
                members.Add(new { UserId = group.CreatedByUserId, FullName = group.CreatedByUser?.FullName ?? "Admin" });
            }

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.IsUserAdminAsync(dto.GroupId, userId))
                return Forbid();

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
                ViewBag.GroupName = (await _context.Groups.FindAsync(dto.GroupId))?.GroupName;
                return View(dto);
            }

            try
            {
                await _taskService.CreateTaskAsync(dto, userId);
                return RedirectToAction("Index", new { groupId = dto.GroupId });
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
                ViewBag.GroupName = (await _context.Groups.FindAsync(dto.GroupId))?.GroupName;
                return View(dto);
            }
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

            if (!await _taskService.CanAccessTaskAsync(id, userId))
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _taskService.CanAccessTaskAsync(dto.TaskItemId, userId))
                return Forbid();

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
                ViewBag.GroupName = (await _context.Groups.FindAsync(dto.GroupId))?.GroupName;
                return View(dto);
            }

            try
            {
                await _taskService.UpdateTaskAsync(dto, userId);
                return RedirectToAction("Index", new { groupId = dto.GroupId });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
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
                ViewBag.GroupName = (await _context.Groups.FindAsync(dto.GroupId))?.GroupName;
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.TaskItems
                .Include(t => t.Group)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Priority)
                .FirstOrDefaultAsync(t => t.TaskItemId == id);

            if (task == null)
                return NotFound();

            if (!await _groupService.IsUserAdminAsync(task.GroupId, userId))
                return Forbid();

            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.IsUserAdminAsync(task.GroupId, userId))
                return Forbid();

            try
            {
                await _taskService.DeleteTaskAsync(id, userId);
                return RedirectToAction(nameof(Index), new { groupId = task.GroupId });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int taskId, TaskItem.TaskStatus status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.TaskItems
                .Include(t => t.Group)
                .FirstOrDefaultAsync(t => t.TaskItemId == taskId);

            if (task == null)
                return NotFound();

            if (!await _taskService.CanAccessTaskAsync(taskId, userId))
                return Forbid();

            try
            {
                await _taskService.UpdateTaskStatusAsync(taskId, status, userId);
                return RedirectToAction(nameof(Index), new { groupId = task.GroupId });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { groupId = task.GroupId });
            }
        }

        // New: Upload Attachment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(int taskId, IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.TaskItems
                .Include(t => t.Group)
                .FirstOrDefaultAsync(t => t.TaskItemId == taskId);

            if (task == null || !await _taskService.CanAccessTaskAsync(taskId, userId))
                return Forbid();

            var isMember = !await _groupService.IsUserAdminAsync(task.GroupId, userId);
            var isWithinDeadline = task.Deadline.HasValue && task.Deadline > DateTime.UtcNow;

            if (isMember && !isWithinDeadline)
            {
                TempData["Error"] = "Cannot upload file after deadline.";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            if (file != null && file.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException());
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var attachment = new FileAttachment
                {
                    FileName = file.FileName,
                    FileUrl = $"/uploads/{fileName}",
                    EntityType = "TaskItem",
                    EntityId = taskId,
                    UserId = userId,
                    UploadedAt = DateTime.UtcNow
                };

                _context.FileAttachments.Add(attachment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "File uploaded successfully.";
            }

            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // New: Delete Attachment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int attachmentId, int taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var attachment = await _context.FileAttachments
                .FirstOrDefaultAsync(f => f.Id == attachmentId && f.EntityType == "TaskItem" && f.EntityId == taskId);

            if (attachment == null || !await _taskService.CanAccessTaskAsync(taskId, userId))
                return Forbid();

            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.TaskItemId == taskId);
            var isMember = !await _groupService.IsUserAdminAsync(task.GroupId, userId);
            var isWithinDeadline = task.Deadline.HasValue && task.Deadline > DateTime.UtcNow;

            if (isMember && !isWithinDeadline)
            {
                TempData["Error"] = "Cannot delete file after deadline.";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FileUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.FileAttachments.Remove(attachment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "File deleted successfully.";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }
    }
}