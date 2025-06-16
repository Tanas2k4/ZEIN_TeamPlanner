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

        // Hiển thị danh sách dự án mà người dùng là thành viên hoặc người tạo
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var groups = await _context.Groups
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .Where(g => g.Members.Any(m => m.UserId == userId) || g.CreatedByUserId == userId)
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

                // Gán người tạo và khởi tạo các danh sách rỗng
                model.CreatedByUserId = userId;
                model.Members = new List<GroupMember>();
                model.Tasks = new List<TaskItem>();
                model.Events = new List<CalendarEvent>();

                _context.Groups.Add(model);
                await _context.SaveChangesAsync();

                // Thêm người tạo làm Admin của nhóm
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

        // Hiển thị chi tiết dự án, chỉ cho phép thành viên hoặc người tạo xem
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var group = await _context.Groups
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null) return NotFound();

            if (!group.Members.Any(m => m.UserId == userId) && group.CreatedByUserId != userId)
            {
                return Forbid();
            }

            return View(group);
        }

        // Hiển thị form mời thành viên, chỉ dành cho Admin
        public async Task<IActionResult> InviteMembers(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var project = await _context.Groups
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.GroupId == id);
            if (project == null) return NotFound();

            var isAdmin = project.Members.Any(m => m.UserId == userId && m.Role == MemberRole.Admin) || project.CreatedByUserId == userId;
            if (!isAdmin) return Forbid();

            var viewModel = new InviteMembersViewModel
            {
                GroupId = id,
                ExistingMembers = project.Members.Select(m => m.User.Email).ToList()
            };
            return View(viewModel);
        }

        // Xử lý mời thành viên, kiểm tra email trùng lặp
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

            var isAdmin = project.Members.Any(m => m.UserId == userId && m.Role == MemberRole.Admin) || project.CreatedByUserId == userId;
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

        // Xóa thành viên khỏi nhóm, trả về JSON cho AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember([FromBody] Dictionary<string, object> request)
        {
            // Kiểm tra dữ liệu đầu vào
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

            // Kiểm tra quyền Admin hoặc người tạo
            var isAdmin = group.Members.Any(m => m.UserId == currentUserId && m.Role == MemberRole.Admin) || group.CreatedByUserId == currentUserId;
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
            // Chặn Admin xóa Admin khác
            if (member.Role == MemberRole.Admin)
            {
                return Json(new { success = false, message = "Không thể xóa Admin khác." });
            }

            _context.GroupMembers.Remove(member);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành viên thành công." });
        }

        // Thay đổi vai trò của thành viên, trả về JSON cho AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole([FromBody] Dictionary<string, object> request)
        {
            // Kiểm tra dữ liệu đầu vào
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

            // Kiểm tra quyền Admin hoặc người tạo
            var isAdmin = group.Members.Any(m => m.UserId == currentUserId && m.Role == MemberRole.Admin) || group.CreatedByUserId == currentUserId;
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

        // Trao quyền Admin cho thành viên, trả về JSON cho AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAdmin([FromBody] Dictionary<string, object> request)
        {
            // Kiểm tra dữ liệu đầu vào
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

            // Kiểm tra quyền Admin hoặc người tạo
            var isAdmin = group.Members.Any(m => m.UserId == currentUserId && m.Role == MemberRole.Admin) || group.CreatedByUserId == currentUserId;
            if (!isAdmin)
            {
                return Json(new { success = false, message = "Bạn không có quyền trao quyền Admin." });
            }

            var member = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
            {
                return Json(new { success = false, message = "Thành viên không tồn tại." });
            }

            // Cập nhật vai trò thành Admin và lưu vào CSDL
            member.Role = MemberRole.Admin;
            _context.GroupMembers.Update(member);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Trao quyền Admin thành công." });
        }
    }
}