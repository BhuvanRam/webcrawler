using WebCrawler;

static int Usage()
{
    Console.Error.WriteLine("Usage: webcrawler <start-url> [--parallel|-p <n>]");
    Console.Error.WriteLine("  start-url   Absolute http(s) URL to begin crawling (same host only).");
    Console.Error.WriteLine("  --parallel  Maximum concurrent page fetches (default: 4).");
    return 1;
}

if (!TryParseArgs(args, out var startUrl, out var parallel))
    return Usage();

var http = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(30)
};

var state = new CrawlState();
var fetcher = new HtmlFetcher(http);
var parser = new HtmlLinkParser();
var options = new CrawlerOptions
{
    StartUrl = startUrl,
    MaxConcurrency = parallel
};

var crawler = new Crawler(state, fetcher, parser, options);

try
{
    await crawler.RunAsync(CancellationToken.None).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    return 130;
}

return 0;

static bool TryParseArgs(string[] args, out Uri startUrl, out int parallel)
{
    startUrl = default!;
    parallel = 4;

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
