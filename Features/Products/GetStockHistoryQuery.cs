using MediatR;
using SupermarketSystem.Api.DTOs;

namespace SupermarketSystem.Api.Services.Products;

public class GetStockHistoryQuery : IRequest<List<StockHistoryDto>>
{
    public int? ProductId { get; set; }
}