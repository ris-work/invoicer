using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
        public static void DisableHoverBackgroundChange(
            this Eto.Forms.Button button,
            Eto.Drawing.Color normalColor
        )
        {
            /*System.Console.WriteLine(
                $"Running on {Eto.Platform.Get(Eto.Platforms.Wpf).ToString()}"
            );*/
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
                    (byte)normalColor.B
                );
                var normalBrush = new System.Windows.Media.SolidColorBrush(wpfColor);

                // Set the initial background.
                wpfButton.Background = normalBrush;

                // Force WPF to use our custom template by overriding the default style.
                wpfButton.OverridesDefaultStyle = true;

                // Create a new ControlTemplate that does nothing fancy.
                var template = new System.Windows.Controls.ControlTemplate(
                    typeof(System.Windows.Controls.Button)
                );

                // Create a Border element that binds its Background (and border properties) to the Button.
                var borderFactory = new System.Windows.FrameworkElementFactory(
                    typeof(System.Windows.Controls.Border)
                );
                borderFactory.SetBinding(
                    System.Windows.Controls.Border.BackgroundProperty,
                    new System.Windows.Data.Binding("Background")
                    {
                        RelativeSource = new System.Windows.Data.RelativeSource(
                            System.Windows.Data.RelativeSourceMode.TemplatedParent
                        ),
                    }
                );
                borderFactory.SetBinding(
                    System.Windows.Controls.Border.BorderBrushProperty,
                    new System.Windows.Data.Binding("BorderBrush")
                    {
                        RelativeSource = new System.Windows.Data.RelativeSource(
                            System.Windows.Data.RelativeSourceMode.TemplatedParent
                        ),
                    }
                );
                borderFactory.SetBinding(
                    System.Windows.Controls.Border.BorderThicknessProperty,
                    new System.Windows.Data.Binding("BorderThickness")
                    {
                        RelativeSource = new System.Windows.Data.RelativeSource(
                            System.Windows.Data.RelativeSourceMode.TemplatedParent
                        ),
                    }
                );

                // Create a ContentPresenter to display the button's content.
                var contentPresenterFactory = new System.Windows.FrameworkElementFactory(
                    typeof(System.Windows.Controls.ContentPresenter)
                );
                contentPresenterFactory.SetValue(
                    System.Windows.Controls.ContentPresenter.HorizontalAlignmentProperty,
                    System.Windows.HorizontalAlignment.Center
                );
                contentPresenterFactory.SetValue(
                    System.Windows.Controls.ContentPresenter.VerticalAlignmentProperty,
                    System.Windows.VerticalAlignment.Center
                );
                contentPresenterFactory.SetBinding(
                    System.Windows.Controls.ContentPresenter.ContentProperty,
                    new System.Windows.Data.Binding("Content")
                    {
                        RelativeSource = new System.Windows.Data.RelativeSource(
                            System.Windows.Data.RelativeSourceMode.TemplatedParent
                        ),
                    }
                );
                contentPresenterFactory.SetBinding(
                    System.Windows.Controls.ContentPresenter.ContentTemplateProperty,
                    new System.Windows.Data.Binding("ContentTemplate")
                    {
                        RelativeSource = new System.Windows.Data.RelativeSource(
                            System.Windows.Data.RelativeSourceMode.TemplatedParent
                        ),
                    }
                );

                // Nest the ContentPresenter inside the Border.
                borderFactory.AppendChild(contentPresenterFactory);
                template.VisualTree = borderFactory;

                // Apply the new ControlTemplate so that no VisualState (e.g., hover) changes the background.
                wpfButton.Template = template;
            }
            else
            {
                System.Console.WriteLine(
                    $"{currentPlatform} != {Eto.Platform.Get(Eto.Platforms.Wpf).ToString()}"
                );
            }
#endif
        }

        public static void ApplyDarkThemeForScrollBarsAndGridView(this Form form)
        {
#if WINDOWS
            if (Eto.Platform.Instance.ToString() == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                var resources = System.Windows.Application.Current.Resources;

                /*// Global ScrollBar style (for the track)
                var scrollBarStyle = new System.Windows.Style(typeof(System.Windows.Controls.Primitives.ScrollBar));
                scrollBarStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.ScrollBar.BackgroundProperty, System.Windows.Media.Brushes.Black));
                scrollBarStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.ScrollBar.ForegroundProperty, System.Windows.Media.Brushes.White));
                resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = scrollBarStyle;

                // RepeatButton style for the arrow (up/down/left/right) buttons in scrollbars.
                var repeatButtonStyle = new System.Windows.Style(typeof(System.Windows.Controls.Primitives.RepeatButton));
                repeatButtonStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.RepeatButton.BackgroundProperty, System.Windows.Media.Brushes.Black));
                repeatButtonStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.RepeatButton.ForegroundProperty, System.Windows.Media.Brushes.White));
                resources[typeof(System.Windows.Controls.Primitives.RepeatButton)] = repeatButtonStyle;

                // GridViewColumnHeader style.
                var gridViewHeaderStyle = new System.Windows.Style(typeof(System.Windows.Controls.GridViewColumnHeader));
                gridViewHeaderStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.GridViewColumnHeader.BackgroundProperty, System.Windows.Media.Brushes.Black));
                gridViewHeaderStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.GridViewColumnHeader.ForegroundProperty, System.Windows.Media.Brushes.White));*/

                // Define a minimal DataTemplate so header content (assumed text) binds to the parent's Foreground.
                var dataTemplate = new System.Windows.DataTemplate();
                var textBlockFactory = new System.Windows.FrameworkElementFactory(
                    typeof(System.Windows.Controls.TextBlock)
                );
                textBlockFactory.SetBinding(
                    System.Windows.Controls.TextBlock.TextProperty,
                    new System.Windows.Data.Binding(".")
                );
                textBlockFactory.SetBinding(
                    System.Windows.Controls.TextBlock.ForegroundProperty,
                    new System.Windows.Data.Binding("Foreground")
                    {
                        RelativeSource = new System.Windows.Data.RelativeSource(
                            System.Windows.Data.RelativeSourceMode.FindAncestor,
                            typeof(System.Windows.Controls.GridViewColumnHeader),
                            1
                        ),
                    }
                );
                dataTemplate.VisualTree = textBlockFactory;
                //gridViewHeaderStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.GridViewColumnHeader.ContentTemplateProperty, dataTemplate));
                //resources[typeof(System.Windows.Controls.GridViewColumnHeader)] = gridViewHeaderStyle;
            }
#endif
        }

        public static void ApplyDarkTheme(this Form form)
        {
#if WINDOWS
            if (Eto.Platform.Instance.ToString() == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                var window = (System.Windows.Window)Eto.Forms.WpfHelpers.ToNative(form, false);
                var themeFiles = new (string File, string Uri)[]
                {
                    (
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SoftDark.xaml"),
                        "pack://siteoforigin:,,,/SoftDark.xaml"
                    ),
                    (
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ControlColours.xaml"),
                        "pack://siteoforigin:,,,/ControlColours.xaml"
                    ),
                    (
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Controls.xaml"),
                        "pack://siteoforigin:,,,/Controls.xaml"
                    ),
                };
                foreach (var theme in themeFiles)
                {
                    if (File.Exists(theme.File))
                    {
                        window.Resources.MergedDictionaries.Add(
                            new System.Windows.ResourceDictionary
                            {
                                Source = new Uri(theme.Uri, UriKind.Absolute),
                            }
                        );
                    }
                }
            }
#endif
        }

        public static void ApplyDarkGridHeaders(this Eto.Forms.GridView gridView)
        {
#if WINDOWS
            if (Eto.Platform.Instance.ToString() == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                var native = Eto.Forms.WpfHelpers.ToNative(gridView, false);

                // Case 1: Eto.GridView wraps a WPF DataGrid.
                if (native is System.Windows.Controls.DataGrid dataGrid)
                {
                    var headerStyle = new System.Windows.Style(
                        typeof(System.Windows.Controls.Primitives.DataGridColumnHeader)
                    );
                    headerStyle.Setters.Add(
                        new System.Windows.Setter(
                            System
                                .Windows
                                .Controls
                                .Primitives
                                .DataGridColumnHeader
                                .BackgroundProperty,
                            System.Windows.Media.Brushes.Black
                        )
                    );
                    headerStyle.Setters.Add(
                        new System.Windows.Setter(
                            System
                                .Windows
                                .Controls
                                .Primitives
                                .DataGridColumnHeader
                                .ForegroundProperty,
                            System.Windows.Media.Brushes.White
                        )
                    );
                    dataGrid.ColumnHeaderStyle = headerStyle;
                }
                // Case 2: Eto.GridView is realized as a ListView with a GridView view.
                else if (
                    native is System.Windows.Controls.ListView listView
                    && listView.View is System.Windows.Controls.GridView listGridView
                )
                {
                    var headerStyle = new System.Windows.Style(
                        typeof(System.Windows.Controls.GridViewColumnHeader)
                    );
                    headerStyle.Setters.Add(
                        new System.Windows.Setter(
                            System.Windows.Controls.GridViewColumnHeader.BackgroundProperty,
                            System.Windows.Media.Brushes.Black
                        )
                    );
                    headerStyle.Setters.Add(
                        new System.Windows.Setter(
                            System.Windows.Controls.GridViewColumnHeader.ForegroundProperty,
                            System.Windows.Media.Brushes.White
                        )
                    );
                    listGridView.ColumnHeaderContainerStyle = headerStyle;
                }
            }
#endif
        }
    }
}
