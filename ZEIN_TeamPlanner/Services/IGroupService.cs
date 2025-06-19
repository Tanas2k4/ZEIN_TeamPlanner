using System.Threading.Tasks;
using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.Services
{
    public interface IGroupService
    {
        Task<bool> CanAccessGroupAsync(int groupId, string userId);
        Task<bool> IsUserAdminAsync(int groupId, string userId);
        Task<Group> CreateGroupAsync(CreateGroupDto dto, string userId);
        Task<Group> UpdateGroupAsync(EditGroupDto dto, string userId);       
        Task RemoveMemberAsync(int groupId, string memberId, string adminId);
        Task DeleteGroupAsync(int groupId, string adminId);
        Task LeaveGroupAsync(int groupId, string userId);
        
    }
}