using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using order_service.Data;
using order_service.Models;
using Shared.Contracts;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace order_service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersDbContext _db;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(OrdersDbContext db, ILogger<OrdersController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var order = new Order
            {
                UserId = Guid.Parse(request.CustomerId),
                Amount = request.Amount,
                Description = request.Description,
                Status = "NEW"
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            var outboxMessage = new OutboxMessage
            {
                Type = nameof(OrderPaymentRequested),
                Payload = JsonSerializer.Serialize(new OrderPaymentRequested
                {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    Amount = order.Amount,
                    Description = order.Description
                }),
                Sent = false,
                CreatedAt = DateTime.UtcNow
            };
            _db.OutboxMessages.Add(outboxMessage);
            _logger.LogInformation("[DEBUG] OutboxMessage added: Sent = false");

            await _db.SaveChangesAsync();
            _logger.LogInformation($"[DEBUG] OutboxMessage saved. Count: {_db.OutboxMessages.Count()} | Last Sent: {_db.OutboxMessages.OrderByDescending(x => x.Id).First().Sent}");

            return Ok(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] string? customerId = null)
        {
            var query = _db.Orders.AsQueryable();
            
            if (!string.IsNullOrEmpty(customerId))
            {
                query = query.Where(o => o.UserId == Guid.Parse(customerId));
            }

            var orders = await query.OrderByDescending(o => o.Id).ToListAsync();
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(string id)
        {
            if (!Guid.TryParse(id, out var orderId))
                return BadRequest("Invalid order ID format");
                
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound();

            return Ok(order);
        }
    }

    public class CreateOrderRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
} 