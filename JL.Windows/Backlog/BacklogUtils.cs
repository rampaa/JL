using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Controls;
using JL.Core;
using JL.Core.Config;
using JL.Core.Frontend;
using JL.Core.Japanese;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Core.Utilities.ObjectPool;
using JL.Windows.Config;
using JL.Windows.GUI;
using JL.Windows.Utilities;
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

    public static void AddToBacklog(string text)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.MaxBacklogCapacity is not -1 && s_backlog.Count > configManager.MaxBacklogCapacity)
        {
            s_backlog.RemoveFirst();
        }

        s_currentNode = s_backlog.AddLast(new BacklogItem(text, DateTime.Now));
        if (configManager.AutoSaveBacklogBeforeClosing && configManager.MaxBacklogCapacity is not 0)
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

    public static void AddToBacklogShowAllBacklog(string text)
    {
        ConfigManager configManager = ConfigManager.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        TextBox mainTextBox = mainWindow.MainTextBox;

        bool removeOldestItem = configManager.MaxBacklogCapacity is not -1 && s_backlog.Count > configManager.MaxBacklogCapacity;

        BacklogItem item = new(text, DateTime.Now);
        s_currentNode = s_backlog.AddLast(item);
        if (configManager.AutoSaveBacklogBeforeClosing && configManager.MaxBacklogCapacity is not 0)
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

    public static void ReplaceLastBacklogText(string text)
    {
        LinkedListNode<BacklogItem>? lastNode = s_backlog.Last;
        if (lastNode is not null)
        {
            lock (s_pendingItemsLock)
            {
                lastNode.Value = new BacklogItem(text, lastNode.Value.Timestamp);
            }
        }
        else
        {
            s_currentNode = s_backlog.AddLast(new BacklogItem(text, DateTime.Now));
            ConfigManager configManager = ConfigManager.Instance;
            if (configManager.AutoSaveBacklogBeforeClosing && configManager.MaxBacklogCapacity is not 0)
            {
                lock (s_pendingItemsLock)
                {
                    _ = s_pendingBacklogItemsForBacklogFile.AddLast(s_currentNode);
                }
            }
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

        string displayText = s_currentNode.Value.Text;
        if (displayText != mainTextBox.Text)
        {
            return;
        }

        string text = s_currentNode.Value.Text;
        if (configManager.StripPunctuationBeforeCalculatingCharacterCount)
        {
            text = JapaneseUtils.RemovePunctuation(text);
        }

        if (text.Length > 0)
        {
            StatsUtils.IncrementStat(StatType.Lines, -1);
            int textLength = text.GetGraphemeCount();
            StatsUtils.IncrementStat(StatType.Characters, -textLength);
        }

        LinkedListNode<BacklogItem>? newCurrentNode = s_currentNode.Previous ?? s_currentNode.Next;
        _ = s_uniqueBacklogItems.Remove(s_currentNode.Value.Text);
        s_backlog.Remove(s_currentNode);

        lock (s_pendingItemsLock)
        {
            _ = s_pendingBacklogItemsForBacklogFile.Remove(s_currentNode);
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
            if (configManager.AutoSaveBacklogBeforeClosing && configManager.MaxBacklogCapacity is not 0)
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
            WindowsUtils.Alert(AlertLevel.Error, "Error writing pending items to backlog file. Check the logs for more details.");
        }
        finally
        {
            _ = s_semaphoreSlimForBacklogFile.Release();
        }
    }

    private static async Task WritePendingItemsToBacklogFile()
    {
        string tempBacklogPath = GetBacklogFilePath(false, "");
        bool appendRecordSeparator = File.Exists(tempBacklogPath);
        bool addTimestamps = ConfigManager.Instance.SaveBacklogTimestamps;

        StringBuilder stringBuilder = ObjectPoolManager.StringBuilderPool.Get();
        try
        {
            lock (s_pendingItemsLock)
            {
                LinkedListNode<LinkedListNode<BacklogItem>>? currentNode = s_pendingBacklogItemsForBacklogFile.First;
                while (currentNode is not null)
                {
                    if (appendRecordSeparator)
                    {
                        _ = stringBuilder.Append(RecordSeparator);
                    }

                    BacklogItem backlogItem = currentNode.Value.Value;
                    if (addTimestamps)
                    {
                        _ = stringBuilder.Append(string.Create(CultureInfo.InvariantCulture, $"[{backlogItem.Timestamp:yyyy.MM.dd HH:mm:ss}]\n{backlogItem.Text}"));
                    }
                    else
                    {
                        _ = stringBuilder.Append(backlogItem.Text);
                    }

                    appendRecordSeparator = true;

                    LinkedListNode<LinkedListNode<BacklogItem>>? nextNode = currentNode.Next;

                    s_pendingBacklogItemsForBacklogFile.Remove(currentNode);

                    currentNode = nextNode;
                }
            }

            if (stringBuilder.Length > 0)
            {
                await File.AppendAllTextAsync(tempBacklogPath, stringBuilder.ToString()).ConfigureAwait(false);
            }
        }
        finally
        {
            ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
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
        string tempStatsFilePath = GetBacklogFilePath(false, "_Stats");
        await File.WriteAllTextAsync(tempStatsFilePath, StatsUtils.SessionStats.ToString()).ConfigureAwait(false);
    }

    public static async Task WriteBacklog()
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (!configManager.AutoSaveBacklogBeforeClosing && !configManager.AutoSaveSessionStatsBeforeClosing)
        {
            return;
        }

        await s_semaphoreSlimForBacklogFile.WaitAsync().ConfigureAwait(false);
        try
        {
            if (configManager.AutoSaveBacklogBeforeClosing && configManager.MaxBacklogCapacity is not 0)
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
            WindowsUtils.Alert(AlertLevel.Error, "Error writing the backlog file. Check the logs for more details.");
        }
        finally
        {
            _ = s_semaphoreSlimForBacklogFile.Release();
        }
    }

    public static void ClearBacklog()
    {
        BacklogItem? lastItem = s_backlog.Last?.Value;
        s_backlog.Clear();
        s_currentNode = null;

        s_uniqueBacklogItems.Clear();
        lock (s_pendingItemsLock)
        {
            s_pendingBacklogItemsForBacklogFile.Clear();
        }

        if (lastItem is not null)
        {
            MainWindow mainWindow = MainWindow.Instance;
            TextBox mainTextBox = mainWindow.MainTextBox;
            mainTextBox.Foreground = ConfigManager.Instance.MainWindowTextColor;
            mainTextBox.Text = lastItem.Value.Text;
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
        if (configManager.MaxBacklogCapacity > 0 && s_backlog.Count > configManager.MaxBacklogCapacity)
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
            } while (s_backlog.Count > configManager.MaxBacklogCapacity);

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

    public static void RecalculateCharacterCountStats()
    {
        if (s_backlog.Count is 0)
        {
            return;
        }

        ulong characterCount = 0;
        ulong lineCount = 0;

        ConfigManager configManager = ConfigManager.Instance;
        LinkedListNode<BacklogItem>? currentBacklogNode = s_backlog.First;
        while (currentBacklogNode is not null)
        {
            string text = currentBacklogNode.Value.Text;
            if (configManager.StripPunctuationBeforeCalculatingCharacterCount)
            {
                text = JapaneseUtils.RemovePunctuation(text);
            }

            if (text.Length > 0)
            {
                ++lineCount;
                characterCount += (ulong)text.GetGraphemeCount();
            }

            currentBacklogNode = currentBacklogNode.Next;
        }

        if (configManager.StripPunctuationBeforeCalculatingCharacterCount)
        {
            StatsUtils.IncrementStat(StatType.Characters, -(long)(StatsUtils.SessionStats.Characters - characterCount));
            StatsUtils.IncrementStat(StatType.Lines, -(long)(StatsUtils.SessionStats.Lines - lineCount));
        }
        else
        {
            StatsUtils.IncrementStat(StatType.Characters, (long)(characterCount - StatsUtils.SessionStats.Characters));
            StatsUtils.IncrementStat(StatType.Lines, (long)(lineCount - StatsUtils.SessionStats.Lines));
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
