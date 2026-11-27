using Kiosk.Api.Hosting;

namespace Kiosk.Api.Chatbot;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chat", async (ChatRequest request, HttpContext http, ChatbotService chatbot, CancellationToken ct) =>
            string.IsNullOrWhiteSpace(request.Message)
                ? Results.ValidationProblem(new Dictionary<string, string[]> { ["message"] = ["Write a message."] })
                : Results.Ok(await chatbot.ReplyAsync(http.User.GetUserId(), request, ct)))
            .RequireAuthorization();
        return app;
    }
}
