using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.Services
{
    public interface ITaskService
    {
        Task<TaskItem> CreateTaskAsync(CreateTaskDto dto, string userId);
        Task<TaskItem> UpdateTaskAsync(EditTaskDto dto, string userId);
        Task<bool> CanAccessTaskAsync(int taskId, string userId);
        Task DeleteTaskAsync(int taskId, string userId);
        Task UpdateTaskStatusAsync(int taskId, TaskItem.TaskStatus status, string userId);
    }
}