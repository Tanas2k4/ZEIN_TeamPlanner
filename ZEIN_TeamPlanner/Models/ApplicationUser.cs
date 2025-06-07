using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; } // Nullable để tránh lỗi nếu không có giá trị
        public string? Address { get; set; } // Nullable nếu không bắt buộc
        public DateTime? DateOfBirth { get; set; } // Nullable vì không bắt buộc

        // Thuộc tính tính toán Age từ DateOfBirth
        public int? Age
        {
            get
            {
                if (DateOfBirth.HasValue)
                {
                    var today = DateTime.UtcNow;
                    int age = today.Year - DateOfBirth.Value.Year;
                    if (today < DateOfBirth.Value.AddYears(age))
                        age--;
                    return age;
                }
                return null;
            }
        }

        // Đặt giá trị mặc định cho CreateAT
        [Required]
        public DateTime CreateAT { get; set; } = DateTime.UtcNow;

        public ICollection<GroupMember> GroupMemberships { get; set; }
        public ICollection<TaskItem> AssignedTasks { get; set; }
    }
}
