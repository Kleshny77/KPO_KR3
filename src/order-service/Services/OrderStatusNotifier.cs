using Microsoft.AspNetCore.SignalR;
using order_service.Hubs;

namespace order_service.Services
{
    public class OrderStatusNotifier
    {
        private readonly IHubContext<OrderStatusHub> _hubContext;
        public OrderStatusNotifier(IHubContext<OrderStatusHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyStatusChanged(string userId, string orderId, string status)
        {
            await _hubContext.Clients.Group(userId).SendAsync("OrderStatusChanged", new { orderId, status });
        }
    }
} 