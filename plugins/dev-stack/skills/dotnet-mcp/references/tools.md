# Tools, prompts, and resources

The detail of rules 9 to 14a and 19 of `SKILL.md`. Each code block compiled and ran on SDK 11.0.100 with `ModelContextProtocol` 2.4.0.

## Contents

- Tool design
- Annotations
- Structured output
- Errors
- Progress and cancellation
- User confirmation with MRTR
- Prompts and resources
- Completions, filters, and extensions

## Tool design

- One tool does one task. Name it in `snake_case` with a verb first: `get_order_status`, `cancel_order`. The client shows the name, and the model selects by name and description.
- The description says what the tool does, what it returns, and when not to use it. Example: "Get the status of one order by its ID. Use search_orders to find an ID."
- Give each parameter a type that limits the value: `DateOnly`, an `enum`, a `Guid`, an `int` with a range. A `string` is the last choice. The SDK makes the input schema from the parameter types.
- Do not take a free-form `Dictionary<string, object>` or a JSON string as an argument. A generic argument bag lets the model set fields that the code did not plan for. In one CVE, the model set the host and the proxy of the HTTP client this way.
- Register services in DI and inject them. A tool class gets a new instance for each call. Keep state in a store, not in a field of the tool class.
- `WithToolsFromAssembly()` registers each `[McpServerToolType]` class. In a test host, use `WithTools<T>()` for one class.

## Annotations

The client uses the annotations to decide when to ask the human before a call. The defaults of `[McpServerTool]` assume the worst case.

| Property | Default | Set it when |
|----------|---------|-------------|
| `ReadOnly` | `false` | The tool changes nothing: set `true` |
| `Destructive` | `true` | The tool only adds data: set `false`. Keep `true` for cancel, delete, overwrite, pay, send |
| `Idempotent` | `false` | A second call with the same arguments has no new effect: set `true` |
| `OpenWorld` | `true` | The tool reaches only your own data: set `false` |
| `Title` | none | A display name for the human |

An annotation is a hint for the client. It does not protect anything. The server still checks the permission (rule 18) and asks for the confirmation (rule 14a).

## Structured output

With `UseStructuredContent = true`, the SDK sends an `outputSchema` in `tools/list` and the result in `structuredContent`. A client or a program can then read typed JSON.

The SDK marks each property as required. Its serializer drops a property with a `null` value. The run gave this schema and this result:

```text
outputSchema: {"type":"object","properties":{"orderId":...,"status":...,"shippedAt":{"type":["string","null"],...}},"required":["orderId","status","shippedAt"]}
structured:   {"orderId":"1","status":"open"}
```

A client that validates the result rejects it. Keep the `null` in the JSON:

```csharp
public sealed record OrderStatus(
    string OrderId,
    string Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] DateTimeOffset? ShippedAt);
```

The result then has `"shippedAt":null`. From 2.0, a tool that returns a number or a string sends the value as is (`72`), not `{ "result": 72 }`. A client must read the value by the output schema.

## Errors

- Throw `McpException` with a message that helps the model: what failed and what to do next. The client gets `isError: true` and the text "An error occurred invoking 'get_order_status': Order 9 not found."
- Each other exception gives a generic message. Log the detail on the server (stderr in stdio).
- Do not put a stack trace, a SQL text, or a secret in an `McpException` message. The model and the client show it.

## Progress and cancellation

```csharp
[McpServerTool(Name = "export_orders", ReadOnly = true, OpenWorld = false)]
[Description("Export the orders of one day as CSV.")]
public async Task<string> Export(
    DateOnly day,
    IProgress<ProgressNotificationValue> progress,
    CancellationToken cancellationToken)
{
    var pages = await store.CountPagesAsync(day, cancellationToken);
    for (var page = 1; page <= pages; page++)
    {
        await store.ExportPageAsync(day, page, cancellationToken);
        progress.Report(new ProgressNotificationValue { Progress = page, Total = pages });
    }
    return await store.ReadExportAsync(day, cancellationToken);
}
```

- The client cancels a call with a notification. The SDK then cancels the token. Pass the token to each async call.
- Progress works in stdio and in stateless HTTP.
- For work that takes minutes, the Tasks extension (`ModelContextProtocol.Extensions.Tasks`) lets the client poll. Ask the human before you add the package (stack rule 5).

## User confirmation with MRTR

Spec 2026-07-28 replaced the server-to-client requests with multi round-trip requests (MRTR). This applies to elicitation, sampling, and roots. The tool returns "input required". The client asks the human and sends the request again with the answers.

A `confirm: true` argument is no confirmation. The model fills each argument, also `confirm`. A token that the tool returns to the model is no confirmation either: the model sends it back alone. Only an elicitation reaches the human, because the client shows it in its own UI.

```csharp
[McpServerTool(Name = "cancel_order", Destructive = true, Idempotent = true, OpenWorld = false)]
[Description("Cancel one open order. The client asks the user to confirm.")]
public async Task<string> CancelOrder(
    [Description("The order ID.")] string orderId,
    McpServer server,
    RequestContext<CallToolRequestParams> context,
    CancellationToken cancellationToken)
{
    if (context.Params?.InputResponses?.TryGetValue("confirm", out var response) is true)
    {
        var result = response.Deserialize(InputResponse.ElicitResultJsonTypeInfo);
        if (result?.IsAccepted is not true || context.Params.RequestState != orderId)
            return $"Order {orderId} was not cancelled.";
        await store.CancelAsync(orderId, cancellationToken);
        return $"Order {orderId} is cancelled.";
    }

    if (!server.IsMrtrSupported)
        return "This client cannot ask the user to confirm. Cancel the order in the order app.";

    throw new InputRequiredException(
        new Dictionary<string, InputRequest>
        {
            ["confirm"] = InputRequest.ForElicitation(new ElicitRequestParams
            {
                Message = $"Cancel order {orderId}?",
                RequestedSchema = new(), // a yes or no question needs an empty schema
            }),
        },
        requestState: orderId);
}
```

- `RequestState` comes back unchanged from the client. Compare it with the argument, so that an answer for one order does not cancel another. The client can read the state, so do not put a secret in it. Sign it or keep it short when it carries more than an ID.
- A client without MRTR support gets a text, not the action. Do not fall back to "cancel without a question".
- On stdio, `server.ElicitAsync` still works. Use `InputRequiredException` anyway: the same tool then works on HTTP.
- A test: the in-memory client with an `ElicitationHandler` that returns `accept` cancels the order. A handler that returns `decline` leaves it open.

## Prompts and resources

```csharp
[McpServerPromptType]
public static class OrderPrompts
{
    [McpServerPrompt(Name = "summarize_order"), Description("Summarize one order for a support mail.")]
    public static string Summarize([Description("The order ID.")] string orderId) =>
        $"Summarize order {orderId} for the customer.";
}

[McpServerResourceType]
public static class OrderResources
{
    [McpServerResource(UriTemplate = "orders://{orderId}", Name = "order", MimeType = "application/json")]
    [Description("One order as JSON.")]
    public static string Get(string orderId) => OrderJson.For(orderId);
}
```

- Register them with `WithPromptsFromAssembly()` and `WithResourcesFromAssembly()`.
- A URI template variable is input from the client. Validate it like a tool argument.
- For "not found", throw `McpException`. Spec 2026-07-28 uses the error code `-32602` for it, not `-32002`.

## Completions, filters, and extensions

- Completions: `WithCompleteHandler(...)` returns a `CompleteResult` with values for a prompt argument or a URI template variable.
- Filters: `WithRequestFilters(f => f.AddCallToolFilter(...))` runs code around each tool call: an audit log, a rate limit for each tool, a check for each tool.
- `Microsoft.Extensions.AI`: `McpServerTool.Create(AIFunction)` makes a tool from an existing `AIFunction`. On the client side, `McpClientTool` is an `AIFunction` for an `IChatClient`.
- Experimental: MCP Apps (`MCPEXP003`) and MCP Skills (`MCPEXP005`). Use them only when the human asks, and name the diagnostic in the report.
