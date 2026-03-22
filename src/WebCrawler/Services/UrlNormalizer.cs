namespace WebCrawler.Services;

public static class UrlNormalizer
{
    public static string Normalize(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Fragment = null
        };
        return builder.Uri.AbsoluteUri;
    }

    public static bool TryCreateAbsolute(string? value, Uri baseUri, out Uri absolute)
    {
        absolute = default!;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!Uri.TryCreate(value, UriKind.Absolute, out absolute))
        {
            if (!Uri.TryCreate(baseUri, value, out absolute))
                return false;
        }

        return true;
    }

    public static bool IsSameHost(Uri uri, string allowedHost) =>
        string.Equals(uri.Host, allowedHost, StringComparison.OrdinalIgnoreCase);
}
