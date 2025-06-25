using Microsoft.AspNetCore.SignalR;

namespace ZEIN_TeamPlanner.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendNotification(string userId, string message, string type, string relatedEntityId, string relatedEntityType)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message, type, relatedEntityId, relatedEntityType);
        }
    }
}