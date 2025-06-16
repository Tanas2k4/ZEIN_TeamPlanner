using System.ComponentModel.DataAnnotations;

namespace ZEIN_TeamPlanner.Models
{
    public class InviteMembersViewModel
    {
        public int GroupId { get; set; }
        public string Emails { get; set; }
        public MemberRole Role { get; set; }
        public List<string> ExistingMembers { get; set; } = new List<string>();
    }
}