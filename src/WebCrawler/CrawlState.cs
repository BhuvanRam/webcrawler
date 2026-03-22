using WebCrawler.Enums;

namespace WebCrawler;

public sealed class CrawlState
{
    private readonly object _gate = new();
    private readonly Dictionary<string, UrlState> _states = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, UrlState> Snapshot()
    {
        lock (_gate)
        {
            return new Dictionary<string, UrlState>(_states, StringComparer.Ordinal);
        }
    }

    public void Seed(string normalizedUrl)
    {
        lock (_gate)
        {
            _states.TryAdd(normalizedUrl, UrlState.Discovered);
        }
    }

    public bool TryAddDiscovered(string normalizedUrl)
    {
        lock (_gate)
        {
            return _states.TryAdd(normalizedUrl, UrlState.Discovered);
        }
    }

    public List<string> TakeDiscoveredBatch(int maxCount)
    {
        lock (_gate)
        {
            var batch = _states
                .Where(p => p.Value == UrlState.Discovered)
                .Take(maxCount)
                .Select(p => p.Key)
                .ToList();

            foreach (var key in batch)
                _states[key] = UrlState.InProgress;

            return batch;
        }
    }

    public void MarkCompleted(string normalizedUrl)
    {
        lock (_gate)
        {
            _states[normalizedUrl] = UrlState.Completed;
        }
    }

    public void MarkFailed(string normalizedUrl)
    {
        lock (_gate)
        {
            _states[normalizedUrl] = UrlState.Failed;
        }
    }
}
