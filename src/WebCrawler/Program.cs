using System.Net.Http;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Debugging;
using WebCrawler;
using WebCrawler.Services;

const string CrawlerClientName = "crawler";

static string ConfigureLogging()
{
    // If the file sink fails (permissions, path, locks), Serilog reports here — check for [Serilog] lines.
    SelfLog.Enable(msg => Console.Error.WriteLine("[Serilog] " + msg));

    Log.CloseAndFlush();

    var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
    Directory.CreateDirectory(logDir);

    // Single predictable path so you always open the same file as the startup line prints.
    var logFile = Path.GetFullPath(Path.Combine(logDir, "crawl.log"));
    File.WriteAllText(logFile, string.Empty, Encoding.UTF8);

    const string fileTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console(
            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: logFile,
            encoding: Encoding.UTF8,
            rollingInterval: RollingInterval.Infinite,
            shared: true,
            flushToDiskInterval: TimeSpan.FromSeconds(1),
            outputTemplate: fileTemplate)
        .CreateLogger();

    return logFile;
}

static int Usage()
{
    Console.Error.WriteLine("Usage: webcrawler <start-url> [--parallel|-p <n>]");
    Console.Error.WriteLine("  start-url   Absolute http(s) URL to begin crawling (same host only).");
    Console.Error.WriteLine("  --parallel  Max concurrent fetches per batch (default: 8). Use e.g. -p 32 for heavy sites.");
    return 1;
}

var logPath = ConfigureLogging();
try
{
    Log.Information("Text log file: {LogPath}", logPath);

    if (!TryParseArgs(args, out var startUrl, out var parallel))
    {
        Log.Warning("Missing or invalid arguments (need an absolute http(s) start URL).");
        return Usage();
    }

    var services = new ServiceCollection();
    // HTTP/2 can multiplex one connection; enabling extra connections helps when the server caps streams.
    services.AddHttpClient(CrawlerClientName)
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true
        })
        .ConfigureHttpClient(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

    using var provider = services.BuildServiceProvider();
    var factory = provider.GetRequiredService<IHttpClientFactory>();
    using var http = factory.CreateClient(CrawlerClientName);

    var state = new CrawlState();
    var fetcher = new HtmlFetcher(http);
    var parser = new HtmlLinkParser();
    var options = new CrawlerOptions
    {
        StartUrl = startUrl,
        MaxConcurrency = parallel
    };

    var crawler = new Crawler(state, fetcher, parser, options);
    await crawler.RunAsync(CancellationToken.None).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    return 130;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unexpected error");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;

static bool TryParseArgs(string[] args, out Uri startUrl, out int parallel)
{
    startUrl = default!;
    parallel = 8;

    if (args.Length == 0)
        return false;

    Uri? parsed = null;
    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (arg is "--parallel" or "-p")
        {
            if (i + 1 >= args.Length)
                return false;
            if (!int.TryParse(args[++i], out parallel) || parallel < 1)
                return false;
            continue;
        }

        if (arg.StartsWith('-'))
            return false;

        if (parsed != null)
            return false;

        if (!Uri.TryCreate(arg, UriKind.Absolute, out parsed))
            return false;

        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
            return false;
    }

    if (parsed == null)
        return false;

    startUrl = parsed;
    return true;
}
