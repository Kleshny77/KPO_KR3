namespace payments_service.Models
{
    public class InboxMessage
    {
        public int Id { get; set; }
        public string MessageId { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
    }
} 