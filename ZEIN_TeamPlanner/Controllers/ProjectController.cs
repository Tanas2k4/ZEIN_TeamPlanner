using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamPlanner.Data;
using ZEIN_TeamPlanner.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZEIN_TeamPlanner.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Hiển thị danh sách dự án mà người dùng là thành viên
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                // Log lỗi hoặc redirect
                return RedirectToAction("Login", "Account");
            }

            var groups = await _context.Groups
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .Where(g => g.Members.Any(m => m.UserId == userId && m.User != null))
                .ToListAsync();

            return View(groups);
        }

        // Hiển thị form tạo dự án mới
        public IActionResult Create()
        {
            return View();
        }

        // Xử lý tạo dự án mới và thêm người tạo làm Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Group model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

                model.CreatedByUserId = userId;
                model.Members = new List<GroupMember>();
                model.Tasks = new List<TaskItem>();
                model.Events = new List<CalendarEvent>();

                _context.Groups.Add(model);
                await _context.SaveChangesAsync();

                var creatorMembership = new GroupMember
                {
                    UserId = userId,
                    GroupId = model.GroupId,
                    Role = MemberRole.Admin,
                    JoinedAt = DateTime.UtcNow
                };
                _context.GroupMembers.Add(creatorMembership);
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", new { id = model.GroupId });
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", $"Lỗi khi lưu dữ liệu: {ex.InnerException?.Message ?? ex.Message}");
                return View(model);
            }
        }

        // Hiển thị chi tiết dự án
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var group = await _context.Groups
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null) return NotFound();

            if (!group.Members.Any(m => m.UserId == userId && m.User != null) && group.CreatedByUserId != userId)
            {
                return Forbid();
            }

            return View(group);
        }

        // Hiển thị form mời thành viên
        public async Task<IActionResult> InviteMembers(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var project = await _context.Groups
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.GroupId == id);
            if (project == null)
            {
                return NotFound();
            }

            var isAdmin = project.Members.Any(m => m.UserId == userId && m.Role == MemberRole.Admin && m.User != null) || project.CreatedByUserId == userId;
            if (!isAdmin)
            {
                return Forbid("Bạn không có quyền mời thành viên.");
            }

            var viewModel = new InviteMembersViewModel
            {
                GroupId = id,
                ExistingMembers = project.Members.Select(m => m.User.Email).ToList()
            };
            return View(viewModel);
        }

        // Xử lý mời thành viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteMembers(InviteMembersViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var project = await _context.Groups
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.GroupId == model.GroupId);
            if (project == null) return NotFound();

            var isAdmin = project.Members.Any(m => m.UserId == userId && m.Role == MemberRole.Admin && m.User != null) || project.CreatedByUserId == userId;
            if (!isAdmin) return Forbid();

            if (ModelState.IsValid)
            {
                var emails = model.Emails.Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim()).Distinct().ToList();
                foreach (var email in emails)
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        ModelState.AddModelError("", $"Người dùng với email {email} không tồn tại.");
                    }
                    else if (project.Members.Any(m => m.UserId == user.Id))
                    {
                        ModelState.AddModelError("", $"Email {email} đã là thành viên của dự án.");
                    }
                    else
                    {
                        var membership = new GroupMember
                        {
                            UserId = user.Id,
                            GroupId = model.GroupId,
                            Role = model.Role,
                            JoinedAt = DateTime.UtcNow
                        };
                        _context.GroupMembers.Add(membership);
                    }
                }

                if (ModelState.IsValid)
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", new { id = model.GroupId });
                }
            }

            model.ExistingMembers = project.Members.Select(m => m.User.Email).ToList();
            return View(model);
        }

        // Xóa thành viên khỏi nhóm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember([FromBody] Dictionary<string, object> request)
        {
            if (!request.ContainsKey("GroupId") || !request.ContainsKey("UserId") ||
                !int.TryParse(request["GroupId"].ToString(), out int groupId) || string.IsNullOrEmpty(request["UserId"]?.ToString()))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }
            string userId = request["UserId"].ToString();

            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Json(new { success = false, message = "Chưa đăng nhập." });
            }

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);
            if (group == null)
            {
                return Json(new { success = false, message = "Không tìm thấy dự án." });
            }

            var isAdmin = group.Members.Any(m => m.UserId == currentUserId && m.Role == MemberRole.Admin && m.User != null) || group.CreatedByUserId == currentUserId;
            if (!isAdmin)
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa thành viên." });
            }

            var member = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
            {
                return Json(new { success = false, message = "Thành viên không tồn tại." });
            }
            if (member.UserId == currentUserId)
            {
                return Json(new { success = false, message = "Không thể xóa chính mình." });
            }
            if (member.Role == MemberRole.Admin)
            {
                return Json(new { success = false, message = "Không thể xóa Admin khác." });
            }

            _context.GroupMembers.Remove(member);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành viên thành công." });
        }

        // Thay đổi vai trò của thành viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole([FromBody] Dictionary<string, object> request)
        {
            if (!request.ContainsKey("GroupId") || !request.ContainsKey("UserId") || !request.ContainsKey("Role") ||
                !int.TryParse(request["GroupId"].ToString(), out int groupId) || string.IsNullOrEmpty(request["UserId"]?.ToString()) ||
                !Enum.TryParse<MemberRole>(request["Role"]?.ToString(), out var role))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }
            string userId = request["UserId"].ToString();

            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Json(new { success = false, message = "Chưa đăng nhập." });
            }

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);
            if (group == null)
            {
                return Json(new { success = false, message = "Không tìm thấy dự án." });
            }

            var isAdmin = group.Members.Any(m => m.UserId == currentUserId && m.Role == MemberRole.Admin && m.User != null) || group.CreatedByUserId == currentUserId;
            if (!isAdmin)
            {
                return Json(new { success = false, message = "Bạn không có quyền thay đổi vai trò." });
            }

            var member = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
            {
                return Json(new { success = false, message = "Thành viên không tồn tại." });
            }
            if (member.UserId == currentUserId && role != MemberRole.Admin)
            {
                return Json(new { success = false, message = "Không thể thay đổi vai trò của chính mình thành không phải Admin." });
            }

            member.Role = role;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Cập nhật vai trò thành {role} thành công." });
        }

        // Trao quyền Admin cho thành viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAdmin([FromBody] Dictionary<string, object> request)
        {
            if (!request.ContainsKey("GroupId") || !request.ContainsKey("UserId") ||
                !int.TryParse(request["GroupId"].ToString(), out int groupId) || string.IsNullOrEmpty(request["UserId"]?.ToString()))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }
            string userId = request["UserId"].ToString();

            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Json(new { success = false, message = "Chưa đăng nhập." });
            }

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);
            if (group == null)
            {
                return Json(new { success = false, message = "Không tìm thấy dự án." });
            }

            var isAdmin = group.Members.Any(m => m.UserId == currentUserId && m.Role == MemberRole.Admin && m.User != null) || group.CreatedByUserId == currentUserId;
            if (!isAdmin)
            {
                return Json(new { success = false, message = "Bạn không có quyền trao quyền Admin." });
            }
            
            var member = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
            {
                return Json(new { success = false, message = "Thành viên không tồn tại." });
            }

            member.Role = MemberRole.Admin;
            _context.GroupMembers.Update(member);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Trao quyền Admin thành công." });
        }

        // Xử lý rời dự án
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestLeaveProject([FromBody] Dictionary<string, object> request)
        {
            if (!request.ContainsKey("GroupId") || !int.TryParse(request["GroupId"].ToString(), out int groupId))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Chưa đăng nhập." });
            }

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);
            if (group == null)
            {
                return Json(new { success = false, message = "Không tìm thấy dự án." });
            }

            var member = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
            {
                return Json(new { success = false, message = "Bạn không phải thành viên của dự án." });
            }

            var adminCount = group.Members.Count(m => m.Role == MemberRole.Admin && m.User != null);
            var totalMembers = group.Members.Count(m => m.User != null);

            if (member.Role == MemberRole.Admin && adminCount == 1 && totalMembers > 1)
            {
                return Json(new { success = false, message = "Bạn là Admin duy nhất, không thể rời dự án khi còn thành viên khác. Vui lòng trao quyền Admin cho người khác trước." });
            }

            _context.GroupMembers.Remove(member);
            await _context.SaveChangesAsync();

            var remainingMembers = await _context.GroupMembers.CountAsync(m => m.GroupId == groupId && m.User != null);
            if (remainingMembers == 0)
            {
                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Bạn đã rời dự án thành công.", redirectUrl = Url.Action("Index") });
        }
    }
}