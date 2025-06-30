using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class InviteMemberDto
    {
        [Required(ErrorMessage = "* Please enter email")]
        [EmailAddress(ErrorMessage = "* Email is not valid")]
        public string Email { get; set; }

        [Required(ErrorMessage = "* Please select role")]
        public GroupRole Role { get; set; }

        [Required]
        public int GroupId { get; set; }
    }
}