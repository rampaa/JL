namespace JL.Windows.Backlog;

internal readonly record struct BacklogItem(string Text, DateTime Timestamp, BacklogStats SessionStats, BacklogStats ProfileStats, BacklogStats LifetimeStats);
