# DotNetClaw

A .NET rewrite of [OpenClaw](https://openclaw.ai) for learning AI agent concepts in C#.

## Setup

Requires .NET 10 and an OpenAI API key:

```bash
export OPENAI_API_KEY=sk-...
dotnet run --project src/DotNetClaw.csproj
```

Type `exit` to quit.

## v0.0.1 - Raw Agent Loop

- File-based .NET 10 app, single-file top-level statements
- Direct HttpClient calls to OpenAI Chat Completions API
- Tool calling with agent loop (calls model again after tool execution)
- One tool: `get_current_time`
- In-memory conversation history
