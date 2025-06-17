using Microsoft.AspNetCore.SignalR;

namespace order_service.Hubs
{
    public class OrderStatusHub : Hub
    {
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
    }
} 