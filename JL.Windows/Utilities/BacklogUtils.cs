using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Controls;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.GUI;

namespace JL.Windows.Utilities;
internal static class BacklogUtils
{
    private static int s_currentTextIndex = 0;
    public static List<string> Backlog { get; } = new();

    public static void AddToBacklog(string text)
    {
        if (ConfigManager.EnableBacklog)
        {
            Backlog.Add(text);
            s_currentTextIndex = Backlog.Count - 1;
        }
    }

    public static void ShowPreviousBacklogItem()
    {
        MainWindow mainWindow = MainWindow.Instance;

        if (!ConfigManager.EnableBacklog || MainWindow.FirstPopupWindow.MiningMode)
        {
            return;
        }

        if (s_currentTextIndex > 0)
        {
            --s_currentTextIndex;
            mainWindow.MainTextBox.Foreground = ConfigManager.MainWindowBacklogTextColor;
        }

        mainWindow.MainTextBox.Text = Backlog[s_currentTextIndex];
    }

    public static void ShowNextBacklogItem()
    {
        MainWindow mainWindow = MainWindow.Instance;

        if (!ConfigManager.EnableBacklog || MainWindow.FirstPopupWindow.MiningMode)
        {
            return;
        }

        if (s_currentTextIndex < Backlog.Count - 1)
        {
            ++s_currentTextIndex;
            mainWindow.MainTextBox.Foreground = ConfigManager.MainWindowBacklogTextColor;
        }

        if (s_currentTextIndex == Backlog.Count - 1)
        {
            mainWindow.MainTextBox.Foreground = ConfigManager.MainWindowTextColor;
        }

        mainWindow.MainTextBox.Text = Backlog[s_currentTextIndex];
    }

    public static void DeleteCurrentLine()
    {
        TextBox mainTextBox = MainWindow.Instance.MainTextBox;

        if (Backlog.Count is 0 || mainTextBox.Text != Backlog[s_currentTextIndex])
        {
            return;
        }

        Stats.IncrementStat(StatType.Characters,
                new StringInfo(JapaneseUtils.RemovePunctuation(Backlog[s_currentTextIndex])).LengthInTextElements * -1);

        Stats.IncrementStat(StatType.Lines, -1);

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

    public static async Task WriteBacklog()
    {
        if (ConfigManager.EnableBacklog
            && ConfigManager.AutoSaveBacklogBeforeClosing
            && Backlog.Count > 0)
        {
            string directory = Path.Join(Utils.ApplicationPath, "Backlogs");
            if (!Directory.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }

            await File.WriteAllLinesAsync(Path.Join(directory, string.Create(CultureInfo.InvariantCulture, $"{Process.GetCurrentProcess().StartTime.ToString("yyyy.MM.dd_HH.mm.ss", CultureInfo.InvariantCulture)}-{DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss", CultureInfo.InvariantCulture)}.txt")), Backlog).ConfigureAwait(false);
        }
    }
}
