using Microsoft.EntityFrameworkCore;
using order_service.Models;

namespace order_service.Data
{
    public class OrdersDbContext : DbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

        public DbSet<Order> Orders { get; set; }
        public DbSet<InboxMessage> InboxMessages { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
    }
} 