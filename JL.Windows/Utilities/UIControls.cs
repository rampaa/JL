using System.Windows.Controls;

namespace JL.Windows.Utilities
{
    // Creating a UI control within ConfigManager makes half of the tests fail
    // Because apparently UI controls can only be created in a UI thread and tests don't like that.
    public static class UiControls
    {
        public static readonly List<ComboBoxItem> JapaneseFonts =
            WindowsUtils.FindJapaneseFonts().OrderByDescending(f => f.Foreground.ToString()).ThenBy(font => font.Content)
                .ToList();

        public static readonly List<ComboBoxItem> PopupJapaneseFonts =
            JapaneseFonts.ConvertAll(f => new ComboBoxItem()
            {
                Content = f.Content,
                FontFamily = f.FontFamily,
                Foreground = f.Foreground
            });
    }
}
