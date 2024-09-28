using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using System.Windows.Forms;
using System.Windows;

namespace Eto.Forms
{
    public static partial class ComboBoxExtensions
    {
        public static void LaunchDropDown(this ComboBox C) {
            //Eto.Forms.MessageBox.Show($"{Eto.Platform.Detect}, {Eto.Platform.Get(Eto.Platforms.WinForms)}");
            var P = Eto.Platform.Instance.ToString();
            if (P == Eto.Platform.Get(Eto.Platforms.WinForms).ToString()) {
                System.Windows.Forms.ComboBox N = (System.Windows.Forms.ComboBox)Eto.Forms.WinFormsHelpers.ToNative(C);
                N.DroppedDown = true;
            }
            else if (P == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                System.Windows.Controls.ComboBox N = (System.Windows.Controls.ComboBox)Eto.Forms.WpfHelpers.ToNative(C);
                N.IsDropDownOpen = true;
            }
            else if (P == Eto.Platform.Get(Eto.Platforms.Gtk).ToString())
            {
                Gtk.ComboBox N = (Gtk.ComboBox)Eto.Forms.Gtk3Helpers.ToNative(C);
                N.Popup();
            }
        }
    }
}
