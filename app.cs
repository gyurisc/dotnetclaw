#pragma warning disable IL2026
#pragma warning disable IL3050

using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
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
    new Dictionary<string, object?>
    {
        { "role", "system" },
        {  "content", """
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
    var input = Console.ReadLine();

    if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
        break;

    messages.Add(new Dictionary<string, object?>
    {
        { "role", "user" },
        { "content", input ?? ""}
    });

    bool awaitingToolResult = true;

    while(awaitingToolResult)
    {
        var request = new
        {
            model = "gpt-4.1-mini", 
            messages = messages,         
            tools = new []
            {
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
            }, 
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
            continue;
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        var message = doc
            .RootElement
            .GetProperty("choices")[0]
            .GetProperty("message");

        if (message.TryGetProperty("tool_calls", out var toolCalls))
        {
            messages.Add(new Dictionary<string, object?> 
            {
                { "role", "assistant" },
                { "content", null }, 
                { "tool_calls", JsonDocument.Parse(toolCalls.GetRawText()).RootElement.Clone() }
            });

            var toolName = toolCalls[0]
                .GetProperty("function")
                .GetProperty("name")
                .GetString();

            if(toolName == "get_current_time") 
            {
                var toolResult = DateTime.UtcNow.ToString("O");
                messages.Add(new Dictionary<string, object?>
                {
                    { "role", "tool" },
                    { "content", toolResult }, 
                    { "tool_call_id", toolCalls[0].GetProperty("id").GetString() }
                });

                continue; // call model again with tool result 
            }
        } 
        else
        {
            var content = message
                .GetProperty("content")
                .GetString();

            Console.WriteLine(content);
            Console.WriteLine();            

            messages.Add(
                new Dictionary<string, object?>
                {
                    { "role", "assistant" },
                    { "content", content ?? "" }
                }
            );   

            awaitingToolResult = false;     
        }        
    }
}
