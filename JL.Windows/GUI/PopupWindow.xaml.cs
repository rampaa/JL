using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Caching;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Lookup;
using JL.Core.Pitch;
using JL.Core.Utilities;
using JL.Windows.GUI.View;
using JL.Windows.GUI.ViewModel;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for PopupWindow.xaml
/// </summary>
public partial class PopupWindow : Window
{
    // Other classes should only access popup by an interface, ideally
    public PopupViewModel Vm { get; }

    public PopupWindow? ParentPopupWindow { get; }

    public PopupWindow? ChildPopupWindow { get; set; }

    public TextBox? LastTextBox { get; set; }

    public ObservableCollection<OneResult> ResultStackPanels { get; } = new();

    public ObservableCollection<Button> DictTypeButtons { get; } = new();

    public static LRUCache<string, OneResult[]> StackPanelCache { get; } = new(
        Storage.CacheSize, Storage.CacheSize / 8);

    public PopupWindow()
    {
        InitializeComponent();
        Vm = new PopupViewModel();
        // DataContext = Vm; // todo
        Init();

        // need to initialize window (position) for later
        Show();
        Hide();
    }

    public PopupWindow(PopupWindow parentPopUp) : this()
    {
        ParentPopupWindow = parentPopUp;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        Vm.WinApi = new(this);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        if (!ConfigManager.Focusable)
        {
            Vm.WinApi!.PreventFocus();
        }
    }

    private void Init()
    {
        Background = ConfigManager.PopupBackgroundColor;
        FontFamily = ConfigManager.PopupFont;

        WindowsUtils.SetSizeToContentForPopup(ConfigManager.PopupDynamicWidth, ConfigManager.PopupDynamicHeight, WindowsUtils.DpiAwarePopupMaxWidth, WindowsUtils.DpiAwarePopupMaxHeight, this);

        WindowsUtils.SetInputGestureText(AddNameButton!, ConfigManager.ShowAddNameWindowKeyGesture);
        WindowsUtils.SetInputGestureText(AddWordButton!, ConfigManager.ShowAddWordWindowKeyGesture);
        WindowsUtils.SetInputGestureText(SearchButton!, ConfigManager.SearchWithBrowserKeyGesture);
        WindowsUtils.SetInputGestureText(StatsButton!, ConfigManager.ShowStatsKeyGesture);

        if (ConfigManager.ShowMiningModeReminder)
        {
            TextBlockMiningModeReminder!.Text =
                $"Click on an entry's main spelling to mine it," + Environment.NewLine +
                $"or press {ConfigManager.ClosePopupKeyGesture.Key} or click on the main window to exit.";
        }
    }

    private void AddName(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowAddNameWindow(Vm.LastSelectedText);
    }

    private void AddWord(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowAddWordWindow(Vm.LastSelectedText);
    }

    private void SearchWithBrowser(object sender, RoutedEventArgs e)
    {
        WindowsUtils.SearchWithBrowser(Vm.LastSelectedText);
    }

    private void ShowStats(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowStatsWindow();
    }

    public async void TextBox_MouseMove(TextBox tb)
    {
        if (Vm.MiningMode || ConfigManager.InactiveLookupMode
                          || (ConfigManager.RequireLookupKeyPress && !WindowsUtils.KeyGestureComparer(ConfigManager.LookupKeyKeyGesture))
                          || (ConfigManager.FixedPopupPositioning && ParentPopupWindow != null)
           )
            return;

        int charPosition = tb.GetCharacterIndexFromPoint(Mouse.GetPosition(tb), false);
        if (charPosition != -1)
        {
            if (charPosition > 0 && char.IsHighSurrogate(tb.Text[charPosition - 1]))
                --charPosition;

            Vm.CurrentText = tb.Text;
            Vm.CurrentCharPosition = charPosition;

            if (ParentPopupWindow != null
                && ConfigManager.DisableLookupsForNonJapaneseCharsInPopups
                && !Storage.JapaneseRegex.IsMatch(tb.Text[charPosition].ToString()))
            {
                if (ConfigManager.HighlightLongestMatch)
                {
                    WindowsUtils.Unselect(LastTextBox);
                }

                Visibility = Visibility.Hidden;
                return;
            }

            int endPosition = tb.Text.Length - charPosition > ConfigManager.MaxSearchLength
                ? Utils.FindWordBoundary(tb.Text[..(charPosition + ConfigManager.MaxSearchLength)], charPosition)
                : Utils.FindWordBoundary(tb.Text, charPosition);

            string text = tb.Text[charPosition..endPosition];

            if (text == Vm.LastText && IsVisible) return;
            Vm.LastText = text;

            ResultStackPanels.Clear();
            List<LookupResult>? lookupResults = Lookup.LookupText(text);

            if (lookupResults is { Count: > 0 })
            {
                LastTextBox = tb;
                Vm.LastSelectedText = lookupResults[0].MatchedText;
                if (ConfigManager.HighlightLongestMatch)
                {
                    double verticalOffset = tb.VerticalOffset;

                    if (ConfigManager.PopupFocusOnLookup
                        || ConfigManager.LookupOnLeftClickOnly
                        || ParentPopupWindow != null)
                    {
                        tb.Focus();
                    }

                    tb.Select(charPosition, lookupResults[0].MatchedText.Length);
                    tb.ScrollToVerticalOffset(verticalOffset);
                }

                Init();

                Vm.LastLookupResults = lookupResults;

                if (ConfigManager.LookupOnLeftClickOnly)
                {
                    EnableMiningMode();
                    DisplayResults(true, text);
                }

                else
                {
                    DisplayResults(false, text);
                }

                Visibility = Visibility.Visible;

                if (ConfigManager.PopupFocusOnLookup
                    || ConfigManager.LookupOnLeftClickOnly
                    || ParentPopupWindow != null)
                {
                    tb.Focus();
                    Activate();
                    Focus();
                }

                if (ConfigManager.AutoPlayAudio)
                {
                    await PlayAudio().ConfigureAwait(false);
                }
            }
            else
            {
                Vm.LastText = "";
                Visibility = Visibility.Hidden;

                //if (ConfigManager.HighlightLongestMatch)
                //{
                //    //Unselect(tb);
                //}
            }
        }
        else
        {
            Vm.LastText = "";
            Visibility = Visibility.Hidden;

            if (ConfigManager.HighlightLongestMatch)
            {
                WindowsUtils.Unselect(tb);
            }
        }
    }

    public async void LookupOnSelect(TextBox tb)
    {
        if (string.IsNullOrWhiteSpace(tb.SelectedText))
            return;

        LastTextBox = tb;

        PopUpScrollViewer!.ScrollToTop();

        List<LookupResult>? lookupResults = Lookup.LookupText(tb.SelectedText);

        if (lookupResults?.Any() ?? false)
        {
            ResultStackPanels.Clear();

            Init();

            Vm.LastLookupResults = lookupResults;
            EnableMiningMode();
            DisplayResults(true, tb.SelectedText);

            Visibility = Visibility.Visible;

            tb.Focus();
            Activate();
            Focus();

            if (ConfigManager.AutoPlayAudio)
            {
                await PlayAudio();
            }
        }
        else
        {
            Visibility = Visibility.Hidden;
        }
    }

    public void UpdatePosition(Point cursorPosition)
    {
        double mouseX = cursorPosition.X / WindowsUtils.Dpi.DpiScaleX;
        double mouseY = cursorPosition.Y / WindowsUtils.Dpi.DpiScaleY;

        bool needsFlipX = ConfigManager.PopupFlipX && mouseX + Width > WindowsUtils.DpiAwareWorkAreaWidth;
        bool needsFlipY = ConfigManager.PopupFlipY && mouseY + Height > WindowsUtils.DpiAwareWorkAreaHeight;

        double newLeft;
        double newTop;

        Vm.UnavoidableMouseEnter = false;

        if (needsFlipX)
        {
            // flip Leftwards while preventing -OOB
            newLeft = mouseX - Width - WindowsUtils.DpiAwareXOffset * 2;
            if (newLeft < 0) newLeft = 0;
        }
        else
        {
            // no flip
            newLeft = mouseX + WindowsUtils.DpiAwareXOffset;
        }

        if (needsFlipY)
        {
            // flip Upwards while preventing -OOB
            newTop = mouseY - Height - WindowsUtils.DpiAwareYOffset * 2;
            if (newTop < 0) newTop = 0;
        }
        else
        {
            // no flip
            newTop = mouseY + WindowsUtils.DpiAwareYOffset;
        }

        // stick to edges if +OOB
        if (newLeft + Width > WindowsUtils.DpiAwareWorkAreaWidth)
        {
            newLeft = WindowsUtils.DpiAwareWorkAreaWidth - Width;
        }

        if (newTop + Height > WindowsUtils.DpiAwareWorkAreaHeight)
        {
            newTop = WindowsUtils.DpiAwareWorkAreaHeight - Height;
        }

        if (mouseX >= newLeft && mouseX <= newLeft + Width && mouseY >= newTop && mouseY <= newTop + Height)
        {
            Vm.UnavoidableMouseEnter = true;
        }

        Left = newLeft;
        Top = newTop;
    }

    public void UpdatePosition(double x, double y)
    {
        Left = x;
        Top = y;
    }

    private void DisplayResults(bool generateAllResults, string? text = null)
    {
        // TODO: Should be configurable
        PopupListBox.Items.Filter = NoAllDictFilter;

        if (text != null && !generateAllResults && StackPanelCache.TryGet(text, out var data))
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (i > ConfigManager.MaxNumResultsNotInMiningMode)
                {
                    break;
                }

                ResultStackPanels.Add(data[i]);
            }

            GenerateDictTypeButtons();
            return;
        }

        int resultCount = Vm.LastLookupResults.Count;
        for (int index = 0; index < resultCount; index++)
        {
            if (!generateAllResults && index > ConfigManager.MaxNumResultsNotInMiningMode)
            {
                break;
            }

            ResultStackPanels.Add(MakeResultStackPanel(Vm.LastLookupResults[index], index, resultCount));
        }

        GenerateDictTypeButtons();
        UpdateLayout();

        // we might cache incomplete results if we don't wait until all dicts are loaded
        if (text != null && Storage.DictsReady && !Storage.UpdatingJMdict && !Storage.UpdatingJMnedict && !Storage.UpdatingKanjidic)
        {
            StackPanelCache.AddReplace(text, ResultStackPanels.ToArray());
        }
    }

    public OneResult MakeResultStackPanel(LookupResult result,
        int index, int resultsCount)
    {
        var oneResult = new OneResult(result, this, index);
        PopupListBox.Items.Filter = NoAllDictFilter;
        return oneResult;

        var innerStackPanel = new StackPanel { Margin = new Thickness(4, 2, 4, 2), Tag = result.Dict };
        WrapPanel top = new();
        StackPanel bottom = new();

        innerStackPanel.Children.Add(top);
        innerStackPanel.Children.Add(bottom);

        UIElement?[] babies =
        {

        };

        for (int i = 0; i < babies.Length; i++)
        {
            UIElement? baby = babies[i];

            // TODO(rampaa): Adapt this
            if (baby is TextBlock textBlock)
            {
                if ((textBlock.Name is "PrimarySpelling" or "Readings"))
                {
                    Dict? pitchDict = Storage.Dicts.Values.FirstOrDefault(dict => dict.Type == DictType.PitchAccentYomichan);
                    if (pitchDict?.Active ?? false)
                    {
                        List<string>? readings = result.Readings;

                        if (textBlock.Name is "PrimarySpelling" && (readings?.Any() ?? false))
                        {
                            top.Children.Add(textBlock);
                        }
                        else
                        {
                            Grid pitchAccentGrid = CreatePitchAccentGrid(result.PrimarySpelling,
                                result.AlternativeSpellings ?? new(),
                                readings ?? new(),
                                textBlock.Text.Split(", ").ToList(),
                                textBlock.Margin.Left,
                                pitchDict);

                            if (pitchAccentGrid.Children.Count == 0)
                            {
                                top.Children.Add(textBlock);
                            }
                            else
                            {
                                pitchAccentGrid.Children.Add(textBlock);
                                top.Children.Add(pitchAccentGrid);
                            }
                        }
                    }

                    else
                        top.Children.Add(textBlock);
                }
                else
                    top.Children.Add(textBlock);
            }
            else if (baby is TextBox textBox)
            {
                if ((textBox.Name is "PrimarySpelling" or "Readings"))
                {
                    Dict? pitchDict = Storage.Dicts.Values.FirstOrDefault(dict => dict.Type == DictType.PitchAccentYomichan);
                    if (pitchDict?.Active ?? false)
                    {
                        List<string>? readings = result.Readings;

                        if (textBox.Name is "PrimarySpelling" && (readings?.Any() ?? false))
                        {
                            top.Children.Add(textBox);
                        }
                        else
                        {
                            Grid pitchAccentGrid = CreatePitchAccentGrid(result.PrimarySpelling,
                                result.AlternativeSpellings ?? new(),
                                readings ?? new(),
                                textBox.Text.Split(", ").ToList(),
                                textBox.Margin.Left,
                                pitchDict);

                            if (pitchAccentGrid.Children.Count == 0)
                            {
                                top.Children.Add(textBox);
                            }
                            else
                            {
                                pitchAccentGrid.Children.Add(textBox);
                                top.Children.Add(pitchAccentGrid);
                            }
                        }
                    }
                    else
                        top.Children.Add(textBox);
                }
                else
                    top.Children.Add(textBox);
            }
        }
    }

    [Obsolete("Unadapted")]
    private static Grid CreatePitchAccentGrid(string primarySpelling, List<string> alternativeSpellings,
        List<string> readings, List<string> splitReadingsWithRInfo, double leftMargin, Dict dict)
    {
        Grid pitchAccentGrid = new();

        bool hasReading = readings.Any();

        int fontSize = hasReading
            ? ConfigManager.ReadingsFontSize
            : ConfigManager.PrimarySpellingFontSize;

        List<string> expressions = hasReading ? readings : new List<string> { primarySpelling };

        double horizontalOffsetForReading = leftMargin;

        for (int i = 0; i < expressions.Count; i++)
        {
            string normalizedExpression = Kana.KatakanaToHiraganaConverter(expressions[i]);
            List<string> combinedFormList = Kana.CreateCombinedForm(expressions[i]);

            if (i > 0)
            {
                horizontalOffsetForReading +=
                    WindowsUtils.MeasureTextSize(splitReadingsWithRInfo[i - 1] + ", ", fontSize).Width;
            }

            if (dict.Contents.TryGetValue(normalizedExpression, out List<IResult>? pitchAccentDictResultList))
            {
                PitchResult? chosenPitchAccentDictResult = null;

                for (int j = 0; j < pitchAccentDictResultList.Count; j++)
                {
                    var pitchAccentDictResult = (PitchResult)pitchAccentDictResultList[j];

                    if (!hasReading || (pitchAccentDictResult.Reading != null &&
                                        normalizedExpression ==
                                        Kana.KatakanaToHiraganaConverter(pitchAccentDictResult.Reading)))
                    {
                        if (primarySpelling == pitchAccentDictResult.Spelling)
                        {
                            chosenPitchAccentDictResult = pitchAccentDictResult;
                            break;
                        }

                        else if (alternativeSpellings?.Contains(pitchAccentDictResult.Spelling) ?? false)
                        {
                            chosenPitchAccentDictResult ??= pitchAccentDictResult;
                        }
                    }
                }

                if (chosenPitchAccentDictResult != null)
                {
                    Polyline polyline = new()
                    {
                        StrokeThickness = 2,
                        Stroke = (SolidColorBrush)new BrushConverter()
                            .ConvertFrom(dict.Options?.PitchAccentMarkerColor?.Value
                            ?? Colors.DeepSkyBlue.ToString())!,
                        StrokeDashArray = new DoubleCollection { 1, 1 }
                    };

                    bool lowPitch = false;
                    double horizontalOffsetForChar = horizontalOffsetForReading;
                    for (int j = 0; j < combinedFormList.Count; j++)
                    {
                        Size charSize = WindowsUtils.MeasureTextSize(combinedFormList[j], fontSize);

                        if (chosenPitchAccentDictResult.Position - 1 == j)
                        {
                            polyline.Points!.Add(new Point(horizontalOffsetForChar, 0));
                            polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width, 0));
                            polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width,
                                charSize.Height));

                            lowPitch = true;
                        }

                        else if (j == 0)
                        {
                            polyline.Points!.Add(new Point(horizontalOffsetForChar, charSize.Height));
                            polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width,
                                charSize.Height));
                            polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width, 0));
                        }

                        else
                        {
                            double charHeight = lowPitch ? charSize.Height : 0;
                            polyline.Points!.Add(new Point(horizontalOffsetForChar, charHeight));
                            polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width,
                                charHeight));
                        }

                        horizontalOffsetForChar += charSize.Width;
                    }

                    pitchAccentGrid.Children.Add(polyline);
                }
            }
        }

        pitchAccentGrid.VerticalAlignment = VerticalAlignment.Center;
        pitchAccentGrid.HorizontalAlignment = HorizontalAlignment.Left;

        return pitchAccentGrid;
    }

    public void Definitions_MouseMove(TextBox tb)
    {
        if (Storage.JapaneseRegex.IsMatch(tb.Text))
        {
            TextBox_MouseMove(tb);
        }

        else if (ConfigManager.HighlightLongestMatch)
        {
            WindowsUtils.Unselect(LastTextBox);
        }
    }

    private void PopupListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;

        MouseWheelEventArgs e2 = new(e.MouseDevice!, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
            Source = e.Source
        };
        PopupListBox!.RaiseEvent(e2);
    }

    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        //int keyVal = (int)e.Key;
        //int numericKeyValue = -1;
        //if ((keyVal >= (int)Key.D1 && keyVal <= (int)Key.D9))
        //{
        //    numericKeyValue = (int)e.Key - (int)Key.D0 - 1;
        //}
        //else if (keyVal >= (int)Key.NumPad1 && keyVal <= (int)Key.NumPad9)
        //{
        //    numericKeyValue = (int)e.Key - (int)Key.NumPad0 - 1;
        //}

        e.Handled = true;

        if (WindowsUtils.KeyGestureComparer(e, ConfigManager.MiningModeKeyGesture))
        {
            if (Vm.MiningMode)
                return;

            EnableMiningMode();

            Activate();
            Focus();

            ResultStackPanels.Clear();
            DisplayResults(true);
        }
        else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.PlayAudioKeyGesture))
        {
            await PlayAudio();
        }
        else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.ClosePopupKeyGesture))
        {
            Vm.MiningMode = false;
            TextBlockMiningModeReminder!.Visibility = Visibility.Collapsed;
            ItemsControlButtons.Visibility = Visibility.Collapsed;

            PopUpScrollViewer!.ScrollToTop();

            if (ConfigManager.LookupOnSelectOnly)
            {
                WindowsUtils.Unselect(LastTextBox);
            }

            if (ParentPopupWindow != null)
            {
                ParentPopupWindow.Focus();
            }

            else
            {
                MainWindow.Instance.Focus();
            }

            Hide();
        }
        else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.KanjiModeKeyGesture))
        {
            ConfigManager.Instance.KanjiMode = !ConfigManager.Instance.KanjiMode;
            Vm.LastText = "";
            Storage.Frontend.InvalidateDisplayCache();
            if (ParentPopupWindow != null)
            {
                TextBox_MouseMove(LastTextBox!);
            }

            else
            {
                MainWindow.Instance.MainTextBox_MouseMove(null, null);
            }
        }
        else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.ShowAddNameWindowKeyGesture))
        {
            if (Storage.DictsReady)
                WindowsUtils.ShowAddNameWindow(Vm.LastSelectedText);
        }
        else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.ShowAddWordWindowKeyGesture))
        {
            if (Storage.DictsReady)
                WindowsUtils.ShowAddWordWindow(Vm.LastSelectedText);
        }
        else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.SearchWithBrowserKeyGesture))
        {
            WindowsUtils.SearchWithBrowser(Vm.LastSelectedText);
        }
        else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.InactiveLookupModeKeyGesture))
        {
            ConfigManager.InactiveLookupMode = !ConfigManager.InactiveLookupMode;
        }
        else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.MotivationKeyGesture))
        {
            WindowsUtils.Motivate($"{Storage.ResourcesPath}/Motivation");
        }
        else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.ShowStatsKeyGesture))
        {
            WindowsUtils.ShowStatsWindow();
        }
        else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.NextDictKeyGesture))
        {
            bool foundSelectedButton = false;
            bool movedToNextDict = false;

            int dictCount = ItemsControlButtons.Items.Count;
            for (int i = 0; i < dictCount; i++)
            {
                var button = (Button)ItemsControlButtons.Items[i];

                if (button.Background == Brushes.DodgerBlue)
                {
                    var brush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF2D2D30")!; // "Dark"
                    brush.Freeze();
                    button.Background = brush;

                    foundSelectedButton = true;
                    continue;
                }

                if (foundSelectedButton && button.IsEnabled)
                {
                    Vm.FilteredDict = (Dict)button.Tag;
                    movedToNextDict = true;
                    button.Background = Brushes.DodgerBlue;
                    PopupListBox.Items.Filter = DictFilter;
                    break;
                }
            }

            if (!movedToNextDict)
            {
                ((Button)ItemsControlButtons.Items[0]).Background = Brushes.DodgerBlue;
                PopupListBox.Items.Filter = NoAllDictFilter;
            }
        }
        else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.PreviousDictKeyGesture))
        {
            bool foundSelectedButton = false;
            bool movedToPreviousDict = false;

            var brush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF2D2D30")!; // "Dark"
            brush.Freeze();

            int dictCount = ItemsControlButtons.Items.Count;
            for (int i = dictCount - 1; i > 0; i--)
            {
                var button = (Button)ItemsControlButtons.Items[i];

                if (button.Background == Brushes.DodgerBlue)
                {
                    button.Background = brush;

                    foundSelectedButton = true;
                    continue;
                }

                if (foundSelectedButton && button.IsEnabled)
                {
                    Vm.FilteredDict = (Dict)button.Tag;
                    button.Background = Brushes.DodgerBlue;
                    movedToPreviousDict = true;
                    PopupListBox.Items.Filter = DictFilter;
                    break;
                }
            }

            if (foundSelectedButton && !movedToPreviousDict)
            {
                ((Button)ItemsControlButtons.Items[0]).Background = Brushes.DodgerBlue;
                PopupListBox.Items.Filter = NoAllDictFilter;
            }

            else if (!foundSelectedButton)
            {
                for (int i = dictCount - 1; i > 0; i--)
                {
                    var btn = (Button)ItemsControlButtons.Items[i];
                    if (btn.IsEnabled)
                    {
                        Vm.FilteredDict = (Dict)btn.Tag;
                        btn.Background = Brushes.DodgerBlue;
                        ((Button)ItemsControlButtons.Items[0]).Background = brush;
                        PopupListBox.Items.Filter = DictFilter;
                        break;
                    }
                }
            }
        }
    }

    private void EnableMiningMode()
    {
        Vm.MiningMode = true;

        if (ConfigManager.ShowMiningModeReminder)
        {
            TextBlockMiningModeReminder!.Visibility = Visibility.Visible;
        }

        ItemsControlButtons.Visibility = Visibility.Visible;

        PopUpScrollViewer!.ScrollToTop();
    }

    private async Task PlayAudio()
    {
        //int index = numericKeyValue != -1 ? numericKeyValue : _playAudioIndex;
        //if (index > PopupListBox.Items.Count - 1)
        //{
        //    WindowsUtils.Alert(AlertLevel.Error, "Index out of range");
        //    return;
        //}

        //var innerStackPanel = (StackPanel)PopupListBox.Items[index];

        var visibleOneResults = PopupListBox.Items.Cast<OneResult>()
            .Where(oneResult => oneResult.Visibility == Visibility.Visible).ToArray();

        if (visibleOneResults.Length == 0)
            return;

        if (Vm.PlayAudioIndex > visibleOneResults.Length - 1)
        {
            Utils.Logger.Debug("PlayAudioIndex is invalid. Resetting to 0");
            Vm.PlayAudioIndex = 0;
        }

        var oneResultVm = visibleOneResults[Vm.PlayAudioIndex].Vm;
        string primarySpelling = oneResultVm.PrimarySpelling;
        string reading = string.IsNullOrEmpty(oneResultVm.Readings) ? "" : oneResultVm.Readings.Split(',')[0];

        await Utils.GetAndPlayAudioFromJpod101(primarySpelling, reading, 1).ConfigureAwait(false);
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (!ConfigManager.LookupOnSelectOnly
            && !ConfigManager.LookupOnLeftClickOnly
            && !ConfigManager.FixedPopupPositioning
            && ChildPopupWindow is { Vm.MiningMode: false })
        {
            ChildPopupWindow.Hide();
            ChildPopupWindow.Vm.LastText = "";
        }

        if (Vm.MiningMode
            || ConfigManager.FixedPopupPositioning
            || Vm.UnavoidableMouseEnter)
        {
            return;
        }

        Hide();
        Vm.LastText = "";
    }

    public void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (!ConfigManager.LookupOnSelectOnly
            && !ConfigManager.LookupOnLeftClickOnly
            && !ConfigManager.FixedPopupPositioning
            && ChildPopupWindow is { Vm.MiningMode: false, Vm.UnavoidableMouseEnter: false })
        {
            ChildPopupWindow.Hide();
            ChildPopupWindow.Vm.LastText = "";
        }

        if (Vm.MiningMode
            || ConfigManager.FixedPopupPositioning
            || IsMouseOver)
        {
            return;
        }

        Hide();
        Vm.LastText = "";

        if (ConfigManager.HighlightLongestMatch && !PopupContextMenu.IsVisible)
        {
            WindowsUtils.Unselect(LastTextBox);
        }
    }

    //private void Window_Deactivated(object sender, EventArgs e)
    //{
    //    if (!IsKeyboardFocusWithin && (!ChildPopupWindow?.IsVisible ?? true))
    //        MainWindow.Instance.FocusEllipse.Fill = Brushes.Transparent;
    //}

    //private void Window_Activated(object sender, EventArgs e)
    //{
    //    MainWindow.Instance.FocusEllipse.Fill = Brushes.Green;
    //}

    private void GenerateDictTypeButtons()
    {
        DictTypeButtons.Clear();

        var buttonAll = new Button { Content = "All", Margin = new Thickness(1), Background = Brushes.DodgerBlue };
        buttonAll.Click += ButtonAllOnClick;
        DictTypeButtons.Add(buttonAll);

        List<Dict> foundDicts = new();

        foreach (var oneResult in ResultStackPanels)
        {
            Dict foundDict = oneResult.Vm.Dict;
            foundDicts.Add(foundDict);
        }

        foreach (Dict dict in Storage.Dicts.Values.OrderBy(dict => dict.Priority).ToList())
        {
            if (!dict.Active || dict.Type == DictType.PitchAccentYomichan)
                continue;

            var button = new Button { Content = dict.Name, Margin = new Thickness(1), Tag = dict };
            button.Click += DictTypeButtonOnClick;

            if (!foundDicts.Contains(dict))
            {
                button.IsEnabled = false;
            }

            DictTypeButtons.Add(button);
        }
    }

    private void ButtonAllOnClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var brush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF2D2D30")!; // "Dark"
        brush.Freeze();
        foreach (Button btn in ItemsControlButtons.Items)
        {
            btn.Background = brush;
        }

        button.Background = Brushes.DodgerBlue;

        PopupListBox.Items.Filter = NoAllDictFilter;
    }

    private void DictTypeButtonOnClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var brush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF2D2D30")!; // "Dark"
        brush.Freeze();

        foreach (Button btn in ItemsControlButtons.Items)
        {
            btn.Background = brush;
        }

        button.Background = Brushes.DodgerBlue;

        Vm.FilteredDict = (Dict)button.Tag;

        PopupListBox.Items.Filter = DictFilter;
    }
    private bool DictFilter(object item)
    {
        var items = (OneResult)item;
        return items.Vm.Dict == Vm.FilteredDict;
    }

    private static bool NoAllDictFilter(object item)
    {
        if (Storage.Frontend.CoreConfig.KanjiMode)
        {
            return true;
        }

        var dict = ((OneResult)item).Vm.Dict;
        return !dict.Options?.NoAll?.Value ?? true;
    }
}
