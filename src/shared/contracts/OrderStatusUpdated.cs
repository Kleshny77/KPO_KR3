namespace Shared.Contracts;

public record OrderStatusUpdated
{
    public Guid OrderId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
} 