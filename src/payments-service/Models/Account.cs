namespace payments_service.Models
{
    public class Account
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public long Balance { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
} 