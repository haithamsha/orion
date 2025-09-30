// This is the contract between the publisher (API) and consumer (Worker)
// It must match the OrderPlacedEvent record in the API project
public record OrderPlacedEvent(int OrderId, string UserId, string CustomerName, decimal TotalAmount);