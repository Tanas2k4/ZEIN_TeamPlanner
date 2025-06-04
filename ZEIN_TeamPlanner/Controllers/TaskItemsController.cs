using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamPlanner.Data;

namespace ZEIN_TeamPlanner.Controllers
{
    public class TaskItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TaskItemsController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var task = await _context.TaskItems
                .Include(t => t.AssignedToUser)
                .ToArrayAsync();
            return View(task);
        }
    }
}
