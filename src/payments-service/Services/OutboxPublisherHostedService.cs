using MassTransit;
using Microsoft.EntityFrameworkCore;
using payments_service.Data;
using payments_service.Models;

namespace payments_service.Services
{
    public class OutboxPublisherHostedService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<OutboxPublisherHostedService> _logger;
        public OutboxPublisherHostedService(IServiceProvider sp, ILogger<OutboxPublisherHostedService> logger)
        {
            _sp = sp;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
                var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                var outbox = await db.OutboxMessages.Where(x => !x.Sent).ToListAsync();
                foreach (var msg in outbox)
                {
                    try
                    {
                        var type = Type.GetType($"payments_service.Contracts.{msg.Type}");
                        if (type != null)
                        {
                            var payload = System.Text.Json.JsonSerializer.Deserialize(msg.Payload, type);
                            await bus.Publish(payload!);
                            msg.Sent = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish outbox message {Id}", msg.Id);
                    }
                }
                await db.SaveChangesAsync();
                await Task.Delay(1000, stoppingToken); // 1 секунда
            }
        }
    }
} 