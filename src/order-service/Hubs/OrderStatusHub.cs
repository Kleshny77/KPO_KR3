using Microsoft.AspNetCore.SignalR;

namespace order_service.Hubs
{
    public class OrderStatusHub : Hub
    {
        // Клиент вызывает JoinUserGroup(userId), чтобы получать уведомления только по своим заказам
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
    }
} 