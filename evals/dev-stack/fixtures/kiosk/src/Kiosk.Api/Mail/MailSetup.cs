namespace Kiosk.Api.Mail;

public static class MailSetup
{
    public static IServiceCollection AddMail(this IServiceCollection services)
    {
        services.AddScoped<IEmailSender, OutboxEmailSender>();
        return services;
    }
}
