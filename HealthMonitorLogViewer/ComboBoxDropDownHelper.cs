/*
 * Copyright (c) Rishikeshan Sulochana/Lavakumar 2024
 * As a special exception, this file is (at your option)
 * also under the MIT license, regardless of the other
 * files' license.
 *
 * Why is this code here?
 * This is an extension helper for making a ComboBox Drop
 * Down.
 *
 * Tested and is working on WinForms/WPF. Should work on
 * GTK# too. Please load the appropriate Eto.Platform
 * assemblies referenced in this file before compiling,
 * even though your program might not care about them.
 * *
 * */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Eto.Forms;
#if WINDOWS

#endif

namespace Eto.Forms
{
    public static partial class ComboBoxExtensions
    {
        public static void LaunchDropDown(this ComboBox C)
        {
            //Eto.Forms.MessageBox.Show($"{Eto.Platform.Detect}, {Eto.Platform.Get(Eto.Platforms.WinForms)}");
            var P = Eto.Platform.Instance.ToString();
            if (P == Eto.Platform.Get(Eto.Platforms.WinForms).ToString())
            {
#if WINDOWS
                System.Windows.Forms.ComboBox N = (System.Windows.Forms.ComboBox)
                    Eto.Forms.WinFormsHelpers.ToNative(C);
#endif
                N.DroppedDown = true;
            }
            else if (P == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
#if WINDOWS
                System.Windows.Controls.ComboBox N = (System.Windows.Controls.ComboBox)
                    Eto.Forms.WpfHelpers.ToNative(C);
                N.IsDropDownOpen = true;
#endif
            }
            else if (P == Eto.Platform.Get(Eto.Platforms.Gtk).ToString())
            {
                Gtk.ComboBox N = (Gtk.ComboBox)Eto.Forms.Gtk3Helpers.ToNative(C);
                N.Popdown();
            }
            else if (P == Eto.Platform.Get(Eto.Platforms.Mac64).ToString())
            {
                //Implement mac
            }
        }

        public static void UnlaunchDropDown(this ComboBox C)
        {
            //Eto.Forms.MessageBox.Show($"{Eto.Platform.Detect}, {Eto.Platform.Get(Eto.Platforms.WinForms)}");
            var P = Eto.Platform.Instance.ToString();
            if (P == Eto.Platform.Get(Eto.Platforms.WinForms).ToString())
            {
#if WINDOWS
                System.Windows.Forms.ComboBox N = (System.Windows.Forms.ComboBox)
                    Eto.Forms.WinFormsHelpers.ToNative(C);
                N.DroppedDown = false;
#endif
            }
            else if (P == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
#if WINDOWS
                System.Windows.Controls.ComboBox N = (System.Windows.Controls.ComboBox)
                    Eto.Forms.WpfHelpers.ToNative(C);
                N.IsDropDownOpen = false;
#endif
            }
            else if (P == Eto.Platform.Get(Eto.Platforms.Gtk).ToString())
            {
                Gtk.ComboBox N = (Gtk.ComboBox)Eto.Forms.Gtk3Helpers.ToNative(C);
                N.Popdown();
            }
            else if (P == Eto.Platform.Get(Eto.Platforms.Mac64).ToString())
            {
                //Implement mac
            }
        }
    }
}
