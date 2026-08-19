using MediatR;

namespace SupermarketSystem.Api.Services.Products;

public class ActivateProductCommand : IRequest<Unit>
{
    public int Id { get; set; }
}
