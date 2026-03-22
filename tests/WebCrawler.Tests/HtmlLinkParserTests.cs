using WebCrawler.Services;
using Xunit;

namespace WebCrawler.Tests;

public class HtmlLinkParserTests
{
    private readonly HtmlLinkParser _parser = new();

    [Fact]
    public void ExtractLinks_Resolves_Relative_Against_Page()
    {
        var page = new Uri("https://example.com/a/b");
        const string html = """<html><body><a href="../c">x</a><a href="https://other.test/z">o</a></body></html>""";

        var links = _parser.ExtractLinks(html, page);

        Assert.Contains("https://example.com/c", links);
        Assert.Contains("https://other.test/z", links);
    }

    [Fact]
    public void ExtractLinks_Skips_Non_Http_Schemes()
    {
        var page = new Uri("https://example.com/");
        const string html = """<a href="mailto:a@b.com">m</a><a href="javascript:void(0)">j</a><a href="/ok">ok</a>""";

        var links = _parser.ExtractLinks(html, page);

        Assert.Single(links);
        Assert.Equal("https://example.com/ok", links[0]);
    }
}
