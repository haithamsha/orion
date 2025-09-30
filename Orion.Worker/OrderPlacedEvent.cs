// This is the contract between the publisher and consumer
public record OrderPlacedEvent(int OrderId, string CustomerName, decimal TotalAmount);