using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Lookup;
using JL.Core.Pitch;
using JL.Windows.GUI.ViewModel;
using JL.Windows.Utilities;

namespace JL.Windows.GUI.View;

public partial class OneResult : UserControl
{
    public OneResultViewModel Vm { get; }

    private PopupWindow PopupWindow { get; }

    public OneResult(LookupResult result, PopupWindow popupWindow, int index)
    {
        InitializeComponent();
        Vm = new OneResultViewModel(result, popupWindow.Vm, index);
        DataContext = Vm;
        PopupWindow = popupWindow;

        // if (!vm.PopupViewModel.MiningMode)
        // {
        //     bottom.Children.Add(new TextBox
        //     {
        //         FontSize = 17, Text = vm.Definitions,TextWrapping = TextWrapping.Wrap,IsReadOnly = true,IsUndoEnabled = false
        //     });
        // }

        Dict? pitchDict = Storage.Dicts.Values.FirstOrDefault(dict => dict.Type == DictType.PitchAccentYomichan);
        if (pitchDict?.Active ?? false)
        {
            if (string.IsNullOrEmpty(Vm.Readings))
            {
                List<string>? readings = result.Readings;
                var textBlock = TextBlockPrimarySpelling;

                List<Polyline> pitchAccentGrid = CreatePitchAccentGrid(result.PrimarySpelling,
                    result.AlternativeSpellings ?? new(),
                    readings ?? new(),
                    Vm.PrimarySpelling.Split(", ").ToList(),
                    textBlock.Margin.Left,
                    pitchDict);

                if (pitchAccentGrid.Any())
                {
                    foreach (Polyline polyline in pitchAccentGrid)
                    {
                        pitchAccentGridPrimarySpelling.Children.Add(polyline);
                    }
                }
            }
            else
            {
                List<string>? readings = result.Readings;
                var textBlock = TextBoxReadings;

                List<Polyline> pitchAccentGrid = CreatePitchAccentGrid(result.PrimarySpelling,
                    result.AlternativeSpellings ?? new(),
                    readings ?? new(),
                    Vm.Readings.Split(", ").ToList(),
                    textBlock.Margin.Left,
                    pitchDict);

                if (pitchAccentGrid.Any())
                {
                    foreach (Polyline polyline in pitchAccentGrid)
                    {
                        pitchAccentGridReadings.Children.Add(polyline);
                    }
                }
            }
        }
    }

    private static List<Polyline> CreatePitchAccentGrid(string primarySpelling, List<string> alternativeSpellings,
        List<string> readings, List<string> splitReadingsWithRInfo, double leftMargin, Dict dict)
    {
        List<Polyline> pitchAccentGrid = new();

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

                    pitchAccentGrid.Add(polyline);
                }
            }
        }

        return pitchAccentGrid;
    }

    private void Unselect(object sender, RoutedEventArgs e)
    {
        WindowsUtils.Unselect((TextBox)sender);
    }

    private void TextBoxPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        PopupWindow.AddNameButton!.IsEnabled = Storage.DictsReady;
        PopupWindow.AddWordButton!.IsEnabled = Storage.DictsReady;

        PopupWindow.Vm.LastSelectedText = ((TextBox)sender).SelectedText;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        PopupWindow.OnMouseLeave(sender, e);
    }

    private async void PrimarySpelling_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle)
        {
            WindowsUtils.CopyTextToClipboard(((TextBlock)sender).Text);
            return;
        }

        if (!Vm.PopupVm.MiningMode || e.ChangedButton == MouseButton.Right)
        {
            return;
        }

        PopupWindow.TextBlockMiningModeReminder!.Visibility = Visibility.Collapsed;
        PopupWindow.ItemsControlButtons.Visibility = Visibility.Collapsed;
        PopupWindow.Hide(); // todo

        await Vm.VM_PrimarySpelling_PreviewMouseUp();
    }

    private void UiElement_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if ((!ConfigManager.LookupOnSelectOnly && !ConfigManager.LookupOnLeftClickOnly)
            || Background!.Opacity == 0
            || ConfigManager.InactiveLookupMode
            || (ConfigManager.RequireLookupKeyPress &&
                !WindowsUtils.KeyGestureComparer(ConfigManager.LookupKeyKeyGesture))
            || (ConfigManager.FixedPopupPositioning && PopupWindow.ParentPopupWindow != null))
        {
            return;
        }

        //if (ConfigManager.RequireLookupKeyPress
        //    && !Keyboard.Modifiers.HasFlag(ConfigManager.LookupKey))
        //    return;

        PopupWindow.ChildPopupWindow ??= new PopupWindow(PopupWindow);

        if (ConfigManager.LookupOnSelectOnly)
        {
            PopupWindow.ChildPopupWindow.LookupOnSelect((TextBox)sender);
        }
        else
        {
            PopupWindow.ChildPopupWindow.TextBox_MouseMove((TextBox)sender);
        }

        if (ConfigManager.FixedPopupPositioning)
        {
            PopupWindow.ChildPopupWindow.UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition,
                WindowsUtils.DpiAwareFixedPopupYPosition);
        }
        else
        {
            PopupWindow.ChildPopupWindow.UpdatePosition(PointToScreen(Mouse.GetPosition(this)));
        }
    }

    private void PopupMouseMove(object sender, MouseEventArgs e)
    {
        if (ConfigManager.LookupOnSelectOnly
            || ConfigManager.LookupOnLeftClickOnly
            || (ConfigManager.RequireLookupKeyPress
                && !WindowsUtils.KeyGestureComparer(ConfigManager.LookupKeyKeyGesture)))
        {
            return;
        }

        PopupWindow.ChildPopupWindow ??= new PopupWindow(PopupWindow);

        if (PopupWindow.ChildPopupWindow.Vm.MiningMode)
            return;

        // prevents stray PopupWindows being created when you move your mouse too fast
        if (PopupWindow.Vm.MiningMode)
        {
            PopupWindow.ChildPopupWindow.Definitions_MouseMove((TextBox)sender);

            if (!PopupWindow.ChildPopupWindow.Vm.MiningMode)
            {
                if (ConfigManager.FixedPopupPositioning)
                {
                    PopupWindow.ChildPopupWindow.UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition,
                        WindowsUtils.DpiAwareFixedPopupYPosition);
                }
                else
                {
                    PopupWindow.ChildPopupWindow.UpdatePosition(PointToScreen(Mouse.GetPosition(this)));
                }
            }
        }
    }
}
