using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class InviteMemberDto
    {
        [Required(ErrorMessage = "* Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "* Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "* Vui lòng chọn vai trò")]
        public GroupRole Role { get; set; }

        [Required]
        public int GroupId { get; set; }
    }
}