using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using payments_service.Data;
using payments_service.Models;
using Confluent.Kafka;
using System.Text.Json;

namespace payments_service.Services
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
            var producerConfig = new ProducerConfig { BootstrapServers = _bootstrapServers };
            using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

                    var unsentMessages = await db.OutboxMessages
                        .Where(m => !m.Sent)
                        .Take(10)
                        .ToListAsync(stoppingToken);

                    foreach (var message in unsentMessages)
                    {
                        try
                        {
                            string topic = message.Type switch
                            {
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
                            _logger.LogInformation("Sent outbox message: {MessageId} of type {Type}", message.MessageId, message.Type);
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