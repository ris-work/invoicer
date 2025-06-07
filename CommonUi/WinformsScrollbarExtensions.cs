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
        //if (!ctl.AutoScroll) ctl.AutoScroll = true;
        if (ctl.AutoScroll) ctl.AutoScroll = false;
        ctl.VerticalScroll.Visible = false;
        ctl.HorizontalScroll.Visible = false;
        

        //HScrollProp?.SetValue(ctl, false, null);
        //VScrollProp?.SetValue(ctl, false, null);
        //UpdateStyles?.Invoke(ctl, null);
    }
}



namespace MyApp
{
    public class NoNativeScrollPanel : Panel
    {
        const int WS_HSCROLL = 0x0010_0000;
        const int WS_VSCROLL = 0x0020_0000;

        public NoNativeScrollPanel()
        {
            // still want WinForms to do all its AutoScroll math
            AutoScroll = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // strip out the built-in scrollbars
                cp.Style &= ~WS_HSCROLL;
                cp.Style &= ~WS_VSCROLL;
                return cp;
            }
        }
    }
}
