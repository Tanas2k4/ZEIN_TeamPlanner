using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.Services
{
    public interface IGroupService
    {
        Task<Group> CreateGroupAsync(CreateGroupDto dto, string creatorId);
        Task<Group> UpdateGroupAsync(EditGroupDto dto, string userId);
    }
}