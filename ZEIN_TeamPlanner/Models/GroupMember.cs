namespace ZEIN_TeamPlanner.Models
{
    public class GroupMember
    {
        public int GroupMemberId { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; }
        public string Role { get; set; } // "Admin", "Member"
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    }
}
