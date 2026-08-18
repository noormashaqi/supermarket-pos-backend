using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupermarketSystem.Api.Common;
using SupermarketSystem.Api.Constants;
using SupermarketSystem.Api.Features.Invoices.Create;
using SupermarketSystem.Api.Features.Invoices.Read;
using SupermarketSystem.Api.Features.Returns.PureReturn;
using SupermarketSystem.Api.Features.Returns.Exchange;
using SupermarketSystem.Api.Features.Invoices.Hold;

namespace SupermarketSystem.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [PermissionRequirement(PermissionKeys.InvoicesCreate)]
    public async Task<IActionResult> CreateInvoice(
        [FromBody] CreateInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId is null)
            return Unauthorized();

        var authenticatedCommand = command with { EmployeeId = (int)employeeId.Value };
        var result = await _mediator.Send(authenticatedCommand, cancellationToken);
        return CreatedAtAction(nameof(GetInvoiceById), new { id = result.InvoiceId }, result);
    }

    [HttpGet("{id:long}")]
    [PermissionRequirement(PermissionKeys.InvoicesView)]
    public async Task<IActionResult> GetInvoiceById(long id, CancellationToken cancellationToken)
    {
        var invoice = await _mediator.Send(new GetInvoiceByIdQuery(id), cancellationToken);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpGet("{id:long}/printable")]
    [PermissionRequirement(PermissionKeys.InvoicesView)]
    public async Task<IActionResult> GetPrintableInvoice(long id, CancellationToken cancellationToken)
    {
        var printable = await _mediator.Send(new GetPrintableInvoiceQuery(id), cancellationToken);
        return printable is null ? NotFound(new { message = $"Invoice with id {id} not found." }) : Ok(printable);
    }

    [HttpGet("{id:long}/print")]
    [PermissionRequirement(PermissionKeys.InvoicesView)]
    public async Task<IActionResult> PrintInvoiceHtml(long id, CancellationToken cancellationToken)
    {
        var printable = await _mediator.Send(new GetPrintableInvoiceQuery(id), cancellationToken);
        if (printable is null)
            return NotFound("<h3>Invoice not found.</h3>");

        return Content(printable.HtmlReceipt, "text/html; charset=utf-8");
    }

    [HttpGet]
    [PermissionRequirement(PermissionKeys.InvoicesView)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] DateTime? date,
        [FromQuery] int? employeeId,
        [FromQuery] int? productId,
        CancellationToken cancellationToken)
    {
        var invoices = await _mediator.Send(new GetInvoicesQuery(date, employeeId, productId), cancellationToken);
        return Ok(invoices);
    }

    [HttpPost("{id:long}/return")]
    [PermissionRequirement(PermissionKeys.InvoicesReturn)]
    public async Task<IActionResult> PureReturn(
        long id,
        [FromBody] PureReturnRequestBody body,
        CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId is null)
            return Unauthorized();

        var command = new PureReturnCommand(id, body.ProductId, body.QuantityReturned, employeeId.Value, body.Reason);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:long}/exchange")]
    [PermissionRequirement(PermissionKeys.InvoicesExchange)]
    public async Task<IActionResult> Exchange(
        long id,
        [FromBody] ExchangeRequestBody body,
        CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId is null)
            return Unauthorized();

        // تحويل قائمة الأصناف من ExchangeItemDto إلى ExchangeItemInput
        var newItems = body.NewItems?.Select(item => new ExchangeItemInput(item.ProductId, item.Quantity)).ToList()
                       ?? new List<ExchangeItemInput>();

        var command = new ExchangeCommand(
            id,
            body.OldProductId,
            body.QuantityReturned,
            newItems,
            employeeId.Value,
            body.Reason);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("hold")]
    [PermissionRequirement(PermissionKeys.InvoicesCreate)]
    public async Task<IActionResult> HoldInvoice(
        [FromBody] HoldInvoiceRequestBody body,
        CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId is null)
            return Unauthorized();

        var command = new HoldInvoiceCommand(
            employeeId.Value,
            body.ReferenceTag,
            body.CustomerName,
            body.DiscountPercentage,
            body.CartState
        );
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("held")]
    [PermissionRequirement(PermissionKeys.InvoicesView)]
    public async Task<IActionResult> GetHeldInvoices(CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId is null)
            return Unauthorized();

        var query = new GetHeldInvoicesQuery(employeeId.Value);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("hold/{id:long}")]
    [PermissionRequirement(PermissionKeys.InvoicesCreate)]
    public async Task<IActionResult> DeleteHeldInvoice(long id, CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId is null)
            return Unauthorized();

        var command = new DeleteHeldInvoiceCommand(id, employeeId.Value);
        var success = await _mediator.Send(command, cancellationToken);
        if (!success)
            return NotFound(new { message = $"Held invoice with id {id} not found or belongs to another cashier." });

        return NoContent();
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

public record HoldInvoiceRequestBody(
    string ReferenceTag,
    string? CustomerName,
    string? DiscountPercentage,
    string CartState
);