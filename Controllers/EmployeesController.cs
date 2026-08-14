using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupermarketSystem.Api.Common;
using SupermarketSystem.Api.Constants;
using SupermarketSystem.Api.Features.Employees;

namespace SupermarketSystem.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmployeesController(IMediator mediator)
    {
        _mediator = mediator;
    }


    // GET /api/employees
    [HttpGet]
    [PermissionRequirement(PermissionKeys.EmployeesView)]
    public async Task<ActionResult<IEnumerable<EmployeeResponse>>> GetEmployees(
        CancellationToken cancellationToken)
    {
        var employees = await _mediator.Send(
            new GetEmployeesQuery(),
            cancellationToken);

        return Ok(employees);
    }


    // GET /api/employees/{id}
    [HttpGet("{id:long}")]
    [PermissionRequirement(PermissionKeys.EmployeesView)]
    public async Task<ActionResult<EmployeeResponse>> GetEmployeeById(
        long id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Employee id must be greater than zero."
            });
        }


        var employee = await _mediator.Send(
            new GetEmployeeByIdQuery(id),
            cancellationToken);


        if (employee is null)
        {
            return NotFound(new
            {
                message = "Employee not found."
            });
        }


        return Ok(employee);
    }



    // POST /api/employees
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [PermissionRequirement(PermissionKeys.EmployeesCreate)]
    public async Task<ActionResult<EmployeeResponse>> CreateEmployee(
        CreateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            command,
            cancellationToken);


        if (!result.Success)
        {
            if(result.ErrorCode == "UsernameAlreadyExists")
            {
                return Conflict(new
                {
                    message = result.Message
                });
            }

            return BadRequest(new
            {
                message = result.Message
            });
        }


        return CreatedAtAction(
            nameof(GetEmployeeById),
            new { id = result.Employee!.Id },
            result.Employee);
    }



    // PUT /api/employees/{id}
    [HttpPut("{id:long}")]
    [PermissionRequirement(PermissionKeys.EmployeesUpdate)]
    public async Task<ActionResult<EmployeeResponse>> UpdateEmployee(
        long id,
        UpdateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;


        var result = await _mediator.Send(
            command,
            cancellationToken);


        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Message
            });
        }


        return Ok(result.Employee);
    }



    // PATCH /api/employees/{id}/deactivate
    [HttpPatch("{id:long}/deactivate")]
    [Authorize(Roles = "Admin")]
    [PermissionRequirement(PermissionKeys.EmployeesDeactivate)]
    public async Task<IActionResult> DeactivateEmployee(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SetEmployeeStatusCommand(id, false, GetCurrentEmployeeId()),
            cancellationToken);


        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Message
            });
        }


        return Ok(new
        {
            message = result.Message
        });
    }

    // PATCH /api/employees/{id}/activate
    [HttpPatch("{id:long}/activate")]
    [Authorize(Roles = "Admin")]
    [PermissionRequirement(PermissionKeys.EmployeesDeactivate)]
    public async Task<IActionResult> ActivateEmployee(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SetEmployeeStatusCommand(id, true, GetCurrentEmployeeId()),
            cancellationToken);


        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Message
            });
        }


        return Ok(new
        {
            message = result.Message
        });
    }

    // PATCH /api/employees/{id}/status
    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = "Admin")]
    [PermissionRequirement(PermissionKeys.EmployeesDeactivate)]
    public async Task<IActionResult> SetEmployeeStatus(
        long id,
        [FromBody] SetEmployeeStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SetEmployeeStatusCommand(id, request.IsActive, GetCurrentEmployeeId()),
            cancellationToken);


        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Message
            });
        }


        return Ok(new
        {
            message = result.Message
        });
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
