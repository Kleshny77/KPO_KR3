using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using payments_service.Data;
using payments_service.Models;
using System.Text.Json;

namespace payments_service.Services
{
    public class OrderPaymentConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderPaymentConsumer> _logger;
        private readonly string _bootstrapServers;

        public OrderPaymentConsumer(IServiceProvider serviceProvider, ILogger<OrderPaymentConsumer> logger, IConfiguration config)
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
                GroupId = "payments-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };
            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe("order-payment-requests");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
                    var msg = JsonSerializer.Deserialize<OrderPaymentRequested>(cr.Message.Value);
                    if (msg == null)
                    {
                        _logger.LogWarning("Failed to deserialize OrderPaymentRequested");
                        continue;
                    }
                    if (await db.InboxMessages.AnyAsync(x => x.MessageId == cr.Message.Key))
                        continue;
                    db.InboxMessages.Add(new InboxMessage { MessageId = cr.Message.Key, ReceivedAt = DateTime.UtcNow });
                    await db.SaveChangesAsync();

                    var account = await db.Accounts.FirstOrDefaultAsync(a => a.UserId == msg.UserId);
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
                        account.Balance -= msg.Amount;
                        success = true;
                        await db.SaveChangesAsync();
                    }
                    var result = new PaymentProcessed
                    {
                        OrderId = msg.OrderId,
                        UserId = msg.UserId,
                        Amount = msg.Amount,
                        Success = success,
                        ErrorMessage = error
                    };
                    db.OutboxMessages.Add(new OutboxMessage
                    {
                        MessageId = Guid.NewGuid().ToString(),
                        Type = nameof(PaymentProcessed),
                        Payload = JsonSerializer.Serialize(result),
                        CreatedAt = DateTime.UtcNow,
                        Sent = false
                    });
                    await db.SaveChangesAsync();
                    consumer.Commit(cr);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OrderPaymentConsumer");
                }
            }
        }
    }
} 