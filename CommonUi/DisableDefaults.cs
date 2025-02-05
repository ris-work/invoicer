using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Eto.Forms;

namespace EtoFE
{
    public static class DisableDefaults
    {
        public delegate void HandlerWithoutDefaults();

        public static void DisableGridViewEnterKey(this Eto.Forms.GridView GW, System.Action H)
        {
            var P = Eto.Platform.Instance.ToString();

            if (P == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                System.Windows.Controls.DataGrid WpfGW = (System.Windows.Controls.DataGrid)
                    (System.Windows.FrameworkElement)(Eto.Forms.WpfHelpers.ToNative(GW, false));
                WpfGW.BorderBrush = System.Windows.Media.Brushes.Transparent;
                WpfGW.PreviewKeyDown += (e, a) =>
                {
                    if (a.Key == System.Windows.Input.Key.Enter)
                    {
                        H();
                        //RoutedEventArgs REA = new RoutedEventArgs(routedEvent: KeyEvent);
                        a.Handled = true;
                        return;
                    }
                    else if (a.Key == System.Windows.Input.Key.Down)
                    {
                        if (WpfGW.SelectedIndex < WpfGW.Items.Count)
                            WpfGW.SelectedIndex++;
                    }
                    else if (a.Key == System.Windows.Input.Key.Up)
                    {
                        if (WpfGW.SelectedIndex > 0)
                            WpfGW.SelectedIndex--;
                    }
                };
                WpfGW.BorderThickness = new System.Windows.Thickness(0);
                WpfGW.GridLinesVisibility = DataGridGridLinesVisibility.None;
            }
        }

        public static void DisableTextBoxDownArrow(this Eto.Forms.TextBox GW, System.Action H)
        {
            var P = Eto.Platform.Instance.ToString();

            if (P == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                System.Windows.Controls.TextBox WpfTB = (System.Windows.Controls.TextBox)
                    (System.Windows.FrameworkElement)(Eto.Forms.WpfHelpers.ToNative(GW, false));
                WpfTB.PreviewKeyDown += (e, a) =>
                {
                    if (a.Key == System.Windows.Input.Key.Down)
                    {
                        H();
                        //RoutedEventArgs REA = new RoutedEventArgs(routedEvent: KeyEvent);
                        a.Handled = true;
                        return;
                    }
                };
            }
        }
    }
}
