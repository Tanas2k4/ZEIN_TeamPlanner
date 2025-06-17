using Microsoft.EntityFrameworkCore;
using TeamPlanner.Data;

using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;

        public TaskService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TaskItem> CreateTaskAsync(CreateTaskDto dto, string userId)
        {
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == dto.GroupId && gm.UserId == userId && gm.LeftAt == null);
            if (!isMember)
                throw new UnauthorizedAccessException("Bạn không phải là thành viên của group này.");

            if (!string.IsNullOrEmpty(dto.AssignedToUserId))
            {
                var isValidAssignee = await _context.GroupMembers
                    .AnyAsync(gm => gm.GroupId == dto.GroupId && gm.UserId == dto.AssignedToUserId && gm.LeftAt == null);
                if (!isValidAssignee)
                    throw new InvalidOperationException("Người được giao không phải là thành viên của group.");
            }

            if (dto.PriorityId.HasValue && !await _context.Priorities.AnyAsync(p => p.PriorityId == dto.PriorityId))
                throw new InvalidOperationException("Ưu tiên không hợp lệ.");

            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Status = dto.Status,
                Deadline = dto.Deadline,
                AssignedToUserId = dto.AssignedToUserId,
                GroupId = dto.GroupId,
                PriorityId = dto.PriorityId,
                Tags = dto.Tags,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = dto.Status == TaskItem.TaskStatus.Done ? DateTime.UtcNow : null
            };

            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<TaskItem> UpdateTaskAsync(EditTaskDto dto, string userId)
        {
            var task = await _context.TaskItems
                .Include(t => t.Group).ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(t => t.TaskItemId == dto.TaskItemId);

            if (task == null)
                throw new KeyNotFoundException("Nhiệm vụ không tồn tại.");

            var isAdmin = task.Group.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Admin);
            if (task.AssignedToUserId != userId && !isAdmin)
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa nhiệm vụ này.");

            if (!string.IsNullOrEmpty(dto.AssignedToUserId))
            {
                var isValidAssignee = await _context.GroupMembers
                    .AnyAsync(gm => gm.GroupId == task.GroupId && gm.UserId == dto.AssignedToUserId && gm.LeftAt == null);
                if (!isValidAssignee)
                    throw new InvalidOperationException("Người được giao không phải là thành viên của group.");
            }

            if (dto.PriorityId.HasValue && !await _context.Priorities.AnyAsync(p => p.PriorityId == dto.PriorityId))
                throw new InvalidOperationException("Ưu tiên không hợp lệ.");

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Status = dto.Status;
            task.Deadline = dto.Deadline;
            task.AssignedToUserId = dto.AssignedToUserId;
            task.PriorityId = dto.PriorityId;
            task.Tags = dto.Tags;
            task.CompletedAt = dto.Status == TaskItem.TaskStatus.Done ? DateTime.UtcNow : null;

            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<bool> CanAccessTaskAsync(int taskId, string userId)
        {
            var task = await _context.TaskItems
                .Include(t => t.Group).ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(t => t.TaskItemId == taskId);

            if (task == null)
                return false;

            return task.Group.Members.Any(m => m.UserId == userId && m.LeftAt == null) ||
                   task.Group.CreatedByUserId == userId;
        }
    }
}