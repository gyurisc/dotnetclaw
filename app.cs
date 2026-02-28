using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var apikey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(apikey))
{
    Console.WriteLine("OPENAI_API_KEY not set.");
    return;
}

var http = new HttpClient();
http.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", apikey);

var messages = new List<Dictionary<string, object>>
{
    new Dictionary<string, object>
    {
        { "role", "system" },
        { "content", "You are DotNetClaw, a pragmatic AI agent." }
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

    messages.Add(new Dictionary<string, object>
    {
        { "role", "user" },
        { "content", input ?? ""}
    });

    var request = new
    {
        model = "gpt-4.1-mini", 
        messages = messages, 
        temperature = 0.2
    }; 

    var json = JsonSerializer.Serialize(request);

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

    var content = doc.RootElement
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString();

    Console.WriteLine(content);
    Console.WriteLine();

    messages.Add(
        new Dictionary<string, object>
        {
            { "role", "assistant" },
            { "content", content ?? "" }
        }
    );
}
