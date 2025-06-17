using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using order_service.Data;
using order_service.Models;
using Confluent.Kafka;
using System.Text.Json;

namespace order_service.Services
{
    public class OutboxProcessor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxProcessor> _logger;
        private readonly string _bootstrapServers;

        public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger, IConfiguration config)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _bootstrapServers = config["KAFKA_BOOTSTRAP_SERVERS"]!;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxProcessor started");
            var producerConfig = new ProducerConfig { BootstrapServers = _bootstrapServers };
            using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

                    var unsentMessages = await db.OutboxMessages
                        .Where(m => !m.Sent)
                        .Take(10)
                        .ToListAsync(stoppingToken);

                    _logger.LogInformation("Found {Count} unsent messages", unsentMessages.Count);

                    foreach (var message in unsentMessages)
                    {
                        try
                        {
                            string topic = message.Type switch
                            {
                                nameof(OrderPaymentRequested) => "order-payment-requests",
                                nameof(PaymentProcessed) => "order-payment-results",
                                _ => null!
                            };
                            if (topic == null)
                            {
                                _logger.LogWarning("Unknown message type: {Type}", message.Type);
                                continue;
                            }
                            await producer.ProduceAsync(topic, new Message<string, string>
                            {
                                Key = message.MessageId,
                                Value = message.Payload
                            }, stoppingToken);
                            message.Sent = true;
                            await db.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Published message: {MessageId} to {Topic}", message.MessageId, topic);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send outbox message: {MessageId}", message.MessageId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in outbox processor");
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
} 