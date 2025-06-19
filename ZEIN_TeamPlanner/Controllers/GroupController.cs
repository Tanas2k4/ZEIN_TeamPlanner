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
    public class GroupsController : Controller
    {
        private readonly IGroupService _groupService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public GroupsController(IGroupService groupService, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _groupService = groupService;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var users = await _userManager.Users
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync();
            ViewBag.Users = users;
            return View(new CreateGroupDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateGroupDto dto)
        {
            if (!ModelState.IsValid)
            {
                var users = await _userManager.Users
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync();
                ViewBag.Users = users;
                return View(dto);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var group = await _groupService.CreateGroupAsync(dto, userId);
                return RedirectToAction("Details", new { id = group.GroupId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var users = await _userManager.Users
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync();
                ViewBag.Users = users;
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.CanAccessGroupAsync(id, userId))
                return Forbid();

            var group = await _context.Groups
                .Include(g => g.Members).ThenInclude(m => m.User)
                .Include(g => g.CreatedByUser)
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null)
                return NotFound();

            ViewBag.IsAdmin = await _groupService.IsUserAdminAsync(id, userId);
            return View(group);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var groups = await _context.Groups
                .Include(g => g.Members)
                .Where(g => g.Members.Any(m => m.UserId == userId && m.LeftAt == null) || g.CreatedByUserId == userId)
                .ToListAsync();
            return View(groups);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.IsUserAdminAsync(id, userId))
                return Forbid();

            var dto = new EditGroupDto
            {
                GroupId = group.GroupId,
                GroupName = group.GroupName,
                Description = group.Description,
                MemberIds = group.Members
                    .Where(m => m.LeftAt == null)
                    .Select(m => m.UserId)
                    .ToList()
            };

            var users = await _userManager.Users
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync();
            ViewBag.Users = users;
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditGroupDto dto)
        {
            if (!ModelState.IsValid)
            {
                var users = await _userManager.Users
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync();
                ViewBag.Users = users;
                return View(dto);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var group = await _groupService.UpdateGroupAsync(dto, userId);
                return RedirectToAction("Details", new { id = group.GroupId });
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
                var users = await _userManager.Users
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync();
                ViewBag.Users = users;
                return View(dto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteMember(int groupId, string email)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.IsUserAdminAsync(groupId, userId))
                return Forbid();

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
                return NotFound();

            var invitedUser = await _userManager.FindByEmailAsync(email);
            if (invitedUser == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng với email này.";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            var oldMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == invitedUser.Id && gm.LeftAt != null);
            if (oldMember != null)
            {
                _context.GroupMembers.Remove(oldMember);
                await _context.SaveChangesAsync();
            }

            var existingMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == invitedUser.Id && gm.LeftAt == null);
            if (existingMember != null)
            {
                TempData["Error"] = "Người dùng này đã là thành viên của nhóm.";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            var groupMember = new GroupMember
            {
                GroupId = groupId,
                UserId = invitedUser.Id,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow
            };

            _context.GroupMembers.Add(groupMember);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã mời {invitedUser.FullName} vào nhóm với vai trò Member.";
            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int groupId, string memberId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                await _groupService.RemoveMemberAsync(groupId, memberId, userId);
                TempData["Success"] = "Xóa thành viên khỏi nhóm thành công.";
            }
            catch (KeyNotFoundException)
            {
                TempData["Error"] = "Nhóm hoặc thành viên không tồn tại.";
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Bạn không có quyền xóa thành viên.";
            }
            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _groupService.IsUserAdminAsync(id, userId))
                return Forbid();

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null)
                return NotFound();

            return View(group);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                await _groupService.DeleteGroupAsync(id, userId);
                TempData["Success"] = "Xóa nhóm thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                TempData["Error"] = "Nhóm không tồn tại.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                await _groupService.LeaveGroupAsync(groupId, userId);
                TempData["Success"] = "Bạn đã rời nhóm thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                TempData["Error"] = "Nhóm không tồn tại.";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = groupId });
            }
        }
    }
}