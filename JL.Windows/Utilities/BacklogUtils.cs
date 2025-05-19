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
    private static LinkedListNode<string>? s_currentNode;
    public static LinkedList<string> Backlog { get; } = [];

    public static void AddToBacklog(string text)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.MaxBacklogCapacity is not -1 && Backlog.Count > configManager.MaxBacklogCapacity)
        {
            Backlog.RemoveFirst();
        }

        s_currentNode = Backlog.AddLast(text);
    }

    public static void ReplaceLastBacklogText(string text)
    {
        if (Backlog.Last is not null)
        {
            Backlog.Last.Value = text;
        }
        else
        {
            s_currentNode = Backlog.AddLast(text);
        }
    }

    public static void ShowPreviousBacklogItem()
    {
        ConfigManager configManager = ConfigManager.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        if (s_currentNode is null || mainWindow.FirstPopupWindow.MiningMode)
        {
            return;
        }

        if (s_currentNode.Previous is not null)
        {
            mainWindow.MainTextBox.Foreground = configManager.MainWindowBacklogTextColor;
            mainWindow.MainTextBox.Text = s_currentNode.Previous.Value;
            s_currentNode = s_currentNode.Previous;
        }
    }

    public static void ShowNextBacklogItem()
    {
        ConfigManager configManager = ConfigManager.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        if (s_currentNode is null || mainWindow.FirstPopupWindow.MiningMode)
        {
            return;
        }

        if (s_currentNode.Next is not null)
        {
            mainWindow.MainTextBox.Foreground = s_currentNode.Next != Backlog.Last
                ? configManager.MainWindowBacklogTextColor
                : configManager.MainWindowTextColor;

            mainWindow.MainTextBox.Text = s_currentNode.Next.Value;
            s_currentNode = s_currentNode.Next;
        }
    }

    public static void DeleteCurrentLine()
    {
        TextBox mainTextBox = MainWindow.Instance.MainTextBox;
        if (s_currentNode is null)
        {
            return;
        }

        string text = s_currentNode.Value;
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

        LinkedListNode<string>? newCurrentNode = s_currentNode.Previous ?? Backlog.Last;
        Backlog.Remove(s_currentNode);
        s_currentNode = newCurrentNode;

        mainTextBox.Foreground = newCurrentNode != Backlog.Last
            ? configManager.MainWindowBacklogTextColor
            : configManager.MainWindowTextColor;

        mainTextBox.Text = newCurrentNode is not null
            ? newCurrentNode.Value
            : "";
    }

    public static void ShowAllBacklog()
    {
        ConfigManager configManager = ConfigManager.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        if (configManager.MaxBacklogCapacity is 0 || mainWindow.FirstPopupWindow.MiningMode || Backlog.Count is 0)
        {
            return;
        }

        string allBacklogText = string.Join('\n', Backlog);
        if (mainWindow.MainTextBox.Text != allBacklogText
            && mainWindow.MainTextBox.GetFirstVisibleLineIndex() is 0)
        {
            int caretIndex = allBacklogText.Length - mainWindow.MainTextBox.Text.Length;

            mainWindow.MainTextBox.Text = allBacklogText;
            mainWindow.MainTextBox.Foreground = configManager.MainWindowBacklogTextColor;

            if (caretIndex >= 0)
            {
                mainWindow.MainTextBox.CaretIndex = caretIndex;
            }

            mainWindow.MainTextBox.ScrollToEnd();
        }
    }

    public static Task WriteBacklog()
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.MaxBacklogCapacity is 0
            || !configManager.AutoSaveBacklogBeforeClosing
            || Backlog.Count is 0)
        {
            return Task.CompletedTask;
        }

        string directory = Path.Join(Utils.ApplicationPath, "Backlogs");
        if (!Directory.Exists(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        return File.WriteAllLinesAsync(Path.Join(directory, string.Create(CultureInfo.InvariantCulture, $"{ProfileUtils.CurrentProfileName}_{Process.GetCurrentProcess().StartTime:yyyy.MM.dd_HH.mm.ss}-{DateTime.Now:yyyy.MM.dd_HH.mm.ss}.txt")), Backlog);
    }

    public static void ClearBacklog()
    {
        string? lastText = Backlog.Last?.Value;
        Backlog.Clear();
        s_currentNode = null;

        if (lastText is not null)
        {
            TextBox mainTextBox = MainWindow.Instance.MainTextBox;
            ConfigManager configManager = ConfigManager.Instance;

            mainTextBox.Foreground = configManager.MainWindowTextColor;
            mainTextBox.Text = lastText;
        }
    }

    public static void TrimBacklog()
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.MaxBacklogCapacity > 0)
        {
            bool changeCurrentNodeToLast = false;
            while (Backlog.Count > configManager.MaxBacklogCapacity)
            {
                changeCurrentNodeToLast |= changeCurrentNodeToLast || Backlog.Last == s_currentNode;
                Backlog.RemoveLast();
            }

            if (changeCurrentNodeToLast)
            {
                s_currentNode = Backlog.Last;
                Debug.Assert(s_currentNode is not null);
                TextBox mainTextBox = MainWindow.Instance.MainTextBox;
                mainTextBox.Foreground = configManager.MainWindowTextColor;
                mainTextBox.Text = s_currentNode.Value;
            }
        }
    }
}
