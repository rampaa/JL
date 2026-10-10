namespace JL.Windows.Backlog;

internal readonly record struct BacklogItem(string Text, DateTime Timestamp, BacklogStats SessionStats, BacklogStats ProfileStats, BacklogStats LifetimeStats)
{
    public BacklogStats SessionStats { get; } = SessionStats;
    public BacklogStats ProfileStats { get; } = ProfileStats;
    public BacklogStats LifetimeStats { get; } = LifetimeStats;
}
