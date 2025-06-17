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

        public async Task NotifyStatusChanged(Guid userId, Guid orderId, string status)
        {
            await _hubContext.Clients.Group(userId.ToString()).SendAsync("OrderStatusChanged", new { orderId, status });
        }
    }
} 