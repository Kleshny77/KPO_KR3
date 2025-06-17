using MassTransit;
using Microsoft.EntityFrameworkCore;
using order_service.Contracts;
using order_service.Data;
using order_service.Models;

namespace order_service.Services
{
    public class OrderPaymentResultConsumer : IConsumer<OrderPaymentResult>
    {
        private readonly OrdersDbContext _db;
        private readonly OrderStatusNotifier _notifier;
        public OrderPaymentResultConsumer(OrdersDbContext db, OrderStatusNotifier notifier)
        {
            _db = db;
            _notifier = notifier;
        }

        public async Task Consume(ConsumeContext<OrderPaymentResult> context)
        {
            var msg = context.Message;
            // Exactly-once: проверяем, не обработано ли уже это сообщение
            if (await _db.InboxMessages.AnyAsync(x => x.MessageId == context.MessageId.ToString()))
                return;
            _db.InboxMessages.Add(new InboxMessage { MessageId = context.MessageId.ToString()!, ReceivedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == msg.OrderId);
            if (order != null)
            {
                order.Status = msg.Success ? "FINISHED" : "CANCELLED";
                await _db.SaveChangesAsync();
                await _notifier.NotifyStatusChanged(order.UserId, order.Id, order.Status);
            }
        }
    }
} 