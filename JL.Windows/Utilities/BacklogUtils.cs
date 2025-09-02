using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Controls;
using JL.Core.Config;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.GUI;

namespace JL.Windows.Utilities;

internal static class BacklogUtils
{
    private static readonly LinkedList<string> s_backlog = [];
    private static LinkedListNode<string>? s_currentNode;
    public static string? LastItem => s_backlog.Last?.Value;

    public static void AddToBacklog(string text)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.MaxBacklogCapacity is not -1 && s_backlog.Count > configManager.MaxBacklogCapacity)
        {
            s_backlog.RemoveFirst();
        }

        s_currentNode = s_backlog.AddLast(text);
    }

    public static void ReplaceLastBacklogText(string text)
    {
        if (s_backlog.Last is not null)
        {
            s_backlog.Last.Value = text;
        }
        else
        {
            s_currentNode = s_backlog.AddLast(text);
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

        if (s_currentNode.Previous is not null)
        {
            mainWindow.MainTextBox.Foreground = ConfigManager.Instance.MainWindowBacklogTextColor;
            mainWindow.MainTextBox.Text = s_currentNode.Previous.Value;
            s_currentNode = s_currentNode.Previous;
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

        if (s_currentNode.Next is not null)
        {
            mainWindow.MainTextBox.Foreground = s_currentNode.Next != s_backlog.Last
                ? ConfigManager.Instance.MainWindowBacklogTextColor
                : ConfigManager.Instance.MainWindowTextColor;

            mainWindow.MainTextBox.Text = s_currentNode.Next.Value;
            s_currentNode = s_currentNode.Next;
        }
    }

    public static void DeleteCurrentLine()
    {
        if (s_currentNode is null)
        {
            return;
        }

        string text = s_currentNode.Value;
        TextBox mainTextBox = MainWindow.Instance.MainTextBox;
        if (text != mainTextBox.Text)
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.StripPunctuationBeforeCalculatingCharacterCount)
        {
            text = JapaneseUtils.RemovePunctuation(text);
        }

        if (text.Length > 0)
        {
            StatsUtils.IncrementStat(StatType.Lines, -1);

            int textLength = new StringInfo(text).LengthInTextElements;
            StatsUtils.IncrementStat(StatType.Characters, -textLength);
        }

        LinkedListNode<string>? newCurrentNode = s_currentNode.Previous ?? s_currentNode.Next;
        s_backlog.Remove(s_currentNode);
        s_currentNode = newCurrentNode;

        mainTextBox.Foreground = newCurrentNode != s_backlog.Last
            ? configManager.MainWindowBacklogTextColor
            : configManager.MainWindowTextColor;

        mainTextBox.Text = newCurrentNode is not null
            ? newCurrentNode.Value
            : "";
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

        string allBacklogText = string.Join('\n', s_backlog);
        if (mainWindow.MainTextBox.Text != allBacklogText
            && mainWindow.MainTextBox.GetFirstVisibleLineIndex() is 0)
        {
            int caretIndex = allBacklogText.Length - mainWindow.MainTextBox.Text.Length;

            mainWindow.MainTextBox.Text = allBacklogText;
            mainWindow.MainTextBox.Foreground = ConfigManager.Instance.MainWindowBacklogTextColor;

            if (caretIndex >= 0)
            {
                mainWindow.MainTextBox.CaretIndex = caretIndex;
            }

            mainWindow.MainTextBox.ScrollToEnd();
        }
    }

    public static Task WriteBacklog()
    {
        if (s_backlog.Count is 0 || !ConfigManager.Instance.AutoSaveBacklogBeforeClosing)
        {
            return Task.CompletedTask;
        }

        string directory = Path.Join(Utils.ApplicationPath, "Backlogs");
        if (!Directory.Exists(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        return File.WriteAllLinesAsync(Path.Join(directory, string.Create(CultureInfo.InvariantCulture, $"{ProfileUtils.CurrentProfileName}_{Process.GetCurrentProcess().StartTime:yyyy.MM.dd_HH.mm.ss}-{DateTime.Now:yyyy.MM.dd_HH.mm.ss}.txt")), s_backlog);
    }

    public static void ClearBacklog()
    {
        string? lastText = s_backlog.Last?.Value;
        s_backlog.Clear();
        s_currentNode = null;

        if (lastText is not null)
        {
            TextBox mainTextBox = MainWindow.Instance.MainTextBox;
            mainTextBox.Foreground = ConfigManager.Instance.MainWindowTextColor;
            mainTextBox.Text = lastText;
        }
    }

    public static void TrimBacklog()
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.MaxBacklogCapacity > 0)
        {
            bool changeCurrentNodeToLast = false;
            while (s_backlog.Count > configManager.MaxBacklogCapacity)
            {
                changeCurrentNodeToLast = changeCurrentNodeToLast || s_backlog.Last == s_currentNode;
                s_backlog.RemoveLast();
            }

            if (changeCurrentNodeToLast)
            {
                s_currentNode = s_backlog.Last;
                Debug.Assert(s_currentNode is not null);
                TextBox mainTextBox = MainWindow.Instance.MainTextBox;
                mainTextBox.Foreground = configManager.MainWindowTextColor;
                mainTextBox.Text = s_currentNode.Value;
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
        LinkedListNode<string>? currentBacklogNode = s_backlog.First;
        while (currentBacklogNode is not null)
        {
            string text = currentBacklogNode.Value;
            if (configManager.StripPunctuationBeforeCalculatingCharacterCount)
            {
                text = JapaneseUtils.RemovePunctuation(text);
            }

            if (text.Length > 0)
            {
                ++lineCount;
                characterCount += (ulong)new StringInfo(text).LengthInTextElements;
            }

            currentBacklogNode = currentBacklogNode.Previous;
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
}
