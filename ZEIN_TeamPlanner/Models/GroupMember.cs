
namespace ZEIN_TeamPlanner.Models
{
    public enum MemberRole
    {
        Admin,
        Editor,
        Viewer
    }
    public class GroupMember
    {
        public int GroupMemberId { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; }
        public MemberRole Role { get; set; } // "Admin", "Member" // Sử dụng enum
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    }
}
