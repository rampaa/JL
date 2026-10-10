namespace JL.Windows.Backlog;

internal readonly record struct BacklogStats(int CharacterCount, int CharacterCountWithoutPunctuation, int ResetCount, bool CountLine, bool LineCounted);
