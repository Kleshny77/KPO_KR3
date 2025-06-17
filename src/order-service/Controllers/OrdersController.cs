using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using order_service.Data;
using order_service.Models;
using order_service.Services;
using order_service.Contracts;
using MassTransit;

namespace order_service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersDbContext _db;
        private readonly IPublishEndpoint _publishEndpoint;

        public OrdersController(OrdersDbContext db, IPublishEndpoint publishEndpoint)
        {
            _db = db;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var order = new Order
            {
                UserId = request.CustomerId,
                Amount = request.Amount,
                Description = request.Description,
                Status = "NEW"
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // Отправляем событие для обработки платежа
            await _publishEndpoint.Publish(new OrderPaymentRequested
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Amount = order.Amount
            });

            return Ok(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] string? customerId = null)
        {
            var query = _db.Orders.AsQueryable();
            
            if (!string.IsNullOrEmpty(customerId))
            {
                query = query.Where(o => o.UserId == customerId);
            }

            var orders = await query.OrderByDescending(o => o.Id).ToListAsync();
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(string id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            return Ok(order);
        }
    }

    public class CreateOrderRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
} 