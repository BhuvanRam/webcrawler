namespace WebCrawler;

public sealed class CrawlerOptions
{
    public required Uri StartUrl { get; init; }
    public int MaxConcurrency { get; init; } = 8;
}
