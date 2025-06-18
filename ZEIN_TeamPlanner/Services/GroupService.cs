using Microsoft.EntityFrameworkCore;
using TeamPlanner.Data;
using ZEIN_TeamPlanner.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ZEIN_TeamPlanner.Services
{
    public class GroupService : IGroupService
    {
        private readonly ApplicationDbContext _context;

        public GroupService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CanAccessGroupAsync(int groupId, string userId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            return group != null &&
                   (group.CreatedByUserId == userId ||
                    group.Members.Any(m => m.UserId == userId && m.LeftAt == null));
        }

        public async Task<bool> IsUserAdminAsync(int groupId, string userId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            return group != null &&
                   (group.CreatedByUserId == userId ||
                    group.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Admin && m.LeftAt == null));
        }

        public async Task<Group> CreateGroupAsync(CreateGroupDto dto, string userId)
        {
            // Kiểm tra tên nhóm có trùng không
            if (await _context.Groups.AnyAsync(g => g.GroupName == dto.GroupName))
                throw new InvalidOperationException("Tên nhóm đã tồn tại.");

            var group = new Group
            {
                GroupName = dto.GroupName,
                Description = dto.Description,
                CreatedByUserId = userId,
                CreateAt = DateTime.UtcNow // Sửa từ CreateAt và dùng DateTimeOffset
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            // Thêm người tạo vào nhóm với vai trò Admin
            var groupMember = new GroupMember
            {
                GroupId = group.GroupId,
                UserId = userId,
                Role = GroupRole.Admin,
                JoinedAt = DateTime.UtcNow
            };

            _context.GroupMembers.Add(groupMember);

            // Thêm các thành viên được chọn
            if (dto.MemberIds != null && dto.MemberIds.Any())
            {
                foreach (var memberId in dto.MemberIds.Where(id => id != userId))
                {
                    if (await _context.Users.AnyAsync(u => u.Id == memberId))
                    {
                        var member = new GroupMember
                        {
                            GroupId = group.GroupId,
                            UserId = memberId,
                            Role = GroupRole.Member,
                            JoinedAt = DateTime.UtcNow
                        };
                        _context.GroupMembers.Add(member);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return group;
        }

        public async Task<Group> UpdateGroupAsync(EditGroupDto dto, string userId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == dto.GroupId);

            if (group == null)
                throw new KeyNotFoundException("Nhóm không tồn tại.");

            if (!group.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Admin && m.LeftAt == null) &&
                group.CreatedByUserId != userId)
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa nhóm này.");

            // Kiểm tra tên nhóm trùng
            if (await _context.Groups.AnyAsync(g => g.GroupName == dto.GroupName && g.GroupId != dto.GroupId))
                throw new InvalidOperationException("Tên nhóm đã tồn tại.");

            // Cập nhật thông tin nhóm
            group.GroupName = dto.GroupName;
            group.Description = dto.Description;

            // Cập nhật danh sách thành viên
            var currentMembers = group.Members.Where(m => m.LeftAt == null).ToList();
            var currentMemberIds = currentMembers.Select(m => m.UserId).ToList();
            var newMemberIds = dto.MemberIds ?? new List<string>();

            // Xóa thành viên không còn trong danh sách
            foreach (var member in currentMembers.Where(m => !newMemberIds.Contains(m.UserId)))
            {
                member.LeftAt = DateTime.UtcNow;
            }

            // Thêm thành viên mới
            foreach (var memberId in newMemberIds.Where(id => !currentMemberIds.Contains(id)))
            {
                if (await _context.Users.AnyAsync(u => u.Id == memberId))
                {
                    var member = new GroupMember
                    {
                        GroupId = group.GroupId,
                        UserId = memberId,
                        Role = GroupRole.Member,
                        JoinedAt = DateTime.UtcNow
                    };
                    _context.GroupMembers.Add(member);
                }
            }

            await _context.SaveChangesAsync();
            return group;
        }
    }
}