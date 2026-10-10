using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Controls;
using JL.Core;
using JL.Core.Config;
using JL.Core.Frontend;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Core.Utilities.ObjectPool;
using JL.Windows.Config;
using JL.Windows.GUI;
using JL.Windows.GUI.Notification;
using Timer = System.Timers.Timer;

namespace JL.Windows.Backlog;

internal static class BacklogUtils
{
    private static readonly string s_backlogDirectory = Path.Join(AppInfo.ApplicationPath, "Backlogs");

    private static readonly LinkedList<BacklogItem> s_backlog = [];
    private static LinkedListNode<BacklogItem>? s_currentNode;

    private static readonly HashSet<string> s_uniqueBacklogItems = new(StringComparer.Ordinal);

    private static readonly LinkedList<LinkedListNode<BacklogItem>> s_pendingBacklogItemsForBacklogFile = new();

    private static readonly SemaphoreSlim s_semaphoreSlimForBacklogFile = new(1, 1);
    private static string? s_backlogFilePathToRestore;
    private static long s_backlogFileLengthToRestore; // = 0
    private const string RecordSeparator = "\u001E\n";

    public static string? LastItem => s_backlog.Last?.Value.Text;
    public static string AllBacklogText
    {
        get
        {
            if (s_backlog.Count is 0)
            {
                return "";
            }

            StringBuilder stringBuilder = ObjectPoolManager.StringBuilderPool.Get();
            try
            {
                LinkedListNode<BacklogItem>? node = s_backlog.First;
                while (node is not null)
                {
                    _ = stringBuilder.Append(node.Value.Text);
                    if (node.Next is not null)
                    {
                        _ = stringBuilder.Append(RecordSeparator);
                    }

                    node = node.Next;
                }

                return stringBuilder.ToString();
            }
            finally
            {
                ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
            }
        }
    }

    private static readonly Timer s_writePendingItemsToBacklogFileTimer = new()
    {
        Interval = TimeSpan.FromMinutes(5).TotalMilliseconds,
        AutoReset = true,
        Enabled = false
    };

    private static readonly Lock s_pendingItemsLock = new();

    public static void AddToBacklog(string text, int characterCount, int characterCountWithoutPunctuation, bool countStats)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.MaxBacklogCapacity > 0 && s_backlog.Count > configManager.MaxBacklogCapacity)
        {
            s_backlog.RemoveFirst();
        }

        s_currentNode = s_backlog.AddLast(CreateBacklogItem(text, characterCount, characterCountWithoutPunctuation, countStats));
        if (configManager is { AutoSaveBacklogBeforeClosing: true, MaxBacklogCapacity: not 0 })
        {
            lock (s_pendingItemsLock)
            {
                _ = s_pendingBacklogItemsForBacklogFile.AddLast(s_currentNode);
            }
        }
    }

    public static void AddToUniqueBacklogItems(string text)
    {
        _ = s_uniqueBacklogItems.Add(text);
    }

    public static void AddToBacklogShowAllBacklog(string text, int characterCount, int characterCountWithoutPunctuation, bool countStats)
    {
        ConfigManager configManager = ConfigManager.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        TextBox mainTextBox = mainWindow.MainTextBox;

        bool removeOldestItem = configManager.MaxBacklogCapacity > 0 && s_backlog.Count > configManager.MaxBacklogCapacity;

        BacklogItem item = CreateBacklogItem(text, characterCount, characterCountWithoutPunctuation, countStats);
        s_currentNode = s_backlog.AddLast(item);
        if (configManager is { AutoSaveBacklogBeforeClosing: true, MaxBacklogCapacity: not 0 })
        {
            lock (s_pendingItemsLock)
            {
                _ = s_pendingBacklogItemsForBacklogFile.AddLast(s_currentNode);
            }
        }

        if (removeOldestItem)
        {
            s_backlog.RemoveFirst();
            mainTextBox.Text = AllBacklogText;
        }
        else
        {
            if (mainTextBox.Text.Length > 0)
            {
                mainTextBox.AppendText($"{RecordSeparator}{item.Text}");
            }
            else
            {
                mainTextBox.Text = item.Text;
            }
        }

        mainTextBox.CaretIndex = mainTextBox.Text.Length;
        mainTextBox.ScrollToEnd();
    }

    public static bool BacklogContains(string text)
    {
        return s_uniqueBacklogItems.Contains(text);
    }

    public static void UpdateUniqueBacklogItem(string oldText, string newText)
    {
        _ = s_uniqueBacklogItems.Remove(oldText);
        _ = s_uniqueBacklogItems.Add(newText);
    }

    public static void UpdateUniqueBacklogItems()
    {
        foreach (BacklogItem backlogItem in s_backlog)
        {
            _ = s_uniqueBacklogItems.Add(backlogItem.Text);
        }
    }

    private static BacklogItem CreateBacklogItem(string text, int characterCount, int characterCountWithoutPunctuation, bool countLine)
    {
        bool lineCounted = countLine && (ConfigManager.Instance.StripPunctuationBeforeCalculatingCharacterCount
            ? characterCountWithoutPunctuation
            : characterCount) > 0;

        BacklogStats stats = new(characterCount, characterCountWithoutPunctuation, 0, countLine, lineCounted);
        return new BacklogItem(text, DateTime.Now,
            stats with { ResetCount = StatsUtils.SessionStats.ResetCount },
            stats with { ResetCount = StatsUtils.ProfileLifetimeStats.ResetCount },
            stats with { ResetCount = StatsUtils.LifetimeStats.ResetCount });
    }

    public static void ReplaceLastBacklogText(string text, int characterCount, int characterCountWithoutPunctuation)
    {
        ConfigManager configManager = ConfigManager.Instance;
        LinkedListNode<BacklogItem>? lastNode = s_backlog.Last;
        if (lastNode is not null)
        {
            lock (s_pendingItemsLock)
            {
                BacklogItem lastItem = lastNode.Value;
                lastNode.Value = new BacklogItem(text, lastItem.Timestamp,
                    AddBacklogStats(lastItem.SessionStats, StatsUtils.SessionStats.ResetCount, characterCount, characterCountWithoutPunctuation),
                    AddBacklogStats(lastItem.ProfileStats, StatsUtils.ProfileLifetimeStats.ResetCount, characterCount, characterCountWithoutPunctuation),
                    AddBacklogStats(lastItem.LifetimeStats, StatsUtils.LifetimeStats.ResetCount, characterCount, characterCountWithoutPunctuation));
                if (configManager is { AutoSaveBacklogBeforeClosing: true, MaxBacklogCapacity: not 0 }
                    && s_pendingBacklogItemsForBacklogFile.Last?.Value != lastNode)
                {
                    // Autosave may already have written this entry before it was merged.
                    _ = s_pendingBacklogItemsForBacklogFile.AddLast(lastNode);
                }
            }
            s_currentNode = lastNode;
        }
        else
        {
            s_currentNode = s_backlog.AddLast(CreateBacklogItem(text, characterCount, characterCountWithoutPunctuation, false));
            if (configManager is { AutoSaveBacklogBeforeClosing: true, MaxBacklogCapacity: not 0 })
            {
                lock (s_pendingItemsLock)
                {
                    _ = s_pendingBacklogItemsForBacklogFile.AddLast(s_currentNode);
                }
            }
        }
    }

    public static void ShowLastBacklogItem()
    {
        s_currentNode = s_backlog.Last;
        MainWindow mainWindow = MainWindow.Instance;
        TextBox mainTextBox = mainWindow.MainTextBox;
        mainTextBox.Foreground = ConfigManager.Instance.MainWindowTextColor;
        string lastText = s_currentNode?.Value.Text ?? "";
        if (mainTextBox.Text != lastText)
        {
            mainTextBox.Text = lastText;
            mainTextBox.CaretIndex = mainTextBox.Text.Length;
            mainTextBox.ScrollToEnd();
            mainWindow.UpdatePosition();
        }
    }

    public static void ShowPreviousBacklogItem()
    {
        if (s_currentNode is null)
        {
            return;
        }

        MainWindow mainWindow = MainWindow.Instance;
        if (mainWindow.FirstPopupWindow.MiningMode)
        {
            return;
        }

        if (ConfigManager.Instance.AlwaysShowBacklog)
        {
            return;
        }

        if (s_currentNode.Previous is not null)
        {
            TextBox mainTextBox = mainWindow.MainTextBox;
            mainTextBox.Foreground = ConfigManager.Instance.MainWindowBacklogTextColor;
            s_currentNode = s_currentNode.Previous;
            mainTextBox.Text = s_currentNode.Value.Text;
            mainWindow.UpdatePosition();
        }
    }

    public static void ShowNextBacklogItem()
    {
        if (s_currentNode is null)
        {
            return;
        }

        MainWindow mainWindow = MainWindow.Instance;
        if (mainWindow.FirstPopupWindow.MiningMode)
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.AlwaysShowBacklog)
        {
            return;
        }

        if (s_currentNode.Next is not null)
        {
            TextBox mainTextBox = mainWindow.MainTextBox;
            mainTextBox.Foreground = s_currentNode.Next != s_backlog.Last
                ? configManager.MainWindowBacklogTextColor
                : configManager.MainWindowTextColor;

            s_currentNode = s_currentNode.Next;
            mainTextBox.Text = s_currentNode.Value.Text;
            mainWindow.UpdatePosition();
        }
    }

    public static void DeleteCurrentLine()
    {
        if (s_currentNode is null)
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.AlwaysShowBacklog)
        {
            return;
        }

        MainWindow mainWindow = MainWindow.Instance;
        TextBox mainTextBox = mainWindow.MainTextBox;

        BacklogItem backlogItem = s_currentNode.Value;
        if (backlogItem.Text != mainTextBox.Text)
        {
            return;
        }

        bool stripPunctuation = configManager.StripPunctuationBeforeCalculatingCharacterCount;
        SubtractBacklogStats(StatsUtils.SessionStats, backlogItem.SessionStats, stripPunctuation);
        SubtractBacklogStats(StatsUtils.ProfileLifetimeStats, backlogItem.ProfileStats, stripPunctuation);
        SubtractBacklogStats(StatsUtils.LifetimeStats, backlogItem.LifetimeStats, stripPunctuation);

        LinkedListNode<BacklogItem>? newCurrentNode = s_currentNode.Previous ?? s_currentNode.Next;
        _ = s_uniqueBacklogItems.Remove(backlogItem.Text);
        s_backlog.Remove(s_currentNode);

        lock (s_pendingItemsLock)
        {
            if (s_pendingBacklogItemsForBacklogFile.Last?.Value == s_currentNode)
            {
                s_pendingBacklogItemsForBacklogFile.RemoveLast();
            }
            else
            {
                _ = s_pendingBacklogItemsForBacklogFile.Remove(s_currentNode);
            }
        }

        s_currentNode = newCurrentNode;

        mainTextBox.Foreground = newCurrentNode != s_backlog.Last
            ? configManager.MainWindowBacklogTextColor
            : configManager.MainWindowTextColor;

        mainTextBox.Text = newCurrentNode is not null
            ? newCurrentNode.Value.Text
            : "";

        mainWindow.UpdatePosition();
    }

    public static void ShowAllBacklog()
    {
        if (s_backlog.Count is 0)
        {
            return;
        }

        MainWindow mainWindow = MainWindow.Instance;
        if (mainWindow.FirstPopupWindow.MiningMode)
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.AlwaysShowBacklog)
        {
            return;
        }

        string allBacklogText = AllBacklogText;
        TextBox mainTextBox = mainWindow.MainTextBox;
        if (mainTextBox.Text != allBacklogText
            && mainTextBox.GetFirstVisibleLineIndex() is 0)
        {
            int caretIndex = allBacklogText.Length - mainTextBox.Text.Length;

            mainTextBox.Text = allBacklogText;

            mainTextBox.Foreground = configManager.MainWindowBacklogTextColor;

            if (caretIndex >= 0)
            {
                mainTextBox.CaretIndex = caretIndex;
            }

            mainTextBox.ScrollToEnd();
            mainWindow.UpdatePosition();
        }
    }

    public static void InitializeOrRestartBacklogTimer()
    {
        if (!s_writePendingItemsToBacklogFileTimer.Enabled)
        {
            s_writePendingItemsToBacklogFileTimer.Elapsed += WritePendingItemsToBacklogFileTimerElapsed;
        }

        // Restarts the timer
        // This is faster than setting the Enabled property to false and then true
        s_writePendingItemsToBacklogFileTimer.Interval = s_writePendingItemsToBacklogFileTimer.Interval;
        s_writePendingItemsToBacklogFileTimer.Enabled = true;
    }

    public static void StopBacklogTimer()
    {
        s_writePendingItemsToBacklogFileTimer.Elapsed -= WritePendingItemsToBacklogFileTimerElapsed;
        s_writePendingItemsToBacklogFileTimer.Enabled = false;
    }

    private static async void WritePendingItemsToBacklogFileTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        await s_semaphoreSlimForBacklogFile.WaitAsync().ConfigureAwait(false);
        try
        {
            if (configManager is { AutoSaveBacklogBeforeClosing: true, MaxBacklogCapacity: not 0 })
            {
                await WritePendingItemsToBacklogFile().ConfigureAwait(false);
            }
            if (configManager.AutoSaveSessionStatsBeforeClosing)
            {
                await WriteSessionStats().ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LoggerManager.Logger.Error(ex, "Error writing pending items to backlog file");
            NotificationManager.Notify(NotificationLevel.Error, "Error writing pending items to backlog file. Check the logs for more details.");
        }
        finally
        {
            _ = s_semaphoreSlimForBacklogFile.Release();
        }
    }

    private static async Task WritePendingItemsToBacklogFile()
    {
        if (s_backlogFilePathToRestore is not null)
        {
            if (File.Exists(s_backlogFilePathToRestore))
            {
                FileStream incompleteFile = new(s_backlogFilePathToRestore, FileMode.Open, FileAccess.Write, FileShare.Read, bufferSize: 1, options: FileOptions.Asynchronous);
                await using (incompleteFile.ConfigureAwait(false))
                {
                    // Repair the failed append even if its pending entries have since been cleared.
                    incompleteFile.SetLength(Math.Min(s_backlogFileLengthToRestore, incompleteFile.Length));
                }
            }
            s_backlogFilePathToRestore = null;
        }

        lock (s_pendingItemsLock)
        {
            if (s_pendingBacklogItemsForBacklogFile.Count is 0)
            {
                return;
            }
        }

        _ = Directory.CreateDirectory(s_backlogDirectory);
        string tempBacklogPath = GetBacklogFilePath(false, "");
        bool addTimestamps = ConfigManager.Instance.SaveBacklogTimestamps;
        char[]? headerBuffer = null;
        (LinkedListNode<LinkedListNode<BacklogItem>> Node, string Text)[]? pendingItems = null;
        int pendingItemCount = 0;
        try
        {
            // StreamWriter buffers the output, so FileStream does not need another buffer.
            FileStream fileStream = new(tempBacklogPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, bufferSize: 1, options: FileOptions.Asynchronous);
            await using (fileStream.ConfigureAwait(false))
            {
                long originalLength = fileStream.Length;
                fileStream.Position = originalLength;
                lock (s_pendingItemsLock)
                {
                    pendingItems = ArrayPool<(LinkedListNode<LinkedListNode<BacklogItem>> Node, string Text)>.Shared.Rent(s_pendingBacklogItemsForBacklogFile.Count);
                    LinkedListNode<LinkedListNode<BacklogItem>>? currentNode = s_pendingBacklogItemsForBacklogFile.First;
                    while (currentNode is not null)
                    {
                        pendingItems[pendingItemCount++] = (currentNode, currentNode.Value.Value.Text);
                        currentNode = currentNode.Next;
                    }
                }

                if (pendingItemCount > 0)
                {
                    if (addTimestamps)
                    {
                        headerBuffer = ArrayPool<char>.Shared.Rent(RecordSeparator.Length + 22);
                        RecordSeparator.AsSpan().CopyTo(headerBuffer);
                        headerBuffer[RecordSeparator.Length] = '[';
                        headerBuffer[RecordSeparator.Length + 20] = ']';
                        headerBuffer[RecordSeparator.Length + 21] = '\n';
                    }

                    s_backlogFilePathToRestore = tempBacklogPath;
                    s_backlogFileLengthToRestore = originalLength;
                    StreamWriter writer = new(fileStream, TextUtils.Utf8NoBom, 4096, leaveOpen: true);
                    await using (writer.ConfigureAwait(false))
                    {
                        bool appendRecordSeparator = originalLength > 0;
                        for (int i = 0; i < pendingItemCount; i++)
                        {
                            (LinkedListNode<LinkedListNode<BacklogItem>> node, string text) = pendingItems[i];
                            if (headerBuffer is not null)
                            {
                                // Merging and recalculating statistics leave the timestamp unchanged.
                                _ = node.Value.Value.Timestamp.TryFormat(headerBuffer.AsSpan(RecordSeparator.Length + 1, 19), out _, "yyyy.MM.dd HH:mm:ss", CultureInfo.InvariantCulture);
                                int headerOffset = appendRecordSeparator ? 0 : RecordSeparator.Length;
                                await writer.WriteAsync(headerBuffer.AsMemory(headerOffset, RecordSeparator.Length + 22 - headerOffset)).ConfigureAwait(false);
                            }
                            else if (appendRecordSeparator)
                            {
                                await writer.WriteAsync(RecordSeparator).ConfigureAwait(false);
                            }

                            await writer.WriteAsync(text).ConfigureAwait(false);
                            appendRecordSeparator = true;
                        }
                    }
                }
            }

            s_backlogFilePathToRestore = null;
            lock (s_pendingItemsLock)
            {
                foreach ((LinkedListNode<LinkedListNode<BacklogItem>> node, string text) in pendingItems.AsSpan(0, pendingItemCount))
                {
                    if (node.List == s_pendingBacklogItemsForBacklogFile && node.Value.Value.Text == text)
                    {
                        s_pendingBacklogItemsForBacklogFile.Remove(node);
                    }
                }
            }
        }
        finally
        {
            if (pendingItems is not null)
            {
                pendingItems.AsSpan(0, pendingItemCount).Clear();
                ArrayPool<(LinkedListNode<LinkedListNode<BacklogItem>> Node, string Text)>.Shared.Return(pendingItems);
            }
            if (headerBuffer is not null)
            {
                ArrayPool<char>.Shared.Return(headerBuffer);
            }
        }
    }

    private static string GetBacklogFilePath(bool permanent, string suffix)
    {
        string fileName = permanent
            ? string.Create(CultureInfo.InvariantCulture, $"{ProfileUtils.CurrentProfileName}_{ProfileUtils.CurrentProfileSessionStartTime:yyyy.MM.dd_HH.mm.ss}-{DateTime.Now:yyyy.MM.dd_HH.mm.ss}{suffix}.txt")
            : string.Create(CultureInfo.InvariantCulture, $"{ProfileUtils.CurrentProfileName}_{ProfileUtils.CurrentProfileSessionStartTime:yyyy.MM.dd_HH.mm.ss}{suffix}.txt");

        return Path.Join(s_backlogDirectory, fileName);
    }

    private static async Task WriteSessionStats()
    {
        _ = Directory.CreateDirectory(s_backlogDirectory);
        string tempStatsFilePath = GetBacklogFilePath(false, "_Stats");

        Stats sessionStats = StatsUtils.SessionStats;
        string sessionStatsStr;
        if (CoreConfigManager.Instance.TrackTermLookupCounts)
        {
            KeyValuePair<string, int>[] lookupStats;
            lock (StatsUtils.TermLookupCountsLock)
            {
                lookupStats = sessionStats.TermLookupCountDict.ToArray();
            }

            if (lookupStats.Length > 0)
            {
                StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
                _ = sb.Append(sessionStats).Append("\n\n");
                _ = sb.Append("Term\tLookup Count\n");
                foreach ((string term, int count) in lookupStats)
                {
                    _ = sb.Append(CultureInfo.InvariantCulture, $"{term}\t{count}\n");
                }

                sessionStatsStr = sb.ToString();
                ObjectPoolManager.StringBuilderPool.Return(sb);
            }
            else
            {
                sessionStatsStr = sessionStats.ToString();
            }
        }
        else
        {
            sessionStatsStr = sessionStats.ToString();
        }

        await File.WriteAllTextAsync(tempStatsFilePath, sessionStatsStr).ConfigureAwait(false);
    }

    public static async Task WriteBacklog()
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager is { AutoSaveBacklogBeforeClosing: false, AutoSaveSessionStatsBeforeClosing: false })
        {
            return;
        }

        await s_semaphoreSlimForBacklogFile.WaitAsync().ConfigureAwait(false);
        try
        {
            if (configManager is { AutoSaveBacklogBeforeClosing: true, MaxBacklogCapacity: not 0 })
            {
                await WritePendingItemsToBacklogFile().ConfigureAwait(false);
                string tempBacklogPath = GetBacklogFilePath(false, "");
                if (File.Exists(tempBacklogPath))
                {
                    string permanentBacklogPath = GetBacklogFilePath(true, "");
                    File.Move(tempBacklogPath, permanentBacklogPath);
                }
            }
            if (configManager.AutoSaveSessionStatsBeforeClosing)
            {
                await WriteSessionStats().ConfigureAwait(false);
                string tempStatsPath = GetBacklogFilePath(false, "_Stats");
                if (File.Exists(tempStatsPath))
                {
                    string permanentStatsPath = GetBacklogFilePath(true, "_Stats");
                    File.Move(tempStatsPath, permanentStatsPath);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LoggerManager.Logger.Error(ex, "Error writing the backlog file");
            NotificationManager.Notify(NotificationLevel.Error, "Error writing the backlog file. Check the logs for more details.");
        }
        finally
        {
            _ = s_semaphoreSlimForBacklogFile.Release();
        }
    }

    public static void ClearBacklog()
    {
        string? lastText = s_backlog.Last?.Value.Text;
        s_backlog.Clear();
        s_currentNode = null;

        s_uniqueBacklogItems.Clear();
        lock (s_pendingItemsLock)
        {
            s_pendingBacklogItemsForBacklogFile.Clear();
        }

        if (lastText is not null)
        {
            MainWindow mainWindow = MainWindow.Instance;
            TextBox mainTextBox = mainWindow.MainTextBox;
            mainTextBox.Foreground = ConfigManager.Instance.MainWindowTextColor;
            mainTextBox.Text = lastText;
            mainWindow.UpdatePosition();
        }
    }

    public static void ClearUniqueBacklogItems()
    {
        s_uniqueBacklogItems.Clear();
    }

    public static void TrimBacklog()
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.MaxBacklogCapacity > 0 && s_backlog.Count - 1 > configManager.MaxBacklogCapacity)
        {
            bool changeCurrentNodeToLast = false;
            do
            {
                LinkedListNode<BacklogItem>? firstNode = s_backlog.First;
                changeCurrentNodeToLast = changeCurrentNodeToLast || firstNode == s_currentNode;
                if (firstNode is not null)
                {
                    _ = s_uniqueBacklogItems.Remove(firstNode.Value.Text);
                }

                s_backlog.RemoveFirst();
            } while (s_backlog.Count - 1 > configManager.MaxBacklogCapacity);

            if (changeCurrentNodeToLast)
            {
                s_currentNode = s_backlog.Last;
                Debug.Assert(s_currentNode is not null);

                MainWindow mainWindow = MainWindow.Instance;
                TextBox mainTextBox = mainWindow.MainTextBox;
                mainTextBox.Foreground = configManager.MainWindowTextColor;
                mainTextBox.Text = s_currentNode.Value.Text;
                mainWindow.UpdatePosition();
            }
        }
    }

    private static BacklogStats AddBacklogStats(BacklogStats backlogStats, int resetCount, int characterCount, int characterCountWithoutPunctuation)
    {
        return backlogStats.ResetCount != resetCount
            ? new BacklogStats(characterCount, characterCountWithoutPunctuation, resetCount, false, false)
            : backlogStats with
            {
                CharacterCount = backlogStats.CharacterCount + characterCount,
                CharacterCountWithoutPunctuation = backlogStats.CharacterCountWithoutPunctuation + characterCountWithoutPunctuation
            };
    }

    public static void RecalculateCharacterCountStats()
    {
        bool stripPunctuation = ConfigManager.Instance.StripPunctuationBeforeCalculatingCharacterCount;
        lock (s_pendingItemsLock)
        {
            LinkedListNode<BacklogItem>? node = s_backlog.First;
            while (node is not null)
            {
                BacklogItem item = node.Value;
                node.Value = new BacklogItem(item.Text, item.Timestamp,
                    RecalculateBacklogStats(StatsUtils.SessionStats, item.SessionStats, stripPunctuation),
                    RecalculateBacklogStats(StatsUtils.ProfileLifetimeStats, item.ProfileStats, stripPunctuation),
                    RecalculateBacklogStats(StatsUtils.LifetimeStats, item.LifetimeStats, stripPunctuation));
                node = node.Next;
            }
        }
    }

    private static BacklogStats RecalculateBacklogStats(Stats stats, BacklogStats backlogStats, bool stripPunctuation)
    {
        if (backlogStats.ResetCount != stats.ResetCount)
        {
            return new BacklogStats(0, 0, stats.ResetCount, false, false);
        }

        long difference = stripPunctuation
            ? (long)backlogStats.CharacterCountWithoutPunctuation - backlogStats.CharacterCount
            : (long)backlogStats.CharacterCount - backlogStats.CharacterCountWithoutPunctuation;
        if (difference > 0)
        {
            stats.Characters += (ulong)difference;
        }
        else if (difference < 0)
        {
            stats.Characters -= (ulong)-difference;
        }

        int characterCount = stripPunctuation ? backlogStats.CharacterCountWithoutPunctuation : backlogStats.CharacterCount;
        bool lineCounted = backlogStats.CountLine && characterCount > 0;
        if (lineCounted != backlogStats.LineCounted)
        {
            if (lineCounted)
            {
                ++stats.Lines;
            }
            else
            {
                --stats.Lines;
            }
        }

        return backlogStats with { LineCounted = lineCounted };
    }

    private static void SubtractBacklogStats(Stats stats, BacklogStats backlogStats, bool stripPunctuation)
    {
        if (backlogStats.ResetCount != stats.ResetCount)
        {
            return;
        }

        int characterCount = stripPunctuation
            ? backlogStats.CharacterCountWithoutPunctuation
            : backlogStats.CharacterCount;
        if (characterCount > 0)
        {
            stats.Characters -= (ulong)characterCount;
        }
        if (backlogStats.LineCounted)
        {
            --stats.Lines;
        }
    }

    public static (string sourceText, int charIndexForSourceText) GetSourceTextFromIndexPosition(string text, int currentCharIndex)
    {
        int startIndex = text.LastIndexOf(RecordSeparator, currentCharIndex, StringComparison.Ordinal);
        startIndex = startIndex < 0
            ? 0
            : startIndex + RecordSeparator.Length;

        int endIndex = text.IndexOf(RecordSeparator, currentCharIndex, StringComparison.Ordinal);
        endIndex = endIndex < 0
            ? text.Length
            : endIndex;

        return (text[startIndex..endIndex], currentCharIndex - startIndex);
    }
}
