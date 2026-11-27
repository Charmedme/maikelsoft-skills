namespace Kiosk.Api.Chatbot;

public static class ChatbotSetup
{
    public static IServiceCollection AddChatbot(this IServiceCollection services)
    {
        services.AddSingleton<IChatModel, ScriptedChatModel>();
        services.AddScoped<CouponService>();
        services.AddScoped<ChatbotService>();
        return services;
    }
}
