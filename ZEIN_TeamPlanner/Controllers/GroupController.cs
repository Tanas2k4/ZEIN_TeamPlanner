using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamPlanner.Data;

namespace ZEIN_TeamPlanner.Controllers
{
    public class GroupController : Controller
    {
        private readonly ApplicationDbContext _context;
        public GroupController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var groups = _context.Groups
                .Include(g => g.Members)
                .Include(g => g.Tasks)
                .Include(g => g.Events)
                .ToList();
            return View(groups);
        }
    }
}
