using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupermarketSystem.Api.Common;
using SupermarketSystem.Api.Constants;
using SupermarketSystem.Api.Features.Customers.Create;
using SupermarketSystem.Api.Features.Customers.Read;
using SupermarketSystem.Api.Features.Customers.Payment;

namespace SupermarketSystem.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [PermissionRequirement(PermissionKeys.CustomersCreate)]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [PermissionRequirement(PermissionKeys.CustomersView)]
    public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
    {
        var customers = await _mediator.Send(new GetCustomersQuery(), cancellationToken);
        return Ok(customers);
    }

    [HttpPost("{id:long}/payments")]
    [PermissionRequirement(PermissionKeys.CustomersRecordPayment)]
    public async Task<IActionResult> RecordPayment(
        long id,
        [FromBody] RecordPaymentRequestBody body,
        CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId is null)
            return Unauthorized();

        var command = new RecordPaymentCommand(id, body.Amount, (int)employeeId.Value, body.Notes);
        var result = await _mediator.Send(command, cancellationToken);
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

public record RecordPaymentRequestBody(decimal Amount, string? Notes);
