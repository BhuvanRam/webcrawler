using Serilog;
using WebCrawler.Services;

namespace WebCrawler;
public sealed class Crawler
{
    private readonly CrawlState _state;
    private readonly HtmlFetcher _fetcher;
    private readonly HtmlLinkParser _parser;
    private readonly CrawlerOptions _options;

    public Crawler(CrawlState state, HtmlFetcher fetcher, HtmlLinkParser parser, CrawlerOptions options)
    {
        _state = state;
        _fetcher = fetcher;
        _parser = parser;
        _options = options;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var allowedHost = _options.StartUrl.Host;
        var normalizedStart = UrlNormalizer.Normalize(_options.StartUrl);
        Log.Information(
            "Crawl starting. AllowedHost={AllowedHost}, MaxConcurrency={MaxConcurrency}, Start={Start}",
            allowedHost,
            _options.MaxConcurrency,
            normalizedStart);
        _state.Seed(normalizedStart);

        while (true)
        {
            var batch = _state.TakeDiscoveredBatch(_options.MaxConcurrency);
            if (batch.Count == 0)
                break;

            Log.Information("Processing batch of {Count} URL(s)", batch.Count);
            var tasks = batch.Select(url => ProcessOneAsync(url, allowedHost, cancellationToken));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        Log.Information("Crawl finished.");
    }

    private async Task ProcessOneAsync(string normalizedUrl, string allowedHost, CancellationToken cancellationToken)
    {
        var requestUri = new Uri(normalizedUrl);
        Log.Information("GET {Url}", normalizedUrl);
        FetchResult? result;
        try
        {
            result = await _fetcher.FetchAsync(requestUri, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _state.MarkFailed(normalizedUrl);
            Log.Warning(ex, "FAILED: {Url}", normalizedUrl);
            return;
        }

        if (result == null)
        {
            _state.MarkFailed(normalizedUrl);
            Log.Warning("FAILED: {Url} (fetch failed or not HTML)", normalizedUrl);
            return;
        }

        if (!UrlNormalizer.IsSameHost(result.FinalUri, allowedHost))
        {
            _state.MarkFailed(normalizedUrl);
            Log.Warning("FAILED: {Url} (redirected outside allowed host)", normalizedUrl);
            return;
        }

        var links = _parser.ExtractLinks(result.Html, result.FinalUri);
        PrintPage(normalizedUrl, links);

        foreach (var link in links)
        {
            if (!Uri.TryCreate(link, UriKind.Absolute, out var absolute) ||
                !UrlNormalizer.IsSameHost(absolute, allowedHost))
                continue;

            _state.TryAddDiscovered(link);
        }

        _state.MarkCompleted(normalizedUrl);
    }

    private static void PrintPage(string visitedUrl, IReadOnlyList<string> links)
    {
        Log.Information("VISIT: {VisitedUrl}", visitedUrl);
        Log.Information("Links:");
        foreach (var link in links)
            Log.Information("  {Link}", link);
    }
}
