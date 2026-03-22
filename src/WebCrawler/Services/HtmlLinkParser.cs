using AngleSharp.Html.Parser;

namespace WebCrawler.Services;

public sealed class HtmlLinkParser
{
    private readonly HtmlParser _parser = new();

    public IReadOnlyList<string> ExtractLinks(string html, Uri pageUri)
    {
        var document = _parser.ParseDocument(html);
        var results = new List<string>();

        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href");
            if (!UrlNormalizer.TryCreateAbsolute(href, pageUri, out var absolute))
                continue;

            if (absolute.Scheme != Uri.UriSchemeHttp && absolute.Scheme != Uri.UriSchemeHttps)
                continue;

            results.Add(UrlNormalizer.Normalize(absolute));
        }

        return results;
    }
}
