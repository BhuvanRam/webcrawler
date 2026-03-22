# WebCrawler

A small **.NET 8** CLI tool that crawls **same-host** pages starting from one URL, prints discovered links, and uses **bounded concurrency** (batching with `Task.WhenAll`). State stays **in memory** only.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build and run

From the repository root:

```bash
dotnet restore WebCrawler.sln
dotnet build WebCrawler.sln -c Release
dotnet run --project src/WebCrawler -- https://example.com
```

Optional parallelism (max concurrent fetches per batch; default **8**):

```bash
dotnet run --project src/WebCrawler -- https://example.com --parallel 32
```

Short flag:

```bash
dotnet run --project src/WebCrawler -- https://example.com -p 16
```

The published executable name is **`webcrawler`** (`webcrawler.dll` when run with `dotnet`).

## Behavior

- **Scope:** Only URLs whose **host** matches the start URL are enqueued (same host only).
- **Concurrency:** Up to `N` parallel HTTP fetches per batch (`--parallel` / `-p`). The first batch is often a single URL until more links are discovered.
- **HTTP:** Uses `IHttpClientFactory` with a named client and a `SocketsHttpHandler` with `EnableMultipleHttp2Connections` enabled. Request timeout is **30 seconds**.

## Logging

Logging uses **Serilog** to the **console** and to a **text file** under the app output directory:

`{AppContext.BaseDirectory}/logs/crawl.log`

For a typical Debug build that is:

`src/WebCrawler/bin/Debug/net8.0/logs/crawl.log`

Each run **truncates** `crawl.log` at startup, then appends lines for that run. The first log line records the **full path** to the file. If the file sink fails, messages may appear on **stderr** with a `[Serilog]` prefix.

Invalid or missing CLI arguments print usage to **stderr**; structured logs still go through Serilog when arguments are valid.

## Tests

```bash
dotnet test WebCrawler.sln -c Release
```

## Docker

Build and run the published CLI (pass your URL and flags after the image name):

```bash
docker build -t webcrawler .
docker run --rm webcrawler https://example.com --parallel 8
```

## Solution layout

| Path | Description |
|------|-------------|
| `src/WebCrawler/` | Console application |
| `tests/WebCrawler.Tests/` | Unit tests (parser, state, crawler with mock HTTP) |
| `Dockerfile` | Multi-stage image: SDK build, runtime entrypoint `dotnet webcrawler.dll` |

## Debugging in Visual Studio

`Properties/launchSettings.json` can set `commandLineArgs` (for example a start URL and `--parallel`) so **F5** runs with the same arguments every time.
