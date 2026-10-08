using Microsoft.Extensions.Time.Testing;

namespace FunBooksAndVideos.TestKit;

/// <summary>Deterministic time for tests.</summary>
public static class TestClock
{
    public static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    public static FakeTimeProvider Create() => new(Now);
}
