namespace order_service.Models
{
    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public long Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "NEW";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}   