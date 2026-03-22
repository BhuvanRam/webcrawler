using System.Net;
using System.Text;
using WebCrawler.Enums;
using WebCrawler.Services;
using Xunit;

namespace WebCrawler.Tests;

public class CrawlerTests
{
    private sealed class MapHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _map;

        public MapHandler(Dictionary<string, string> map)
        {
            _map = map;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = request.RequestUri!.AbsoluteUri;
            if (!_map.TryGetValue(key, out var html))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    RequestMessage = request
                });
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html"),
                RequestMessage = request
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task Crawler_Follows_Same_Host_Links_Only()
    {
        var root = new Uri("https://example.com/");
        var rootKey = UrlNormalizer.Normalize(root);
        var subKey = UrlNormalizer.Normalize(new Uri("https://example.com/sub"));

        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [rootKey] = """<html><body><a href="/sub">go</a><a href="https://evil.test/x">bad</a></body></html>""",
            [subKey] = "<html></html>"
        };

        using var http = new HttpClient(new MapHandler(map));
        var state = new CrawlState();
        var fetcher = new HtmlFetcher(http);
        var parser = new HtmlLinkParser();
        var options = new CrawlerOptions { StartUrl = root, MaxConcurrency = 2 };

        var crawler = new Crawler(state, fetcher, parser, options);
        await crawler.RunAsync(CancellationToken.None).ConfigureAwait(false);

        var snap = state.Snapshot();
        Assert.Equal(UrlState.Completed, snap[rootKey]);
        Assert.Equal(UrlState.Completed, snap[subKey]);
        Assert.False(snap.ContainsKey("https://evil.test/x"));
    }
}
