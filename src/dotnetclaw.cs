#pragma warning disable IL2026
#pragma warning disable IL3050

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

var apikey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(apikey))
{
    Console.WriteLine("OPENAI_API_KEY not set.");
    return;
}

var http = new HttpClient();
http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", apikey);

var jsonOptions = new JsonSerializerOptions
{
    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
};

var messages = new List<Dictionary<string, object?>>
{
    new()
    {
        { "role", "system" },
        { "content", """
            You are DotNetClaw, a pragmatic AI agent.
            When a user asks for the current time, you MUST call the function `get_current_time`.
            Do not answer directly if the time is requested. Always use the tool.
            """ }
    }
};

Console.WriteLine("DotNetClaw v0.0.1");
Console.WriteLine("Type 'exit' to quit.");
Console.WriteLine();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(input))
        continue;

    if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
        break;

    messages.Add(new() { { "role", "user" }, { "content", input } });

    var awaitingToolResult = true;

    while (awaitingToolResult)
    {
        var request = new
        {
            model = "gpt-4.1-mini",
            messages,
            tools = GetTools(),
            tool_choice = "auto",
            temperature = 0.2,
        };

        var json = JsonSerializer.Serialize(request, jsonOptions);
        var response = await http.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error: {response.StatusCode}");
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            break;
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        var message = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message");

        if (message.TryGetProperty("tool_calls", out var toolCalls))
        {
            messages.Add(new()
            {
                { "role", "assistant" },
                { "content", null },
                { "tool_calls", JsonDocument.Parse(toolCalls.GetRawText()).RootElement.Clone() }
            });

            foreach (var call in toolCalls.EnumerateArray())
            {
                var name = call.GetProperty("function").GetProperty("name").GetString()!;
                var toolArgs = call.GetProperty("function").GetProperty("arguments").GetString()!;
                var result = ExecuteTool(name, toolArgs);

                messages.Add(new()
                {
                    { "role", "tool" },
                    { "content", result },
                    { "tool_call_id", call.GetProperty("id").GetString() }
                });
            }

            continue;
        }

        var content = message.GetProperty("content").GetString() ?? "";
        Console.WriteLine(content);
        Console.WriteLine();

        messages.Add(new() { { "role", "assistant" }, { "content", content } });
        awaitingToolResult = false;
    }
}

// --- Tool definitions ---

object[] GetTools() =>
[
    new
    {
        type = "function",
        function = new
        {
            name = "get_current_time",
            description = "Returns the current UTC time",
            parameters = new
            {
                type = "object",
                properties = new { },
                required = Array.Empty<string>()
            }
        }
    }
];

// --- Tool dispatch ---

string ExecuteTool(string name, string toolArgs) => name switch
{
    "get_current_time" => DateTime.UtcNow.ToString("O"),
    _ => $"Unknown tool: {name}"
};
