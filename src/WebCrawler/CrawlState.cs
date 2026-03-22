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
            var batch = new List<string>();
            foreach (var pair in _states)
            {
                if (pair.Value != UrlState.Discovered)
                    continue;

                _states[pair.Key] = UrlState.InProgress;
                batch.Add(pair.Key);
                if (batch.Count >= maxCount)
                    break;
            }

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
