using MassTransit;
using Microsoft.EntityFrameworkCore;
using payments_service.Contracts;
using payments_service.Data;
using payments_service.Models;

namespace payments_service.Services
{
    public class OrderPaymentConsumer : IConsumer<OrderPaymentRequested>
    {
        private readonly PaymentsDbContext _db;
        public OrderPaymentConsumer(PaymentsDbContext db)
        {
            _db = db;
        }

        public async Task Consume(ConsumeContext<OrderPaymentRequested> context)
        {
            var msg = context.Message;
            // Exactly-once: проверяем, не обработано ли уже это сообщение
            if (await _db.InboxMessages.AnyAsync(x => x.MessageId == context.MessageId.ToString()))
                return;
            _db.InboxMessages.Add(new InboxMessage { MessageId = context.MessageId.ToString()!, ReceivedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();

            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == msg.UserId);
            bool success = false;
            string? error = null;
            if (account == null)
            {
                error = "Account not found";
            }
            else if (account.Balance < msg.Amount)
            {
                error = "Insufficient funds";
            }
            else
            {
                // Атомарное списание
                account.Balance -= msg.Amount;
                success = true;
                await _db.SaveChangesAsync();
            }

            // Outbox: записываем событие результата оплаты
            var result = new OrderPaymentResult
            {
                OrderId = msg.OrderId,
                Success = success,
                Error = error
            };
            _db.OutboxMessages.Add(new OutboxMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                Type = nameof(OrderPaymentResult),
                Payload = System.Text.Json.JsonSerializer.Serialize(result),
                CreatedAt = DateTime.UtcNow,
                Sent = false
            });
            await _db.SaveChangesAsync();
        }
    }
} 