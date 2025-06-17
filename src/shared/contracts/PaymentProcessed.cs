namespace Shared.Contracts;

public record PaymentProcessed
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public long Amount { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
} 