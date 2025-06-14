using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ZEIN_TeamPlanner.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ZEIN_TeamPlanner.Controllers
{
    public class SettingController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public SettingController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var model = new SettingsViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl ?? "/images/default-avatar.png"
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSettings(SettingsViewModel model, IFormFile avatar)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!string.IsNullOrEmpty(model.FullName))
            {
                user.FullName = model.FullName;
            }

            if (avatar != null && avatar.Length > 0)
            {
                if (avatar.Length > 1_000_000)
                {
                    ModelState.AddModelError("avatar", "Image size must be less than 1MB.");
                }
                else
                {
                    var fileName = $"{Guid.NewGuid()}_{avatar.FileName}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await avatar.CopyToAsync(stream);
                    }
                    user.AvatarUrl = $"/images/{fileName}";
                }
            }

            //// Handle checkbox array for NotificationPreferences
            //user.NotificationPreferences = string.Join(",", Request.Form["NotificationPreferences"] ?? new string[] { });
            //user.Status = model.Status;
            //user.Theme = model.Theme;
            //user.DefaultPage = model.DefaultPage;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("Index");
            }
            else
            {
                // Log errors if needed
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }
        }
    }

    public class SettingsViewModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        //public string NotificationPreferences { get; set; } // e.g., "Messages,Tasks"
        //public string Status { get; set; } // e.g., "Available", "Busy"
        //public string Theme { get; set; } // e.g., "Light", "Dark"
        //public string DefaultPage { get; set; } // e.g., "Dashboard/Index"
    }
}