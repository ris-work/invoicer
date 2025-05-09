using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Eto.Forms;
using Microsoft.Maui.Controls.PlatformConfiguration;
//using System.Windows.Forms;

#if WINDOWS
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Forms;
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
                //WpfGW.Columns.First().CellStyle = null;
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

        public static void ApplyDarkThemeForScrollBarsAndGridView(this Eto.Forms.Form form)
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

        public static void ApplyDarkTheme(this Eto.Forms.Form form)
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
                            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)ColorSettings.LesserBackgroundColor.Ab, (byte)ColorSettings.LesserBackgroundColor.Rb, (byte)ColorSettings.LesserBackgroundColor.Gb, (byte)ColorSettings.LesserBackgroundColor.Bb))
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
                            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)ColorSettings.ForegroundColor.Ab, (byte)ColorSettings.ForegroundColor.Rb, (byte)ColorSettings.ForegroundColor.Gb, (byte)ColorSettings.ForegroundColor.Bb))
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

        public static void ApplyDarkThemeScrollBars(this Eto.Forms.GridView gridView)
        {
#if WINDOWS
            // Check if we're running on the WPF backend.
            var platformStr = Eto.Platform.Instance.ToString();
            if (platformStr == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                // Convert Eto GridView to its native WPF DataGrid.
                System.Windows.Controls.DataGrid wpfGrid = (System.Windows.Controls.DataGrid)
                    (System.Windows.FrameworkElement)Eto.Forms.WpfHelpers.ToNative(gridView, false);

                // Force the control to generate its template.
                wpfGrid.ApplyTemplate();
                wpfGrid.UpdateLayout();

                // Use the Dispatcher to wait until the visual tree is loaded.
                wpfGrid.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        System.Windows.Controls.ScrollViewer scrollViewer = null;
                        // Safely retrieve the template.
                        var template = wpfGrid.Template;
                        if (template != null)
                        {
                            // Attempt to find the ScrollViewer by name.
                            scrollViewer =
                                template.FindName("DG_ScrollViewer", wpfGrid)
                                as System.Windows.Controls.ScrollViewer;
                        }

                        // If found, apply dark theme styling to scrollbars.
                        if (scrollViewer != null)
                        {
                            ApplyDarkScrollBars(scrollViewer);
                        }
                        else
                        {
                            // Fallback: search the entire visual tree.
                            ApplyDarkScrollBars(wpfGrid);
                        }
                    }),
                    System.Windows.Threading.DispatcherPriority.Loaded
                );
            }
#endif
        }

#if WINDOWS
        private static void ApplyDarkScrollBars(System.Windows.DependencyObject d)
        {
            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(d, i);
                if (child is System.Windows.Controls.Primitives.ScrollBar sb)
                {
                    // Adjust these brushes to your desired dark theme.
                    sb.Background = System.Windows.Media.Brushes.DarkSlateGray;
                    sb.Foreground = System.Windows.Media.Brushes.WhiteSmoke;
                }
                // Recurse through the visual tree.
                ApplyDarkScrollBars(child);
            }
        }
#endif

        public static void ApplyDarkThemeScrollBars2(this Eto.Forms.GridView gridView)
        {
#if WINDOWS
            // This code runs only when on the WPF backend.
            var platformStr = Eto.Platform.Instance.ToString();
            if (platformStr == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                // Get the native control from the Eto handle.
                var nativeControl =
                    Eto.Forms.WpfHelpers.ToNative(gridView, false)
                    as System.Windows.FrameworkElement;
                if (nativeControl is System.Windows.Controls.DataGrid wpfGrid)
                {
                    // Create a style for the ScrollBar type.
                    var scrollBarStyle = new System.Windows.Style(
                        typeof(System.Windows.Controls.Primitives.ScrollBar)
                    );
                    scrollBarStyle.Setters.Add(
                        new System.Windows.Setter(
                            System.Windows.Controls.Control.BackgroundProperty,
                            System.Windows.Media.Brushes.DarkSlateGray
                        )
                    );
                    scrollBarStyle.Setters.Add(
                        new System.Windows.Setter(
                            System.Windows.Controls.Control.ForegroundProperty,
                            System.Windows.Media.Brushes.WhiteSmoke
                        )
                    );
                    // Optionally, adjust the border colors or other properties.
                    scrollBarStyle.Setters.Add(
                        new System.Windows.Setter(
                            System.Windows.Controls.Control.BorderBrushProperty,
                            System.Windows.Media.Brushes.Black
                        )
                    );

                    // Inject the style into the DataGrid's resources.
                    // This way, when the grid creates its scrollbars (even later), they will pick up this style.
                    wpfGrid.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] =
                        scrollBarStyle;
                }
            }
#endif
        }

        /// <summary>
        /// Forcefully applies dark styling to the arrow buttons (WPF RepeatButton) used in scrollbars.
        /// </summary>
        public static void ForceDarkThemeScrollBarArrows(this Eto.Forms.GridView gridView)
        {
#if WINDOWS
            // Ensure we're running with the WPF backend.
            if (Eto.Platform.Instance.ToString() == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                // Get the native control from the Eto handle.
                var nativeControl =
                    Eto.Forms.WpfHelpers.ToNative(gridView, false)
                    as System.Windows.FrameworkElement;
                if (nativeControl is System.Windows.Controls.DataGrid wpfGrid)
                {
                    // Run when the visual tree has been loaded.
                    wpfGrid.Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            // Look for all RepeatButton controls (i.e. arrow buttons) within the grid.
                            foreach (
                                var arrow in FindVisualChildren<System.Windows.Controls.Primitives.RepeatButton>(
                                    wpfGrid
                                )
                            )
                            {
                                // Create a new style for RepeatButton.
                                var arrowStyle = new System.Windows.Style(
                                    typeof(System.Windows.Controls.Primitives.RepeatButton)
                                );
                                arrowStyle.Setters.Add(
                                    new System.Windows.Setter(
                                        System.Windows.Controls.Control.BackgroundProperty,
                                        System.Windows.Media.Brushes.DimGray
                                    )
                                );
                                arrowStyle.Setters.Add(
                                    new System.Windows.Setter(
                                        System.Windows.Controls.Control.BorderBrushProperty,
                                        System.Windows.Media.Brushes.Black
                                    )
                                );
                                arrowStyle.Setters.Add(
                                    new System.Windows.Setter(
                                        System.Windows.Controls.Control.ForegroundProperty,
                                        System.Windows.Media.Brushes.WhiteSmoke
                                    )
                                );

                                // Directly assign the style.
                                arrow.Style = arrowStyle;
                                arrow.ApplyTemplate();
                            }
                        }),
                        DispatcherPriority.Loaded
                    );
                }
            }
#endif
        }

        /// <summary>
        /// Forcefully applies dark styling to the thumb (WPF Thumb) used in scrollbars.
        /// </summary>
        public static void ForceDarkThemeScrollBarThumbs(this Eto.Forms.GridView gridView)
        {
#if WINDOWS
            // Ensure that the backend is WPF.
            if (Eto.Platform.Instance.ToString() == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                // Get the native control from the Eto handle.
                var nativeControl =
                    Eto.Forms.WpfHelpers.ToNative(gridView, false)
                    as System.Windows.FrameworkElement;
                if (nativeControl is System.Windows.Controls.DataGrid wpfGrid)
                {
                    // Use the Dispatcher to ensure the visual tree is complete.
                    wpfGrid.Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            // Find all Thumb controls (the draggable parts of scrollbars) in the visual tree.
                            foreach (
                                var thumb in FindVisualChildren<System.Windows.Controls.Primitives.Thumb>(
                                    wpfGrid
                                )
                            )
                            {
                                // Create a new style for the Thumb.
                                var thumbStyle = new System.Windows.Style(
                                    typeof(System.Windows.Controls.Primitives.Thumb)
                                );
                                thumbStyle.Setters.Add(
                                    new System.Windows.Setter(
                                        System.Windows.Controls.Control.BackgroundProperty,
                                        System.Windows.Media.Brushes.Gray
                                    )
                                );
                                thumbStyle.Setters.Add(
                                    new System.Windows.Setter(
                                        System.Windows.Controls.Control.BorderBrushProperty,
                                        System.Windows.Media.Brushes.Black
                                    )
                                );
                                thumbStyle.Setters.Add(
                                    new System.Windows.Setter(
                                        System.Windows.Controls.Control.ForegroundProperty,
                                        System.Windows.Media.Brushes.WhiteSmoke
                                    )
                                );

                                // Directly assign the style.
                                thumb.Style = thumbStyle;
                                thumb.ApplyTemplate();
                            }
                        }),
                        DispatcherPriority.Loaded
                    );
                }
            }
#endif
        }

#if WINDOWS
        /// <summary>
        /// Recursively finds all visual children of a given type.
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject depObj)
            where T : System.Windows.DependencyObject
        {
            if (depObj != null)
            {
                int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj);
                for (int i = 0; i < count; i++)
                {
                    System.Windows.DependencyObject child =
                        System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                    if (child is T typedChild)
                    {
                        yield return typedChild;
                    }
                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
#endif

#if WINDOWS
        // These static flags ensure we only apply the styles once.
        private static bool _pageButtonStyleApplied;
        private static bool _thumbStyleApplied;

        /// <summary>
        /// Applies a global style for scrollbar page buttons (RepeatButton) based on your provided XML.
        /// This sets a ControlTemplate that displays a Border with a Transparent background.
        /// </summary>
        public static void ApplyGlobalScrollBarPageButtonStyle()
        {
            // Execute only on the WPF backend.
            if (Eto.Platform.Instance.ToString() == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                if (_pageButtonStyleApplied)
                    return; // Prevent re‑application.

                // Create a new style for System.Windows.Controls.Primitives.RepeatButton.
                var pageButtonStyle = new global::System.Windows.Style(
                    typeof(global::System.Windows.Controls.Primitives.RepeatButton)
                );
                pageButtonStyle.Setters.Add(
                    new global::System.Windows.Setter(
                        global::System.Windows.UIElement.SnapsToDevicePixelsProperty,
                        true
                    )
                );
                pageButtonStyle.Setters.Add(
                    new global::System.Windows.Setter(
                        global::System.Windows.Controls.Control.OverridesDefaultStyleProperty,
                        true
                    )
                );
                pageButtonStyle.Setters.Add(
                    new global::System.Windows.Setter(
                        global::System.Windows.Controls.Control.IsTabStopProperty,
                        false
                    )
                );
                pageButtonStyle.Setters.Add(
                    new global::System.Windows.Setter(
                        global::System.Windows.Controls.Control.FocusableProperty,
                        false
                    )
                );

                // Build a ControlTemplate equivalent to:
                //   <ControlTemplate TargetType="{x:Type RepeatButton}">
                //     <Border Background="Transparent" />
                //   </ControlTemplate>
                var template = new global::System.Windows.Controls.ControlTemplate(
                    typeof(global::System.Windows.Controls.Primitives.RepeatButton)
                );
                var borderFactory = new global::System.Windows.FrameworkElementFactory(
                    typeof(global::System.Windows.Controls.Border)
                );
                borderFactory.SetValue(
                    global::System.Windows.Controls.Border.BackgroundProperty,
                    global::System.Windows.Media.Brushes.Transparent
                );
                template.VisualTree = borderFactory;
                pageButtonStyle.Setters.Add(
                    new global::System.Windows.Setter(
                        global::System.Windows.Controls.Control.TemplateProperty,
                        template
                    )
                );

                // Insert the style into the Application's resources.
                if (global::System.Windows.Application.Current != null)
                    global::System.Windows.Application.Current.Resources[
                        typeof(global::System.Windows.Controls.Primitives.RepeatButton)
                    ] = pageButtonStyle;

                _pageButtonStyleApplied = true;
            }
        }

        /// <summary>
        /// Applies a global style for scrollbar thumbs based on your provided XML.
        /// This sets a ControlTemplate that displays a Border with CornerRadius="2", binding Background and BorderBrush via TemplateBinding, and BorderThickness="1".
        /// </summary>
        public static void ApplyGlobalScrollBarThumbStyle()
        {
            // Execute only on the WPF backend.
            if (Eto.Platform.Instance.ToString() == Eto.Platform.Get(Eto.Platforms.Wpf).ToString())
            {
                if (_thumbStyleApplied)
                    return; // Prevent re‑application.

                // Create a new style for System.Windows.Controls.Primitives.Thumb.
                var thumbStyle = new global::System.Windows.Style(
                    typeof(global::System.Windows.Controls.Primitives.Thumb)
                );
                thumbStyle.Setters.Add(
                    new global::System.Windows.Setter(
                        global::System.Windows.UIElement.SnapsToDevicePixelsProperty,
                        true
                    )
                );
                thumbStyle.Setters.Add(
                    new global::System.Windows.Setter(
                        global::System.Windows.Controls.Control.OverridesDefaultStyleProperty,
                        true
                    )
                );
                thumbStyle.Setters.Add(
                    new global::System.Windows.Setter(
                        global::System.Windows.Controls.Control.IsTabStopProperty,
                        false
                    )
                );
                thumbStyle.Setters.Add(
                    new global::System.Windows.Setter(
                        global::System.Windows.Controls.Control.FocusableProperty,
                        false
                    )
                );

                // Build a ControlTemplate equivalent to:
                //   <ControlTemplate TargetType="{x:Type Thumb}">
                //     <Border CornerRadius="2"
                //             Background="{TemplateBinding Background}"
                //             BorderBrush="{TemplateBinding BorderBrush}"
                //             BorderThickness="1" />
                //   </ControlTemplate>
                var template = new global::System.Windows.Controls.ControlTemplate(
                    typeof(global::System.Windows.Controls.Primitives.Thumb)
                );
                var borderFactory = new global::System.Windows.FrameworkElementFactory(
                    typeof(global::System.Windows.Controls.Border)
                );
                borderFactory.SetValue(
                    global::System.Windows.Controls.Border.CornerRadiusProperty,
                    new global::System.Windows.CornerRadius(2)
                );
                borderFactory.SetValue(
                    global::System.Windows.Controls.Border.BackgroundProperty,
                    new global::System.Windows.TemplateBindingExtension(
                        global::System.Windows.Controls.Control.BackgroundProperty
                    )
                );
                borderFactory.SetValue(
                    global::System.Windows.Controls.Border.BorderBrushProperty,
                    new global::System.Windows.TemplateBindingExtension(
                        global::System.Windows.Controls.Control.BorderBrushProperty
                    )
                );
                borderFactory.SetValue(
                    global::System.Windows.Controls.Border.BorderThicknessProperty,
                    new global::System.Windows.Thickness(1)
                );
                template.VisualTree = borderFactory;
                thumbStyle.Setters.Add(
                    new global::System.Windows.Setter(
                        global::System.Windows.Controls.Control.TemplateProperty,
                        template
                    )
                );

                // Insert the style into the Application's resources.
                if (global::System.Windows.Application.Current != null)
                    global::System.Windows.Application.Current.Resources[
                        typeof(global::System.Windows.Controls.Primitives.Thumb)
                    ] = thumbStyle;

                _thumbStyleApplied = true;
            }
        }
#else
        // In non-Windows builds these methods do nothing.
        public static void ApplyGlobalScrollBarPageButtonStyle() { }

        public static void ApplyGlobalScrollBarThumbStyle() { }
#endif

        /// <summary>
        /// Extension method for Eto.Forms.Button that disables its border on WinForms and GTK.
        /// Evaluates the provided variable P against the current Eto backend.
        /// </summary>
        /// <param name="button">The Eto.Forms.Button to configure.</param>
        /// <param name="P">A string representing the current Eto backend platform.</param>
        public static void ConfigureForPlatform(this Eto.Forms.Button button)
        {
            var P = Eto.Platform.Instance.ToString();
#if WINDOWS
            if (P == Eto.Platform.Get(Eto.Platforms.WinForms).ToString())
            {
                // Access the native System.Windows.Forms.Button control via ControlObject.
                var winButton = button.ControlObject as System.Windows.Forms.Button;
                Console.WriteLine($"{button.ControlObject.GetType()}, winButton: {winButton}");
                if (winButton != null)
                {
                    winButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                    winButton.FlatAppearance.BorderSize = 0;
                    
                }
            }
#endif
            if (P == Eto.Platform.Get(Eto.Platforms.Gtk).ToString())
            {
                // Access the native Gtk.Button control using ControlObject.
                var gtkButton = button.ControlObject as Gtk.Button;
                if (gtkButton != null)
                {
                    // Disable borders by setting the Relief style to None.
                    gtkButton.Relief = Gtk.ReliefStyle.None;
                    var cssProvider = new Gtk.CssProvider();
                    cssProvider.LoadFromData(
                        @"
    .black-bg {
        background-image: none;
        background-color: #000000;
    }
"
                    );
                    Gtk.StyleContext.AddProviderForScreen(
                        gtkButton.Screen,
                        cssProvider,
                        Gtk.StyleProviderPriority.Application
                    );
                    gtkButton.StyleContext.AddClass("black-bg");
                }
            }
            // No modifications are required for WPF or other backends.
        }

        /// <summary>
        /// Extension method to apply a dark theme for scrollbars on an Eto.Forms.Container.
        /// Uses custom logic for WinForms (placeholder) and a GTK CSS provider for GTK.
        /// </summary>
        /// <param name="container">An Eto.Forms.Container hosting scrollable content.</param>
        /// <param name="P">A string representing the current Eto backend platform.</param>
        public static void ApplyDarkScrollbarTheme(this Eto.Forms.Container container)
        {
            var P = Eto.Platform.Instance.ToString();
#if WINDOWS
            if (P == Eto.Platform.Get(Eto.Platforms.WinForms).ToString())
            {
                // For WinForms, the native control is often a System.Windows.Forms.Panel when using auto-scrolling.
                // Standard OS-rendered scrollbars are not easily themed, so a custom owner-drawn implementation is typically needed.
                var winControl = container.ControlObject as System.Windows.Forms.Panel;
                if (winControl != null)
                {
                    // Insert your custom owner-drawn scrollbar painting logic here.
                }
            }
#endif
            if (P == Eto.Platform.Get(Eto.Platforms.Gtk).ToString())
            {
                // For GTK, apply a dark theme for scrollbars using a CSS provider.
                var gtkWidget = container.ControlObject as Gtk.Widget;
                if (gtkWidget != null)
                {
                    try
                    {
                        var cssProvider = new Gtk.CssProvider();
                        // Adjust the CSS as needed to further style the scrollbars.
                        cssProvider.LoadFromData("scrollbar { background-color: #2e2e2e; }");
                        // Apply the CSS style to the screen hosting the widget.
                        Gtk.StyleContext.AddProviderForScreen(
                            gtkWidget.Screen,
                            cssProvider,
                            Gtk.StyleProviderPriority.User
                        );
                    }
                    catch (System.Exception ex)
                    {
                        System.Console.WriteLine(
                            "Error applying dark theme to GTK scrollbars: " + ex.Message
                        );
                    }
                }
            }
            // For WPF or other backends, consider theming via native style methods (e.g., XAML styles).
        }
        public static void ConfigureForPlatform(this Eto.Forms.GridView targetGridView)
        {
            var P = Eto.Platform.Instance.ToString();

            if (P == Eto.Platform.Get(Eto.Platforms.Gtk).ToString())
            {
                Console.WriteLine(targetGridView.ControlObject);
                // Access the native Gtk.Button control using ControlObject.
                var gtkButton = targetGridView.ControlObject as Gtk.TreeView;
                if (gtkButton != null)
                {
                    // Disable borders by setting the Relief style to None.
                    Gtk.Adjustment vadjustment = gtkButton.Vadjustment;
                    gtkButton.HeadersVisible = true;
                    gtkButton.HeadersClickable = true;
                    gtkButton.ScrollEvent += (e, a) => { gtkButton.QueueDraw(); };
                    gtkButton.KeyPressEvent += (e, a) => { gtkButton.QueueDraw(); };
                    gtkButton.KeyReleaseEvent += (e, a) => { gtkButton.QueueDraw(); };
                    gtkButton.RubberBanding = false;

                    var cssProvider = new Gtk.CssProvider();
                    cssProvider.LoadFromData(
                        @"
    .black-bg {
        background-image: none;
        background-color: #000000;
    }
"
                    );
                    Gtk.StyleContext.AddProviderForScreen(
                        gtkButton.Screen,
                        cssProvider,
                        Gtk.StyleProviderPriority.Application
                    );
                    gtkButton.StyleContext.AddClass("black-bg");
                }
            }
            // No modifications are required for WPF or other backends.
        }
    }
}
