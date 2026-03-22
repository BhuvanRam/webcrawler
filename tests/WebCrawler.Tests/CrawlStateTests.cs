namespace WebCrawler.Tests;

public class CrawlStateTests
{
    [Fact]
    public void TakeDiscoveredBatch_Moves_State_To_InProgress()
    {
        var state = new CrawlState();
        state.Seed("https://example.com/a");
        state.TryAddDiscovered("https://example.com/b");

        var batch = state.TakeDiscoveredBatch(10);

        Assert.Equal(2, batch.Count);
        Assert.Contains("https://example.com/a", batch);
        Assert.Contains("https://example.com/b", batch);

        var snap = state.Snapshot();
        Assert.Equal(UrlState.InProgress, snap["https://example.com/a"]);
        Assert.Equal(UrlState.InProgress, snap["https://example.com/b"]);
    }

    [Fact]
    public void TryAddDiscovered_Is_Idempotent()
    {
        var state = new CrawlState();
        Assert.True(state.TryAddDiscovered("https://example.com/x"));
        Assert.False(state.TryAddDiscovered("https://example.com/x"));
    }
}
