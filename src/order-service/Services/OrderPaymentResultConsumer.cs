using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using order_service.Data;
using order_service.Models;
using System.Text.Json;

namespace order_service.Services
{
    public class OrderPaymentResultConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderPaymentResultConsumer> _logger;
        private readonly string _bootstrapServers;

        public OrderPaymentResultConsumer(IServiceProvider serviceProvider, ILogger<OrderPaymentResultConsumer> logger, IConfiguration config)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _bootstrapServers = config["KAFKA_BOOTSTRAP_SERVERS"]!;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = "order-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };
            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe("order-payment-results");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                    var notifier = scope.ServiceProvider.GetRequiredService<OrderStatusNotifier>();
                    var msg = JsonSerializer.Deserialize<PaymentProcessed>(cr.Message.Value);
                    if (msg == null)
                    {
                        _logger.LogWarning("Failed to deserialize PaymentProcessed");
                        continue;
                    }
                    if (await db.InboxMessages.AnyAsync(x => x.MessageId == cr.Message.Key))
                        continue;
                    db.InboxMessages.Add(new InboxMessage { MessageId = cr.Message.Key, ReceivedAt = DateTime.UtcNow });
                    await db.SaveChangesAsync();

                    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == msg.OrderId);
                    if (order != null)
                    {
                        order.Status = msg.Success ? "FINISHED" : "CANCELLED";
                        await db.SaveChangesAsync();
                        await notifier.NotifyStatusChanged(order.UserId, order.Id, order.Status);
                    }
                    consumer.Commit(cr);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OrderPaymentResultConsumer");
                }
            }
        }
    }
} 