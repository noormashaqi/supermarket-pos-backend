using Dapper;
using MediatR;
using SupermarketSystem.Api.Interface;
using System.Text;

namespace SupermarketSystem.Api.Features.Invoices.Read;

public class GetInvoicesHandler : IRequestHandler<GetInvoicesQuery, List<InvoiceListItemDto>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetInvoicesHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<InvoiceListItemDto>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = new StringBuilder(@"
            SELECT DISTINCT i.Id, i.InvoiceNumber, i.EmployeeId, e.FullName AS EmployeeName,
                   i.Date, i.TotalAfterDiscount, i.HasReturn, i.PaymentMethod, i.PaymentStatus
            FROM Invoices i
            JOIN Employees e ON e.Id = i.EmployeeId");

        if (request.ProductId.HasValue)
            sql.Append(" JOIN InvoiceItems ii ON ii.InvoiceId = i.Id");

        sql.Append(" WHERE 1 = 1");

        if (request.Date.HasValue)
            sql.Append(" AND DATE(i.Date) = @Date");

        if (request.EmployeeId.HasValue)
            sql.Append(" AND i.EmployeeId = @EmployeeId");

        if (request.ProductId.HasValue)
            sql.Append(" AND ii.ProductId = @ProductId");

        sql.Append(" ORDER BY i.Date DESC");

        var results = await connection.QueryAsync<InvoiceListItemDto>(
            new CommandDefinition(
                sql.ToString(),
                new
                {
                    Date = request.Date?.Date,
                    request.EmployeeId,
                    request.ProductId
                },
                cancellationToken: cancellationToken));

        return results.ToList();
    }
}