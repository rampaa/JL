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
    private static int s_currentTextIndex; // 0
    public static List<string> Backlog { get; } = [];

    public static void AddToBacklog(string text)
    {
        Backlog.Add(text);
        s_currentTextIndex = Backlog.Count - 1;
    }

    public static void ReplaceLastBacklogText(string text)
    {
        Backlog[^1] = text;
    }

    public static void ShowPreviousBacklogItem()
    {
        if (!ConfigManager.EnableBacklog || MainWindow.Instance.FirstPopupWindow.MiningMode || Backlog.Count is 0)
        {
            return;
        }

        if (s_currentTextIndex > 0)
        {
            --s_currentTextIndex;
            MainWindow.Instance.MainTextBox.Foreground = ConfigManager.MainWindowBacklogTextColor;
        }

        MainWindow.Instance.MainTextBox.Text = Backlog[s_currentTextIndex];
    }

    public static void ShowNextBacklogItem()
    {
        if (!ConfigManager.EnableBacklog || MainWindow.Instance.FirstPopupWindow.MiningMode || Backlog.Count is 0)
        {
            return;
        }

        if (s_currentTextIndex < Backlog.Count - 1)
        {
            ++s_currentTextIndex;
            MainWindow.Instance.MainTextBox.Foreground = ConfigManager.MainWindowBacklogTextColor;
        }

        if (s_currentTextIndex == Backlog.Count - 1)
        {
            MainWindow.Instance.MainTextBox.Foreground = ConfigManager.MainWindowTextColor;
        }

        MainWindow.Instance.MainTextBox.Text = Backlog[s_currentTextIndex];
    }

    public static void DeleteCurrentLine()
    {
        TextBox mainTextBox = MainWindow.Instance.MainTextBox;
        if (Backlog.Count is 0)
        {
            return;
        }

        string text = Backlog[s_currentTextIndex];
        if (text != mainTextBox.Text)
        {
            return;
        }

        if (ConfigManager.StripPunctuationBeforeCalculatingCharacterCount)
        {
            text = JapaneseUtils.RemovePunctuation(text);
        }

        if (text.Length > 0)
        {
            Stats.IncrementStat(StatType.Lines, -1);

            int textLength = new StringInfo(text).LengthInTextElements;
            Stats.IncrementStat(StatType.Characters, -textLength);
        }

        Backlog.RemoveAt(s_currentTextIndex);
        if (s_currentTextIndex > 0)
        {
            --s_currentTextIndex;
        }

        mainTextBox.Foreground = s_currentTextIndex < Backlog.Count - 1
            ? ConfigManager.MainWindowBacklogTextColor
            : ConfigManager.MainWindowTextColor;

        mainTextBox.Text = Backlog.Count > 0
            ? Backlog[s_currentTextIndex]
            : "";
    }

    public static void ShowAllBacklog()
    {
        if (!ConfigManager.EnableBacklog || MainWindow.Instance.FirstPopupWindow.MiningMode || Backlog.Count is 0)
        {
            return;
        }

        string allBacklogText = string.Join('\n', Backlog);
        if (MainWindow.Instance.MainTextBox.Text != allBacklogText
            && MainWindow.Instance.MainTextBox.GetFirstVisibleLineIndex() is 0)
        {
            int caretIndex = allBacklogText.Length - MainWindow.Instance.MainTextBox.Text.Length;

            MainWindow.Instance.MainTextBox.Text = allBacklogText;
            MainWindow.Instance.MainTextBox.Foreground = ConfigManager.MainWindowBacklogTextColor;

            if (caretIndex >= 0)
            {
                MainWindow.Instance.MainTextBox.CaretIndex = caretIndex;
            }

            MainWindow.Instance.MainTextBox.ScrollToEnd();
        }
    }

    public static Task WriteBacklog()
    {
        if (!ConfigManager.EnableBacklog
            || !ConfigManager.AutoSaveBacklogBeforeClosing
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
}
