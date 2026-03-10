# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What Is DotNetClaw

A .NET 10 rewrite of [OpenClaw](https://openclaw.ai) — a learning project for understanding AI agent concepts in C#. Follows dabit3's [guide](https://gist.githubusercontent.com/dabit3/bc60d3bea0b02927995cd9bf53c3db32/raw/be311031f1a4b8686cce8be3d2251703ff037a67/you_couldve_invented_openclaw.md) on building a persistent AI assistant from first principles.

**This is not a product or a library.** Everything here serves learning and discoverability — code should be easy to read, data should be easy to inspect, and concepts should be easy to follow. Favor clarity over abstraction. Keep things simple and fun.

## Build & Run

```bash
# Build
dotnet build src/DotNetClaw.csproj

# Run from repo root (requires OPENAI_API_KEY env var)
dotnet run --project src/DotNetClaw.csproj

# Run with a named session
dotnet run --project src/DotNetClaw.csproj -- mysession
```

No solution file — build the csproj directly. Always run from the repo root so `sessions/` is created there.

## Architecture

- **File-based approach** — all source lives under `src/`. No classes, no project structure, no abstractions.
- **Single-file agent loop** in `src/dotnetclaw.cs` using top-level statements
- Agent loop: read input → call OpenAI with tools → execute tool calls and loop back → print final response
- Raw `HttpClient` + `System.Text.Json` — no SDKs, no typed DTOs. This is intentional for learning.
- Tool definitions in `GetTools()`, dispatch in `ExecuteTool()` — adding a tool is a two-liner
- Sessions persisted as JSONL in `sessions/` at repo root (gitignored)
- Model: `gpt-4.1-mini`, temperature 0.2

## Key Conventions

- Suppresses `IL2026` and `IL3050` trimming warnings at the top of the file
- `Nullable` and `ImplicitUsings` enabled in the csproj
- API key read from `OPENAI_API_KEY` environment variable
- `args` is reserved by top-level statements — use `toolArgs` or similar for local variables

## Design Decisions

- **No SDK** — raw HTTP calls so the API mechanics are visible and learnable
- **No classes** — procedural style, everything in one file until it gets painful (~300+ lines)
- **No solution file** — unnecessary overhead for a single project
- **JSONL for sessions** — human-readable, appendable, easy to inspect with any text editor
- **Local functions over methods** — `GetTools()`, `ExecuteTool()`, `LoadSession()`, `AppendToSession()` live at the bottom of the file
