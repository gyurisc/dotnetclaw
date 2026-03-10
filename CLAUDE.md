# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What Is DotNetClaw

A .NET 10 rewrite of [OpenClaw](https://openclaw.ai) — a side project for learning AI agent concepts in C#. Prioritize simplicity and fun over production polish.

Currently at v0.0.1: raw HttpClient calls to the OpenAI Chat Completions API with tool/function-calling, a single tool (`get_current_time`), and in-memory conversation history.

## Build & Run

```bash
# Build
dotnet build src/DotNetClaw.csproj

# Run (requires OPENAI_API_KEY env var)
dotnet run --project src/DotNetClaw.csproj
```

No solution file — build the csproj directly.

## Architecture

- **File-based approach** — all source lives under `src/`. No class-based project structure.
- **Single-file agent loop** in `src/dotnetclaw.cs`
- Top-level statements, no classes — the entire app is a single procedural script
- Agent loop pattern: read user input → send to OpenAI with tool definitions → if model returns `tool_calls`, execute the tool locally and loop back to the model with the result → if model returns content, print it and wait for next input
- Uses `System.Text.Json` with `JsonDocument` for parsing API responses (no typed DTOs)
- Tool definitions are inline anonymous objects serialized directly
- Model: `gpt-4.1-mini`, temperature 0.2

## Key Conventions

- Suppresses `IL2026` and `IL3050` trimming warnings at the top of the file
- `Nullable` and `ImplicitUsings` enabled in the csproj
- API key read from `OPENAI_API_KEY` environment variable
