# DotNetClaw

A .NET rewrite of [OpenClaw](https://openclaw.ai) for learning AI agent concepts in C#.

Following dabit3's ["Building a Persistent AI Assistant from First Principles"](https://gist.githubusercontent.com/dabit3/bc60d3bea0b02927995cd9bf53c3db32/raw/be311031f1a4b8686cce8be3d2251703ff037a67/you_couldve_invented_openclaw.md) ([tweet](https://x.com/dabit3/status/2021387483364151451)).

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
