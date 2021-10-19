using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace JapaneseLookup.Utilities
{
    public static class Utils
    {
        public static bool KeyGestureComparer(KeyEventArgs e, KeyGesture keyGesture)
        {
            if (keyGesture == null)
                return false;

            if (keyGesture.Modifiers.Equals(ModifierKeys.Windows))
                return keyGesture.Key == e.Key && (Keyboard.Modifiers & ModifierKeys.Windows) == 0;
            else
                return keyGesture.Matches(null, e);
        }
    }
}