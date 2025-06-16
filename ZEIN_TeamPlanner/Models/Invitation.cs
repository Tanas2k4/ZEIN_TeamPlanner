using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class Invitation
    {
        public int InvitationId { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public int GroupId { get; set; }
        public Group Group { get; set; }
        [Required]
        public MemberRole Role { get; set; } // Sử dụng enum MemberRole
        [Required]
        public string Token { get; set; }
        public bool IsAccepted { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}