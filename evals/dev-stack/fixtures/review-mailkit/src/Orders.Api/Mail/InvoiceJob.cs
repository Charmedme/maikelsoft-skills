using Orders.Api.Customers;

namespace Orders.Api.Mail;

// Sends the monthly invoice mail on the first day of each month.
public sealed class InvoiceJob(ICustomerDirectory customers, InvoiceMail invoices, MailSender sender, TimeProvider time) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1), time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
            if (today.Day == 1)
            {
                await SendInvoicesAsync(today.AddMonths(-1), stoppingToken);
            }
        }
    }

    private async Task SendInvoicesAsync(DateOnly month, CancellationToken ct)
    {
        var all = await customers.GetAllAsync(ct);
        using var message = sender.NewMessage();
        foreach (var customer in all)
        {
            message.To.Add(new MimeKit.MailboxAddress(customer.Name, customer.Email));
        }

        message.Subject = $"Your invoice for {month:yyyy-MM}";
        message.Body = invoices.CreateBody(all[0], month, 0m);
        await sender.SendAsync(message, ct);
    }
}
