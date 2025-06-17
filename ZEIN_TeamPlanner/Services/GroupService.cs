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
        public async Task<Group> UpdateGroupAsync(EditGroupDto dto, string userId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == dto.GroupId);

            if (group == null)
                throw new KeyNotFoundException("Group không tồn tại.");

            // Check if user is an admin
            if (!group.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Admin))
                throw new UnauthorizedAccessException("Chỉ admin mới có thể chỉnh sửa group.");

            // Check for duplicate group name (excluding current group)
            if (await _context.Groups.AnyAsync(g => g.GroupName == dto.GroupName && g.GroupId != dto.GroupId))
                throw new InvalidOperationException("Tên Group đã tồn tại.");

            // Update group details
            group.GroupName = dto.GroupName;
            group.Description = dto.Description;

            // Update members
            var currentMemberIds = group.Members
                .Where(m => m.LeftAt == null)
                .Select(m => m.UserId)
                .ToList();

            var newMemberIds = dto.MemberIds ?? new List<string>();

            // Remove members not in new list
            foreach (var member in group.Members.Where(m => m.LeftAt == null))
            {
                if (!newMemberIds.Contains(member.UserId) && member.UserId != group.CreatedByUserId)
                {
                    member.LeftAt = DateTime.UtcNow; // Soft delete
                }
            }

            // Add new members
            foreach (var memberId in newMemberIds)
            {
                if (!currentMemberIds.Contains(memberId) && memberId != group.CreatedByUserId)
                {
                    if (await _context.Users.AnyAsync(u => u.Id == memberId))
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

            await _context.SaveChangesAsync();
            return group;
        }
    }
}