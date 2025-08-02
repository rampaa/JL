using JL.Core.Lookup;
using JL.Windows.GUI;

namespace JL.Windows;
internal sealed class LookupDisplayResult(PopupWindow popupWindow, LookupResult lookupResult, int index, bool nonLastItem)
{
    public PopupWindow OwnerWindow { get; } = popupWindow;
    public LookupResult LookupResult { get; } = lookupResult;
    public int Index { get; } = index;
    public bool NonLastItem { get; } = nonLastItem;
    public bool IsDuplicate { get; set; } // = false;
}
