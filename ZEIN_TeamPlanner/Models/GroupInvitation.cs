using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class GroupInvitation
    {
        public int Id { get; set; }

        [Required]
        public int GroupId { get; set; }
        public Group Group { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public GroupRole Role { get; set; }

        [Required]
        public string Token { get; set; }

        public DateTime SentAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public bool IsAccepted { get; set; }
    }
}