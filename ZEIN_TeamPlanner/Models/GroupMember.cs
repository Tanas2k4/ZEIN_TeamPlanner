using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZEIN_TeamPlanner.Models
{
    // Define enum at namespace level for reusability
    public enum GroupRole
    {
        Admin,
        Member
    }

    public class GroupMember
    {
        [Key, Column(Order = 0)]
        public int GroupId { get; set; }

        [Key, Column(Order = 1)]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [ForeignKey("GroupId")]
        public Group Group { get; set; }

        public GroupRole Role { get; set; } = GroupRole.Member;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LeftAt { get; set; } // Tracks when a user leaves the group
    }
}