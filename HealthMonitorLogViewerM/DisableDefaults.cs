﻿using System;
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
                    H();
                    //RoutedEventArgs REA = new RoutedEventArgs(routedEvent: KeyEvent);
                    a.Handled = true;
                    return;
                };
                WpfGW.BorderThickness = new System.Windows.Thickness(0);
                WpfGW.GridLinesVisibility = DataGridGridLinesVisibility.None;
            }
        }

        public static void DisableLines(this Eto.Forms.GridView GW)
        {
            var P = Eto.Platform.Instance.ToString();

            if (P == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                System.Windows.Controls.DataGrid WpfGW = (System.Windows.Controls.DataGrid)
                    (System.Windows.FrameworkElement)(Eto.Forms.WpfHelpers.ToNative(GW, false));
                WpfGW.BorderBrush = System.Windows.Media.Brushes.Transparent;
                WpfGW.HorizontalGridLinesBrush = System.Windows.Media.Brushes.Aqua;
                WpfGW.VerticalGridLinesBrush = System.Windows.Media.Brushes.Transparent;
                //WpfGW.BorderThickness = new System.Windows.Thickness(0);
                WpfGW.BorderThickness = new System.Windows.Thickness(0);

                //WpfGW.Row
                WpfGW.GridLinesVisibility = DataGridGridLinesVisibility.None;

                WpfGW.CellStyle = null;
                WpfGW.Columns.First().CellStyle = null;
                Setter setter = new Setter(
                    DataGridCell.BorderBrushProperty,
                    System.Windows.Media.Brushes.Transparent
                );
                WpfGW.Style = null;
                //WpfGW.B
                //Eto.Forms.MessageBox.Show("WPF!");
            }
        }
    }
}
