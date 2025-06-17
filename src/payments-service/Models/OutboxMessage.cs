namespace payments_service.Models
{
    public class OutboxMessage
    {
        public int Id { get; set; }
        public string MessageId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool Sent { get; set; }
    }
} 