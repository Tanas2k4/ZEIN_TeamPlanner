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
    [Authorize] // Require authentication for all actions
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

        // GET: /Groups/Create - Display the create group form
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Fetch users for the member selection dropdown
            var users = await _userManager.Users
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync();
            ViewBag.Users = users;

            // Initialize an empty DTO for the form
            return View(new CreateGroupDto());
        }

        // POST: /Groups/Create - Handle form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateGroupDto dto)
        {
            if (!ModelState.IsValid)
            {
                // Reload users for the dropdown if validation fails
                var users = await _userManager.Users
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync();
                ViewBag.Users = users;
                return View(dto);
            }

            try
            {
                // Get the current user's ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Create the group using the service
                var group = await _groupService.CreateGroupAsync(dto, userId);

                // Redirect to the group details page (implement as needed)
                return RedirectToAction("Details", new { id = group.GroupId });
            }
            catch (InvalidOperationException ex)
            {
                // Handle errors like duplicate group names
                ModelState.AddModelError("", ex.Message);
                var users = await _userManager.Users
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync();
                ViewBag.Users = users;
                return View(dto);
            }
        }

        // Placeholder for group details page
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var group = await _context.Groups
                .Include(g => g.Members).ThenInclude(m => m.User) // Include members and their users
                .Include(g => g.CreatedByUser) // Include creator
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null)
            {
                return NotFound();
            }

            // Check if the user is a member or creator
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!group.Members.Any(m => m.UserId == userId) && group.CreatedByUserId != userId)
            {
                return Forbid(); // Restrict access to group members or creator
            }

            return View(group);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var groups = await _context.Groups
                .Include(g => g.Members)
                .Where(g => g.Members.Any(m => m.UserId == userId) || g.CreatedByUserId == userId)
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
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!group.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Admin))
            {
                return Forbid();
            }

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
            return View(dto); // Ensure EditGroupDto is passed
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
                return View(dto); // Ensure EditGroupDto is passed
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
                return View(dto); // Ensure EditGroupDto is passed
            }
        }
    }

}