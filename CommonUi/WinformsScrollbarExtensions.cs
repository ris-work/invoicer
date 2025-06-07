using System;
using System.Reflection;
using System.Windows.Forms;

public static class ScrollableControlExtensions
{
    static readonly PropertyInfo HScrollProp =
        typeof(ScrollableControl).GetProperty("HScroll",
            BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly PropertyInfo VScrollProp =
        typeof(ScrollableControl).GetProperty("VScroll",
            BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly MethodInfo UpdateStyles =
        typeof(Control).GetMethod("UpdateStyles",
            BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>
    /// Hides the WinForms-native scrollbars, but leaves AutoScroll logic intact.
    /// </summary>
    public static void HideNativeScrollBars(this ScrollableControl ctl)
    {
        if (!ctl.AutoScroll) ctl.AutoScroll = true;

        HScrollProp?.SetValue(ctl, false, null);
        VScrollProp?.SetValue(ctl, false, null);
        UpdateStyles?.Invoke(ctl, null);
    }
}
