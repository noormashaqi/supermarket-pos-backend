using System.Net;
using System.Text;
using Dapper;
using MediatR;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Features.Invoices.Read;

public class GetPrintableInvoiceHandler : IRequestHandler<GetPrintableInvoiceQuery, PrintableInvoiceDto?>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetPrintableInvoiceHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PrintableInvoiceDto?> Handle(GetPrintableInvoiceQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        var invoiceHeader = await connection.QuerySingleOrDefaultAsync<InvoiceHeaderDto>(
            new CommandDefinition(
                @"SELECT i.Id AS InvoiceId, i.InvoiceNumber, COALESCE(e.FullName, 'Staff') AS EmployeeName,
                         i.Date, i.TotalBeforeDiscount, i.DiscountPercentage, i.TotalAfterDiscount, i.HasReturn,
                         i.PaymentMethod, i.PaymentStatus, i.CustomerId,
                         COALESCE(c.Nickname, c.FullName) AS CustomerNickname,
                         c.CurrentBalance AS OutstandingBalance
                  FROM Invoices i
                  LEFT JOIN Employees e ON e.Id = i.EmployeeId
                  LEFT JOIN Customers c ON c.Id = i.CustomerId
                  WHERE i.Id = @Id",
                new { request.Id },
                cancellationToken: cancellationToken));

        if (invoiceHeader is null)
            return null;

        var items = (await connection.QueryAsync<PrintableInvoiceItemDto>(
            new CommandDefinition(
                @"SELECT ProductId, ProductNameSnapshot AS ProductName, UnitPriceSnapshot AS UnitPrice, Quantity, LineTotal
                  FROM InvoiceItems
                  WHERE InvoiceId = @Id",
                new { request.Id },
                cancellationToken: cancellationToken))).ToList();

        var discountAmount = invoiceHeader.TotalBeforeDiscount - invoiceHeader.TotalAfterDiscount;
        var encodedInvoiceNumber = WebUtility.HtmlEncode(invoiceHeader.InvoiceNumber);
        var encodedEmployeeName = WebUtility.HtmlEncode(invoiceHeader.EmployeeName);
        var isDebt = string.Equals(invoiceHeader.PaymentMethod, "Debt", StringComparison.OrdinalIgnoreCase);

        var htmlBuilder = new StringBuilder();
        htmlBuilder.AppendLine("<!DOCTYPE html>");
        htmlBuilder.AppendLine("<html dir='ltr' lang='en'>");
        htmlBuilder.AppendLine("<head>");
        htmlBuilder.AppendLine("    <meta charset='UTF-8'>");
        htmlBuilder.AppendLine($"    <title>Invoice #{encodedInvoiceNumber}</title>");
        htmlBuilder.AppendLine("    <style>");
        htmlBuilder.AppendLine("        body { font-family: 'Courier New', Courier, monospace, sans-serif; width: 80mm; margin: 0 auto; padding: 10px; color: #000; background: #fff; font-size: 13px; }");
        htmlBuilder.AppendLine("        .text-center { text-align: center; }");
        htmlBuilder.AppendLine("        .text-right { text-align: right; }");
        htmlBuilder.AppendLine("        .text-left { text-align: left; }");
        htmlBuilder.AppendLine("        .header { border-bottom: 2px dashed #000; padding-bottom: 8px; margin-bottom: 8px; }");
        htmlBuilder.AppendLine("        .header h2 { margin: 4px 0; font-size: 18px; }");
        htmlBuilder.AppendLine("        .inv-number { font-size: 16px; font-weight: bold; margin: 6px 0; background: #eee; padding: 4px; border: 1px solid #ccc; }");
        htmlBuilder.AppendLine("        .info-table { width: 100%; margin-bottom: 8px; font-size: 12px; }");
        htmlBuilder.AppendLine("        .info-table td { padding: 2px 0; }");
        htmlBuilder.AppendLine("        .items-table { width: 100%; border-collapse: collapse; margin: 8px 0; }");
        htmlBuilder.AppendLine("        .items-table th { border-bottom: 1px solid #000; border-top: 1px solid #000; text-align: left; padding: 4px 2px; font-size: 12px; }");
        htmlBuilder.AppendLine("        .items-table td { padding: 4px 2px; border-bottom: 1px dotted #ccc; vertical-align: top; }");
        htmlBuilder.AppendLine("        .totals-table { width: 100%; margin-top: 8px; border-top: 2px dashed #000; padding-top: 6px; }");
        htmlBuilder.AppendLine("        .totals-table td { padding: 3px 0; }");
        htmlBuilder.AppendLine("        .net-total { font-size: 16px; font-weight: bold; border-top: 1px solid #000; border-bottom: 2px solid #000; padding: 6px 0; }");
        htmlBuilder.AppendLine("        .debt-info { margin-top: 8px; padding: 6px; border: 2px solid #000; background: #fff3cd; font-size: 12px; }");
        htmlBuilder.AppendLine("        .footer { margin-top: 12px; border-top: 1px dashed #000; padding-top: 8px; font-size: 11px; }");
        htmlBuilder.AppendLine("        @media print { body { width: 100%; margin: 0; padding: 0; } .no-print { display: none; } }");
        htmlBuilder.AppendLine("    </style>");
        htmlBuilder.AppendLine("</head>");
        htmlBuilder.AppendLine("<body>");

        htmlBuilder.AppendLine("    <div class='no-print' style='text-align: center; margin-bottom: 12px;'>");
        htmlBuilder.AppendLine("        <button onclick='window.print()' style='padding: 8px 16px; font-size: 14px; cursor: pointer; background: #007bff; color: #fff; border: none; border-radius: 4px;'>🖨️ Print Receipt</button>");
        htmlBuilder.AppendLine("    </div>");

        htmlBuilder.AppendLine("    <div class='header text-center'>");
        htmlBuilder.AppendLine("        <h2>SUPERMARKET</h2>");
        htmlBuilder.AppendLine("        <div>Sales Receipt</div>");
        htmlBuilder.AppendLine($"        <div class='inv-number'>Invoice #{encodedInvoiceNumber}</div>");
        htmlBuilder.AppendLine("    </div>");

        htmlBuilder.AppendLine("    <table class='info-table'>");
        htmlBuilder.AppendLine($"        <tr><td><strong>Date & Time:</strong></td><td class='text-right'>{invoiceHeader.Date:yyyy-MM-dd HH:mm:ss}</td></tr>");
        htmlBuilder.AppendLine($"        <tr><td><strong>Cashier:</strong></td><td class='text-right'>{encodedEmployeeName}</td></tr>");
        htmlBuilder.AppendLine($"        <tr><td><strong>Payment Method:</strong></td><td class='text-right'>{(isDebt ? "DEBT" : "Cash")}</td></tr>");

        if (isDebt && invoiceHeader.CustomerNickname is not null)
        {
            var encodedCustomerName = WebUtility.HtmlEncode(invoiceHeader.CustomerNickname);
            htmlBuilder.AppendLine($"        <tr><td><strong>Customer:</strong></td><td class='text-right'>{encodedCustomerName}</td></tr>");
        }

        htmlBuilder.AppendLine("    </table>");

        htmlBuilder.AppendLine("    <table class='items-table'>");
        htmlBuilder.AppendLine("        <thead>");
        htmlBuilder.AppendLine("            <tr>");
        htmlBuilder.AppendLine("                <th style='width: 45%;'>Item</th>");
        htmlBuilder.AppendLine("                <th class='text-center' style='width: 15%;'>Qty</th>");
        htmlBuilder.AppendLine("                <th class='text-right' style='width: 20%;'>Price</th>");
        htmlBuilder.AppendLine("                <th class='text-right' style='width: 20%;'>Total</th>");
        htmlBuilder.AppendLine("            </tr>");
        htmlBuilder.AppendLine("        </thead>");
        htmlBuilder.AppendLine("        <tbody>");

        foreach (var item in items)
        {
            var encodedProductName = WebUtility.HtmlEncode(item.ProductName);
            htmlBuilder.AppendLine("            <tr>");
            htmlBuilder.AppendLine($"                <td>{encodedProductName}</td>");
            htmlBuilder.AppendLine($"                <td class='text-center'>{item.Quantity}</td>");
            htmlBuilder.AppendLine($"                <td class='text-right'>{item.UnitPrice:N2}</td>");
            htmlBuilder.AppendLine($"                <td class='text-right'>{item.LineTotal:N2}</td>");
            htmlBuilder.AppendLine("            </tr>");
        }

        htmlBuilder.AppendLine("        </tbody>");
        htmlBuilder.AppendLine("    </table>");

        htmlBuilder.AppendLine("    <table class='totals-table'>");
        htmlBuilder.AppendLine($"        <tr><td>Subtotal:</td><td class='text-right'>{invoiceHeader.TotalBeforeDiscount:N2}</td></tr>");

        if (invoiceHeader.DiscountPercentage > 0)
        {
            htmlBuilder.AppendLine($"        <tr><td>Discount (%):</td><td class='text-right'>{invoiceHeader.DiscountPercentage:N2}%</td></tr>");
            htmlBuilder.AppendLine($"        <tr><td>Discount Amount:</td><td class='text-right'>-{discountAmount:N2}</td></tr>");
        }

        var totalLabel = isDebt ? "Total (DEBT)" : "Total Payable (Cash)";
        htmlBuilder.AppendLine($"        <tr class='net-total'><td>{totalLabel}:</td><td class='text-right'>{invoiceHeader.TotalAfterDiscount:N2}</td></tr>");
        htmlBuilder.AppendLine("    </table>");

        // Debt info section
        if (isDebt && invoiceHeader.OutstandingBalance.HasValue)
        {
            htmlBuilder.AppendLine("    <div class='debt-info'>");
            htmlBuilder.AppendLine($"        <strong>Payment Status:</strong> DEBT<br>");
            if (invoiceHeader.CustomerNickname is not null)
                htmlBuilder.AppendLine($"        <strong>Customer:</strong> {WebUtility.HtmlEncode(invoiceHeader.CustomerNickname)}<br>");
            htmlBuilder.AppendLine($"        <strong>Outstanding Balance:</strong> {invoiceHeader.OutstandingBalance.Value:N2}");
            htmlBuilder.AppendLine("    </div>");
        }

        htmlBuilder.AppendLine("    <div class='footer text-center'>");
        htmlBuilder.AppendLine("        <p>Thank you for shopping with us!</p>");
        htmlBuilder.AppendLine("        <p>Please keep this receipt for returns or exchanges.</p>");
        htmlBuilder.AppendLine("    </div>");

        htmlBuilder.AppendLine("    <script>");
        htmlBuilder.AppendLine("        window.onload = function() { window.print(); };");
        htmlBuilder.AppendLine("    </script>");

        htmlBuilder.AppendLine("</body>");
        htmlBuilder.AppendLine("</html>");

        return new PrintableInvoiceDto
        {
            InvoiceId = invoiceHeader.InvoiceId,
            InvoiceNumber = invoiceHeader.InvoiceNumber,
            EmployeeName = invoiceHeader.EmployeeName,
            Date = invoiceHeader.Date,
            PaymentMethod = invoiceHeader.PaymentMethod,
            PaymentStatus = invoiceHeader.PaymentStatus,
            CustomerNickname = invoiceHeader.CustomerNickname,
            OutstandingBalance = invoiceHeader.OutstandingBalance,
            TotalBeforeDiscount = invoiceHeader.TotalBeforeDiscount,
            DiscountPercentage = invoiceHeader.DiscountPercentage,
            DiscountAmount = discountAmount,
            TotalAfterDiscount = invoiceHeader.TotalAfterDiscount,
            HasReturn = invoiceHeader.HasReturn,
            Items = items,
            HtmlReceipt = htmlBuilder.ToString()
        };
    }

    private class InvoiceHeaderDto
    {
        public long InvoiceId { get; init; }
        public string InvoiceNumber { get; init; } = string.Empty;
        public string EmployeeName { get; init; } = string.Empty;
        public DateTime Date { get; init; }
        public decimal TotalBeforeDiscount { get; init; }
        public decimal DiscountPercentage { get; init; }
        public decimal TotalAfterDiscount { get; init; }
        public bool HasReturn { get; init; }
        public string PaymentMethod { get; init; } = "Cash";
        public string PaymentStatus { get; init; } = "Paid";
        public long? CustomerId { get; init; }
        public string? CustomerNickname { get; init; }
        public decimal? OutstandingBalance { get; init; }
    }
}
