using System.Text.Json;

namespace Kiosk.Api.Chatbot;

public sealed record ChatMessage(string Role, string Content);

public sealed record ChatPrompt(string System, IReadOnlyList<ChatMessage> Messages);

public sealed record ChatToolDefinition(string Name, string Description);

public sealed record ChatToolCall(string Name, IReadOnlyDictionary<string, JsonElement> Arguments);

public sealed record ChatModelReply(string Text, IReadOnlyList<ChatToolCall> ToolCalls);

/// <summary>The language model behind the support chat.</summary>
public interface IChatModel
{
    Task<ChatModelReply> CompleteAsync(ChatPrompt prompt, IReadOnlyList<ChatToolDefinition> tools, CancellationToken ct);
}
