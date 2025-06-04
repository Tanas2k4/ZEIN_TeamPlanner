using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FullName{get; set;} 
        public string Address {get; set;}
        public int? Age { get; set;}
        public DateTime? DateOfBirth { get; set;}
        public DateTime? CreateAT { get; set; } = DateTime.UtcNow;
        public ICollection<GroupMember> GroupMemberships { get; set; }
        public ICollection<TaskItem> AssignedTasks { get; set; }
    }
}
