using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Lookup;
using JL.Windows.GUI.ViewModel;
using JL.Windows.Utilities;

namespace JL.Windows.GUI.View;

public partial class OneResult : UserControl
{
    public OneResultViewModel Vm { get; }

    private PopupWindow PopupWindow { get; }

    public OneResult(LookupResult lr, PopupWindow popupWindow, int index)
    {
        InitializeComponent();
        Vm = new OneResultViewModel(lr, popupWindow.Vm, index);
        DataContext = Vm;
        PopupWindow = popupWindow;

        // if (!vm.PopupViewModel.MiningMode)
        // {
        //     bottom.Children.Add(new TextBox
        //     {
        //         FontSize = 17, Text = vm.Definitions,TextWrapping = TextWrapping.Wrap,IsReadOnly = true,IsUndoEnabled = false
        //     });
        // }
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
