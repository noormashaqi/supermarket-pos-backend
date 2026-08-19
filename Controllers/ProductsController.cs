using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupermarketSystem.Api.Common;
using SupermarketSystem.Api.Constants;
using SupermarketSystem.Api.Services.Products;

namespace SupermarketSystem.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [PermissionRequirement(PermissionKeys.ProductsView)]
    public async Task<IActionResult> GetAll([FromQuery] int? categoryId, [FromQuery] bool activeOnly = false)
    {
        var result = await _mediator.Send(new GetProductsQuery
        {
            CategoryId = categoryId,
            ActiveOnly = activeOnly
        });
        return Ok(result);
    }

    [HttpGet("low-stock")]
    [PermissionRequirement(PermissionKeys.ProductsView)]
    public async Task<IActionResult> GetLowStock()
    {
        var result = await _mediator.Send(new GetLowStockProductsQuery());
        return Ok(result);
    }

    [HttpGet("out-of-stock")]
    [PermissionRequirement(PermissionKeys.ProductsView)]
    public async Task<IActionResult> GetOutOfStock()
    {
        var result = await _mediator.Send(new GetOutOfStockProductsQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    [PermissionRequirement(PermissionKeys.ProductsView)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery { Id = id });
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [PermissionRequirement(PermissionKeys.ProductsCreate)]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId is null)
            return Unauthorized();

        command.EmployeeId = employeeId.Value;
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [PermissionRequirement(PermissionKeys.ProductsUpdate)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPatch("{id}/deactivate")]
    [HttpPut("{id}/deactivate")]
    [HttpDelete("{id}")]
    [PermissionRequirement(PermissionKeys.ProductsDeactivate)]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _mediator.Send(new DeactivateProductCommand { Id = id });
        var updatedProduct = await _mediator.Send(new GetProductByIdQuery { Id = id });
        return Ok(updatedProduct ?? (object)new { id, isActive = false });
    }

    [HttpPatch("{id}/activate")]
    [HttpPut("{id}/activate")]
    [HttpPost("{id}/activate")]
    [PermissionRequirement(PermissionKeys.ProductsUpdate)]
    public async Task<IActionResult> Activate(int id)
    {
        await _mediator.Send(new ActivateProductCommand { Id = id });
        var updatedProduct = await _mediator.Send(new GetProductByIdQuery { Id = id });
        return Ok(updatedProduct ?? (object)new { id, isActive = true });
    }

    [HttpPost("{id}/stock/add")]
    [PermissionRequirement(PermissionKeys.ProductsStockAdd)]
    public async Task<IActionResult> AddStock(int id, [FromBody] AddStockCommand command)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId is null)
            return Unauthorized();

        command.ProductId = id;
        command.EmployeeId = employeeId.Value;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("stock-history")]
    [PermissionRequirement(PermissionKeys.ProductsView)]
    public async Task<IActionResult> GetAllStockHistory([FromQuery] int? productId)
    {
        var result = await _mediator.Send(new GetStockHistoryQuery { ProductId = productId });
        return Ok(result);
    }

    [HttpGet("{id}/stock/history")]
    [PermissionRequirement(PermissionKeys.ProductsView)]
    public async Task<IActionResult> GetStockHistory(int id)
    {
        var result = await _mediator.Send(new GetStockHistoryQuery { ProductId = id });
        return Ok(result);
    }

    private long? GetCurrentEmployeeId()
    {
        var employeeIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirst(ClaimTypes.NameIdentifier);

        return employeeIdClaim is not null && long.TryParse(employeeIdClaim.Value, out var employeeId)
            ? employeeId
            : null;
    }
}