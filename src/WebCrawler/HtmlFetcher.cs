namespace WebCrawler;

public sealed class FetchResult
{
    public required string Html { get; init; }
    public required Uri FinalUri { get; init; }
}

public sealed class HtmlFetcher
{
    private readonly HttpClient _http;
    private const int MaxAttempts = 2;

    public HtmlFetcher(HttpClient http)
    {
        _http = http;
    }

    public async Task<FetchResult?> FetchAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var response = await _http
                    .GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return null;

                var finalUri = response.RequestMessage?.RequestUri ?? requestUri;

                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType != null && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
                    return null;

                var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(html))
                    return null;

                return new FetchResult { Html = html, FinalUri = finalUri };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException) when (attempt < MaxAttempts)
            {
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }
        }

        return null;
    }
}
