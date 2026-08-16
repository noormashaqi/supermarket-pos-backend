public record ExchangeItemDto(int ProductId, int Quantity);

public record ExchangeRequestBody(
    int OldProductId,
    int QuantityReturned,
    List<ExchangeItemDto> NewItems,
    string? Reason
);