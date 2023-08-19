using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace JL.Windows.GUI.UserControls;

// https://github.com/dotnet/wpf/issues/1133
internal sealed class TouchScreenTextBox : TextBox
{
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return IsReadOnly
            ? new FrameworkElementAutomationPeer(this)
            : base.OnCreateAutomationPeer();
    }
}
