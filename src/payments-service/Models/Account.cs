namespace payments_service.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }
} 