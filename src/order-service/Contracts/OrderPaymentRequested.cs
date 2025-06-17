namespace order_service.Contracts
{
    public class OrderPaymentRequested
    {
        public string OrderId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
} 