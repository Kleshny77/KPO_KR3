namespace payments_service.Models
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }
        public long Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public Guid? ReferenceOrderId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 