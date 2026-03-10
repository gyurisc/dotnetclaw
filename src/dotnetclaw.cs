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

var debug = args.Contains("--debug");
var sessionName = args.Where(a => !a.StartsWith("--")).FirstOrDefault() ?? "default";
var sessionsDir = Path.Combine(Directory.GetCurrentDirectory(), "sessions");
Directory.CreateDirectory(sessionsDir);
var sessionPath = Path.Combine(sessionsDir, $"{sessionName}.jsonl");

var http = new HttpClient();
http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", apikey);

var jsonOptions = new JsonSerializerOptions
{
    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
};

var soulPath = Path.Combine(Directory.GetCurrentDirectory(), "workspace", "SOUL.md");
var soul = File.Exists(soulPath)
    ? File.ReadAllText(soulPath)
    : "You are DotNetClaw, a pragmatic AI agent.";

var systemMessage = new Dictionary<string, object?>
{
    { "role", "system" },
    { "content", soul }
};

var messages = new List<Dictionary<string, object?>> { systemMessage };
var restored = LoadSession(sessionPath);
messages.AddRange(restored);

Console.WriteLine("DotNetClaw v0.0.1");
Console.WriteLine($"Session: {sessionName} ({restored.Count} messages loaded)");
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

    var userMsg = new Dictionary<string, object?> { { "role", "user" }, { "content", input } };
    messages.Add(userMsg);
    AppendToSession(sessionPath, userMsg);

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

        if (debug)
        {
            Console.WriteLine("--- REQUEST ---");
            Console.WriteLine(JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                WriteIndented = true
            }));
            Console.WriteLine("--- END ---");
            Console.WriteLine();
        }

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
            var assistantMsg = new Dictionary<string, object?>
            {
                { "role", "assistant" },
                { "content", null },
                { "tool_calls", JsonDocument.Parse(toolCalls.GetRawText()).RootElement.Clone() }
            };
            messages.Add(assistantMsg);
            AppendToSession(sessionPath, assistantMsg);

            foreach (var call in toolCalls.EnumerateArray())
            {
                var name = call.GetProperty("function").GetProperty("name").GetString()!;
                var toolArgs = call.GetProperty("function").GetProperty("arguments").GetString()!;
                var result = ExecuteTool(name, toolArgs);

                var toolMsg = new Dictionary<string, object?>
                {
                    { "role", "tool" },
                    { "content", result },
                    { "tool_call_id", call.GetProperty("id").GetString() }
                };
                messages.Add(toolMsg);
                AppendToSession(sessionPath, toolMsg);
            }

            continue;
        }

        var content = message.GetProperty("content").GetString() ?? "";
        Console.WriteLine(content);
        Console.WriteLine();

        var replyMsg = new Dictionary<string, object?> { { "role", "assistant" }, { "content", content } };
        messages.Add(replyMsg);
        AppendToSession(sessionPath, replyMsg);
        awaitingToolResult = false;
    }
}

// --- Session persistence ---

List<Dictionary<string, object?>> LoadSession(string path)
{
    var loaded = new List<Dictionary<string, object?>>();
    if (!File.Exists(path)) return loaded;

    foreach (var line in File.ReadLines(path))
    {
        if (string.IsNullOrWhiteSpace(line)) continue;
        var msg = JsonSerializer.Deserialize<Dictionary<string, object?>>(line, jsonOptions);
        if (msg != null) loaded.Add(msg);
    }
    return loaded;
}

void AppendToSession(string path, Dictionary<string, object?> message)
{
    File.AppendAllText(path, JsonSerializer.Serialize(message, jsonOptions) + "\n");
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
