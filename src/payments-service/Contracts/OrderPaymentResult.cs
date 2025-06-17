namespace payments_service.Contracts
{
    public class OrderPaymentResult
    {
        public string OrderId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
} 