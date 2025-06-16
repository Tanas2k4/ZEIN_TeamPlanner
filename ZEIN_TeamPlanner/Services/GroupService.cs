using Microsoft.EntityFrameworkCore;
using TeamPlanner.Data;
//using ZEIN_TeamPlanner.Data;
using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.Services
{
    public class GroupService : IGroupService
    {
        private readonly ApplicationDbContext _context;

        public GroupService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Group> CreateGroupAsync(CreateGroupDto dto, string creatorId)
        {
            // Check for duplicate group name
            if (await _context.Groups.AnyAsync(g => g.GroupName == dto.GroupName))
                throw new InvalidOperationException("Tên Group đã tồn tại.");

            var group = new Group
            {
                GroupName = dto.GroupName,
                Description = dto.Description,
                CreatedByUserId = creatorId,
                CreateAt = DateTime.UtcNow,
                Members = new List<GroupMember>
                {
                    new GroupMember
                    {
                        UserId = creatorId,
                        Role = GroupRole.Admin,
                        JoinedAt = DateTime.UtcNow
                    }
                }
            };

            // Add selected members
            if (dto.MemberIds != null && dto.MemberIds.Any())
            {
                foreach (var memberId in dto.MemberIds)
                {
                    if (memberId != creatorId && await _context.Users.AnyAsync(u => u.Id == memberId))
                    {
                        group.Members.Add(new GroupMember
                        {
                            UserId = memberId,
                            Role = GroupRole.Member,
                            JoinedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
            return group;
        }
    }
}