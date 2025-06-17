namespace Shared.Contracts;

public record OrderPaymentRequested
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public long Amount { get; init; }
    public string Description { get; init; } = string.Empty;
} 