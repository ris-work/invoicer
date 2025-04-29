using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Eto.Forms;
#if WINDOWS
using System.Windows.Controls;
using System.Windows.Media;
#endif


namespace CommonUi
{
    public static class DisableDefaults
    {
        public delegate void HandlerWithoutDefaults();

        public static void DisableGridViewEnterKey(this Eto.Forms.GridView GW, System.Action H)
        {
#if WINDOWS
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
#endif
        }

        public static void DisableTextBoxDownArrow(this Eto.Forms.TextBox GW, System.Action H)
        {
#if WINDOWS
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
#endif
        }

        public static void DisableLines(this Eto.Forms.GridView GW)
        {
#if WINDOWS
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
#endif
        }
        /// <summary>
        /// Forces the given background color on an Eto.Forms.Button on the WPF backend,
        /// overriding the default blue-gray hover background.
        /// </summary>
        /// <param name="button">The Eto.Forms.Button instance.</param>
        /// <param name="normalColor">The Eto.Drawing.Color that should be used even when hovered.</param>
        public static void DisableHoverBackgroundChange(this Eto.Forms.Button button, Eto.Drawing.Color normalColor)
        {
            System.Console.WriteLine($"Running on {Eto.Platform.Get(Eto.Platforms.Wpf).ToString()}");
#if WINDOWS
            // Verify that we're running on the WPF platform.
            var currentPlatform = Eto.Platform.Instance.ToString();
            if (currentPlatform == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                System.Console.WriteLine("WPF detected, attempt resetting button hover");
                // Retrieve the underlying native WPF Button.
                System.Windows.Controls.Button wpfButton = (System.Windows.Controls.Button)
                    (System.Windows.FrameworkElement)(Eto.Forms.WpfHelpers.ToNative(button, false));

                // Convert Eto.Drawing.Color to System.Windows.Media.Color.
                var wpfColor = System.Windows.Media.Color.FromArgb(
                    (byte)normalColor.A,
                    (byte)normalColor.R,
                    (byte)normalColor.G,
                    (byte)normalColor.B);
                var normalBrush = new System.Windows.Media.SolidColorBrush(wpfColor);

                // Set the initial background.
                wpfButton.Background = normalBrush;

                // Force WPF to use our custom template by overriding the default style.
                wpfButton.OverridesDefaultStyle = true;

                // Create a new ControlTemplate that does nothing fancy.
                var template = new System.Windows.Controls.ControlTemplate(typeof(System.Windows.Controls.Button));

                // Create a Border element that binds its Background (and border properties) to the Button.
                var borderFactory = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Border));
                borderFactory.SetBinding(System.Windows.Controls.Border.BackgroundProperty,
                    new System.Windows.Data.Binding("Background")
                    {
                        RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
                    });
                borderFactory.SetBinding(System.Windows.Controls.Border.BorderBrushProperty,
                    new System.Windows.Data.Binding("BorderBrush")
                    {
                        RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
                    });
                borderFactory.SetBinding(System.Windows.Controls.Border.BorderThicknessProperty,
                    new System.Windows.Data.Binding("BorderThickness")
                    {
                        RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
                    });

                // Create a ContentPresenter to display the button's content.
                var contentPresenterFactory = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.ContentPresenter));
                contentPresenterFactory.SetValue(System.Windows.Controls.ContentPresenter.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
                contentPresenterFactory.SetValue(System.Windows.Controls.ContentPresenter.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
                contentPresenterFactory.SetBinding(System.Windows.Controls.ContentPresenter.ContentProperty,
                    new System.Windows.Data.Binding("Content")
                    {
                        RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
                    });
                contentPresenterFactory.SetBinding(System.Windows.Controls.ContentPresenter.ContentTemplateProperty,
                    new System.Windows.Data.Binding("ContentTemplate")
                    {
                        RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
                    });

                // Nest the ContentPresenter inside the Border.
                borderFactory.AppendChild(contentPresenterFactory);
                template.VisualTree = borderFactory;

                // Apply the new ControlTemplate so that no VisualState (e.g., hover) changes the background.
                wpfButton.Template = template;
            }
            else {
                System.Console.WriteLine($"{currentPlatform} != {Eto.Platform.Get(Eto.Platforms.Wpf).ToString()}");
            }
#endif
        }
        public static void ApplyDarkThemeForScrollBarsAndGridView(this Form form)
        {
#if WINDOWS
            if (Eto.Platform.Instance.ToString() == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                var resources = System.Windows.Application.Current.Resources;
                // Style for all ScrollBars.
                resources.Add(
                    typeof(System.Windows.Controls.Primitives.ScrollBar),
                    new System.Windows.Style(typeof(System.Windows.Controls.Primitives.ScrollBar))
                    {
                        Setters =
                        {
                        new System.Windows.Setter(System.Windows.Controls.Primitives.ScrollBar.BackgroundProperty, System.Windows.Media.Brushes.Black),
                        new System.Windows.Setter(System.Windows.Controls.Primitives.ScrollBar.ForegroundProperty, System.Windows.Media.Brushes.White)
                        }
                    }
                );
                // Style for GridView column headers.
                resources.Add(
                    typeof(System.Windows.Controls.GridViewColumnHeader),
                    new System.Windows.Style(typeof(System.Windows.Controls.GridViewColumnHeader))
                    {
                        Setters =
                        {
                        new System.Windows.Setter(System.Windows.Controls.GridViewColumnHeader.BackgroundProperty, System.Windows.Media.Brushes.Black),
                        new System.Windows.Setter(System.Windows.Controls.GridViewColumnHeader.ForegroundProperty, System.Windows.Media.Brushes.White)
                        }
                    }
                );
                // Style for RepeatButtons (the arrow buttons in scrollbars).
                resources.Add(
                    typeof(System.Windows.Controls.Primitives.RepeatButton),
                    new System.Windows.Style(typeof(System.Windows.Controls.Primitives.RepeatButton))
                    {
                        Setters =
                        {
                        new System.Windows.Setter(System.Windows.Controls.Primitives.RepeatButton.BackgroundProperty, System.Windows.Media.Brushes.Black),
                        new System.Windows.Setter(System.Windows.Controls.Primitives.RepeatButton.ForegroundProperty, System.Windows.Media.Brushes.White)
                        }
                    }
                );
            }
#endif
        }
    }
}
