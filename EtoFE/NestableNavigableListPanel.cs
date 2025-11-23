using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonUi;
using Eto;
using Eto.Containers;
using Eto.Forms;
using EtoFE;

namespace EtoFE
{
    public class NestableNavigableListPanel : Panel
    {
        public string CurrentPanelName = "";

        public NestableNavigableListPanel(
            List<(string Title, object InnerPanel, string Name)> loadOncePanels
        )
        {

            CurrentPanelName = $"RV InvNew Inventory Manager";
            var EmptyPanel = new Panel()
            {
                Width = 3,
                BackgroundColor = ColorSettings.ForegroundColor,
            };
            var EmptyPanelOuter = new StackLayout(new StackLayoutItem(EmptyPanel, true))
            {
                BackgroundColor = ColorSettings.BackgroundColor,
                Padding = 3,
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
            GridView LB = new GridView() { ShowHeader = false, GridLines = GridLines.None };
            LB.Size = new Eto.Drawing.Size(200, -1);
            LB.Columns.Add(
                new GridColumn()
                {
                    HeaderText = "Navigate to...",
                    DataCell = new TextBoxCell(0) { },
                }
            );
            LB.BackgroundColor = ColorSettings.BackgroundColor;
            //LB.TextColor = ColorSettings.ForegroundColor;
            //LB.Font = new Eto.Drawing.Font("Courier", 18);

            Panel CurrentPanel = new Panel() { MinimumSize = new Eto.Drawing.Size(900, 700) };

            int SelectedButtonIndex = -1;
            var UpdateTheme = () =>
            {
                CurrentPanel.BackgroundColor = ColorSettings
                    .RotateAllToPanelSettings(0 * (1 + SelectedButtonIndex))
                    .BackgroundColor;
                EmptyPanel.BackgroundColor = ColorSettings
                    .RotateAllToPanelSettings(0 * SelectedButtonIndex)
                    .LesserForegroundColor;
                EmptyPanelOuter.BackgroundColor = ColorSettings
                    .RotateAllToPanelSettings(60 * SelectedButtonIndex)
                    .BackgroundColor;
            };
            Dictionary<string, object> Panels = new Dictionary<string, object>();
            foreach (var panel in loadOncePanels)
            {
                Panels.Add(
                    TranslationHelper.Translate(panel.Name, panel.Title, Program.lang),
                    panel.Item2
                );
            }
            LB.DataStore = loadOncePanels.Select(x => new List<string>() { x.Item1 }).ToList();

            LB.GridLines = GridLines.None;

            IReadOnlyDictionary<string, object> ROD = Panels;
            LB.CellFormatting += (e, a) =>
            {
                if (a.Row == LB.SelectedRow)
                {
                    a.ForegroundColor = ColorSettings.BackgroundColor;
                    a.BackgroundColor = ColorSettings.ForegroundColor;
                }
                else
                {
                    a.ForegroundColor = ColorSettings.ForegroundColor;
                    a.BackgroundColor = ColorSettings.BackgroundColor;
                }
                a.Font = new Eto.Drawing.Font(
                    "Segoe UI",
                    10,
                    Eto.Drawing.FontStyle.Bold,
                    Eto.Drawing.FontDecoration.None
                );
            };
            List<Button> Buttons = new();
            List<Panel> ButtonsContainer = new();
            int i = 0;
            int SelectedItemIndex = 0;

            // INSERT HERE - After variable declarations but before button event handlers
            Action ResetCurrentPanel = () =>
            {
                // Reset to initial state
                if (CurrentPanel.Content is Panel contentPanel && contentPanel.Content is ILoadOncePanel<object> loadOncePanel)
                {
                    loadOncePanel.Destroy();
                }
                CurrentPanel.Content = null;
                SelectedButtonIndex = -1;
                CurrentPanelName = "RV InvNew Inventory Manager";

                // Reset button styles
                foreach (Button button in Buttons)
                {
                    button.TextColor = ColorSettings.ForegroundColor;
                    button.BackgroundColor = ColorSettings.BackgroundColor;
                    button.Invalidate(true);
                }

                foreach (Panel panel in ButtonsContainer)
                {
                    panel.BackgroundColor = ColorSettings.BackgroundColor;
                    panel.Invalidate(true);
                }

                UpdateTheme();
                this.Invalidate(true);
            };
            foreach ((string, object, string) LoadOncePanel in loadOncePanels)
            {
                Button B = new Button()
                {
                    Text = TranslationHelper.Translate(
                        LoadOncePanel.Item3,
                        LoadOncePanel.Item1,
                        Program.lang
                    ),
                    Width = Program.InnerPanelButtonWidth ?? -1,
                };
                B.ConfigureForPlatform();
                B.DisableHoverBackgroundChange(ColorSettings.SelectedColumnColor);
                //B.VerticalAlignment = VerticalAlignment.Center;
                B.Height = Program.InnerPanelButtonHeight ?? -1;
                B.MinimumSize = new Eto.Drawing.Size(0, 35);
                B.Width = Program.InnerPanelButtonWidth ?? -1;
                B.RemoveBorder();
                B.AlignLeft();

                B.Font = new Eto.Drawing.Font(Program.UIFont, 10) { };
                B.TextColor = ColorSettings.ForegroundColor;
                B.Enabled = true;
                B.BackgroundColor = ColorSettings.BackgroundColor;
                //B.Click += (e, a) => { };
                B.MouseEnter += (e, a) =>
                {
                    Button CurrentLabel = ((Button)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel))
                    {
                        B.TextColor = ColorSettings.SelectedColumnColor;
                        B.BackgroundColor = ColorSettings.ForegroundColor;
                    }
                };
                B.GotFocus += (e, a) =>
                {
                    Button CurrentLabel = ((Button)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel))
                    {
                        B.TextColor = ColorSettings.SelectedColumnColor;
                        B.BackgroundColor = ColorSettings.ForegroundColor;
                    }
                };
                B.LostFocus += (e, a) =>
                {
                    Button CurrentLabel = ((Button)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel))
                    {
                        B.TextColor = ColorSettings.ForegroundColor;
                        B.BackgroundColor = ColorSettings.BackgroundColor;
                    }
                };
                B.MouseLeave += (e, a) =>
                {
                    Button CurrentLabel = ((Button)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel))
                    {
                        B.TextColor = ColorSettings.ForegroundColor;
                        B.BackgroundColor = ColorSettings.BackgroundColor;
                    }
                    this.Invalidate(true);
                };
                B.TabIndex = i;
                B.MouseDown += (e, a) =>
                {
                    Button ClickedLabel = ((Button)e);
                    var CurrentButtonHolderPanel = ButtonsContainer[Buttons.IndexOf(ClickedLabel)];
                    //MessageBox.Show($"Clicked {ClickedLabel.Text}", MessageBoxType.Information);
                    CurrentPanel.SuspendLayout();
                    CurrentPanel.Content = (Control)
                        (
                            (ILoadOncePanel<object>)
                                ROD.GetValueOrDefault<string, object?>(
                                    (string)((string)ClickedLabel.Text),
                                    null
                                )
                        ).GetInnerAsObject();
                    CurrentPanel.ResumeLayout();
                    SelectedButtonIndex = Buttons.IndexOf(ClickedLabel);
                    SelectedItemIndex = SelectedButtonIndex;
                    foreach (Button L in Buttons)
                    {
                        L.TextColor = ColorSettings.ForegroundColor;
                        L.BackgroundColor = ColorSettings.BackgroundColor;
                        L.Invalidate(true);
                    }
                    foreach (Panel L in ButtonsContainer)
                    {
                        L.BackgroundColor = ColorSettings.BackgroundColor;
                        L.Invalidate(true);
                    }
                    ClickedLabel.TextColor = ColorSettings.BackgroundColor;
                    ClickedLabel.BackgroundColor = ColorSettings.ForegroundColor;
                    CurrentButtonHolderPanel.BackgroundColor = ColorSettings.ForegroundColor;
                    this.Size = new Eto.Drawing.Size(-1, -1);
                    this.Invalidate(true);
                    this.TriggerStyleChanged();
                    CurrentPanelName =
                        $"\u300e{ClickedLabel.Text}\u300f RV InvNew Inventory Manager";
                    UpdateTheme();
                };

                B.KeyDown += (e, a) =>
                {
                    if (a.Key == Keys.Enter || a.Key == Keys.Space)
                    {
                        Button ClickedLabel = ((Button)e);
                        var CurrentButtonHolderPanel = ButtonsContainer[
                            Buttons.IndexOf(ClickedLabel)
                        ];
                        //MessageBox.Show($"Clicked {ClickedLabel.Text}", MessageBoxType.Information);
                        CurrentPanel.SuspendLayout();
                        CurrentPanel.Content = (Control)
                            (
                                (ILoadOncePanel<object>)
                                    ROD.GetValueOrDefault<string, object?>(
                                        (string)((string)ClickedLabel.Text),
                                        null
                                    )
                            ).GetInnerAsObject();
                        CurrentPanel.ResumeLayout();
                        SelectedButtonIndex = Buttons.IndexOf(ClickedLabel);
                        SelectedItemIndex = SelectedButtonIndex;
                        foreach (Button L in Buttons)
                        {
                            L.TextColor = ColorSettings.ForegroundColor;
                            L.BackgroundColor = ColorSettings.BackgroundColor;
                            L.Invalidate(true);
                        }
                        foreach (Panel L in ButtonsContainer)
                        {
                            L.BackgroundColor = ColorSettings.BackgroundColor;
                            L.Invalidate(true);
                        }
                        ClickedLabel.TextColor = ColorSettings.BackgroundColor;
                        ClickedLabel.BackgroundColor = ColorSettings.ForegroundColor;
                        CurrentButtonHolderPanel.BackgroundColor = ColorSettings.ForegroundColor;
                        this.Size = new Eto.Drawing.Size(-1, -1);
                        this.Invalidate(true);
                        this.TriggerStyleChanged();
                        CurrentPanelName =
                            $"\u300e{ClickedLabel.Text}\u300f RV InvNew Inventory Manager";
                    }
                    UpdateTheme();
                };

                Buttons.Add(B);
                var BC = new Panel()
                {
                    Height = Program.InnerPanelButtonContainerHeight ?? -1,
                    Width = Program.InnerPanelButtonContainerWidth ?? -1,
                    Content = new StackLayout(B, null) { Padding = new Eto.Drawing.Padding(5, 0) },
                };
                BC.MouseDown += (e, a) =>
                {
                    //MessageBox.Show("Event", "Clicked", MessageBoxType.Information);
                    Panel ClickedPanel = ((Panel)e);
                    Button TargetButton = (Button)
                        ((StackLayout)ClickedPanel.Children.First()).Children.First();
                    //MessageBox.Show($"Clicked {ClickedLabel.Text}", MessageBoxType.Information);

                    CurrentPanel.Content = (Control)
                        (
                            (ILoadOncePanel<object>)
                                ROD.GetValueOrDefault<string, object?>(
                                    (string)((string)TargetButton.Text),
                                    null
                                )
                        ).GetInnerAsObject();
                    SelectedButtonIndex = Buttons.IndexOf(TargetButton);
                    SelectedItemIndex = SelectedButtonIndex;
                    foreach (Button L in Buttons)
                    {
                        L.TextColor = ColorSettings.ForegroundColor;
                        L.BackgroundColor = ColorSettings.BackgroundColor;
                        L.Invalidate(true);
                    }
                    foreach (Panel L in ButtonsContainer)
                    {
                        L.BackgroundColor = ColorSettings.BackgroundColor;
                        L.Invalidate(true);
                    }
                    TargetButton.TextColor = ColorSettings.BackgroundColor;
                    TargetButton.BackgroundColor = ColorSettings.ForegroundColor;
                    ClickedPanel.BackgroundColor = ColorSettings.ForegroundColor;
                    this.Size = new Eto.Drawing.Size(-1, -1);
                    this.Invalidate(true);
                    this.TriggerStyleChanged();
                    CurrentPanelName =
                        $"\u300e{TargetButton.Text}\u300f RV InvNew Inventory Manager";
                    UpdateTheme();
                };
                ButtonsContainer.Add(BC);

                i++;
            }
            LB.SelectedItemsChanged += (sender, e) =>
            {
                CurrentPanel.Content = (Control)
                    (
                        (ILoadOncePanel<object>)
                            ROD.GetValueOrDefault<string, object?>(
                                (string)((List<string>)LB.SelectedItem).First(),
                                null
                            )
                    ).GetInnerAsObject();
                CurrentPanelName = $"{(string)((List<string>)LB.SelectedItem).First()}";
                this.Size = new Eto.Drawing.Size(-1, -1);
                this.Invalidate();
                this.UpdateLayout();
            };
            LB.DisableLines();

            //LB.DisableGridViewEnterKey();
            BackgroundColor = ColorSettings.RotateAllToPanelSettings(0).BackgroundColor;
            Padding = 10;
            Button EnableAccessibilityButton = new Button()
            {
                Text = TranslationHelper.Translate(" Enable Accessibility... ♿👓 "),
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
                MinimumSize = new Eto.Drawing.Size(30, 30),
                BackgroundColor = ColorSettings.BackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
                Width = Program.ControlWidth ?? 100,
            };
            Button QuitCurrentPanelButton = new Button()
            {
                Text = " X ",
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
                MinimumSize = new Eto.Drawing.Size(30, 30),
                BackgroundColor = ColorSettings.SelectedColumnColor,
                TextColor = ColorSettings.ForegroundColor,
            };
            Button RequestLogViewerButton = new Button()
            {
                Text = TranslationHelper.Translate("R"),
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
                MinimumSize = new Eto.Drawing.Size(30, 30),
                BackgroundColor = ColorSettings.BackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
                Width = Program.ControlWidth ?? 100,
            };

            Button PopOutButton = new Button()
            {
                Text = TranslationHelper.Translate("P"),
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
                MinimumSize = new Eto.Drawing.Size(30, 30),
                BackgroundColor = ColorSettings.BackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
                Width = Program.ControlWidth ?? 100,
            };
            EnableAccessibilityButton.DisableHoverBackgroundChange(ColorSettings.BackgroundColor);
            EnableAccessibilityButton.ConfigureForPlatform();
            //QuitCurrentPanelButton.DisableHoverBackgroundChange(Eto.Drawing.Colors.Red);
            EnableAccessibilityButton.Click += (sender, e) =>
            {
                (new ListPanelOptionsAsButtons(loadOncePanels.ToArray())).Show();
            };
            // Replace the existing QuitCurrentPanelButton.Click event handler with this:
            // REPLACE THE EXISTING QuitCurrentPanelButton.Click EVENT HANDLER WITH THIS:
            QuitCurrentPanelButton.Click += (sender, e) =>
            {
                try
                {
                    if (CurrentPanel.Content is Panel contentPanel)
                    {
                        // Check if the content of the panel implements ILoadOncePanel
                        if (contentPanel.Content is ILoadOncePanel<object> loadOncePanel)
                        {
                            loadOncePanel.Destroy();
                            Console.WriteLine("Destroying the current panel.");
                        }
                        // Alternative approach: try to find Destroy method via reflection
                        else if (contentPanel.Content != null)
                        {
                            var contentType = contentPanel.Content.GetType();
                            var destroyMethod = contentType.GetMethod("Destroy");
                            if (destroyMethod != null)
                            {
                                destroyMethod.Invoke(contentPanel.Content, null);
                                Console.WriteLine("Destroying the current panel via reflection.");
                            }
                        }

                        // Clear the content
                        CurrentPanel.Content = null;

                        // Reset to initial state
                        ResetCurrentPanel();  // This now works because it's a lambda in scope
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Unable to destroy panel - CurrentPanel.Content is not a Panel: {CurrentPanel.Content?.GetType()}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error destroying panel: {ex.Message}");
                    // Still try to reset the UI
                    CurrentPanel.Content = null;
                    ResetCurrentPanel();  // This now works because it's a lambda in scope
                }
            };

            // Request Log Viewer button click handler
            RequestLogViewerButton.Click += (sender, e) =>
            {
                var logWindow = new Form()
                {
                    Title = TranslationHelper.Translate("Request Log Viewer"),
                    Size = new Eto.Drawing.Size(900, 600),
                    Content = new RequestLogViewerPanel()
                };
                logWindow.Show();
            };

            // Pop Out button click handler
            PopOutButton.Click += (sender, e) =>
            {
                try
                {
                    if (CurrentPanel.Content is Panel contentPanel && contentPanel.Content != null)
                    {
                        // Find the corresponding LoadOncePanel using the SelectedButtonIndex
                        if (SelectedButtonIndex >= 0 && SelectedButtonIndex < Buttons.Count)
                        {
                            string panelName = Buttons[SelectedButtonIndex].Text;
                            if (ROD.TryGetValue(panelName, out object panelObj) && panelObj is ILoadOncePanel<object> loadOncePanel)
                            {
                                // Destroy the LoadOncePanel before moving the content
                                loadOncePanel.Destroy();
                            }
                        }

                        var popOutWindow = new Form()
                        {
                            Title = CurrentPanelName,
                            Size = new Eto.Drawing.Size(900, 700),
                            Content = contentPanel.Content
                        };
                        popOutWindow.Show();

                        // Reset the current panel to its initial state
                        ResetCurrentPanel();
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Unable to pop out panel - CurrentPanel.Content is not a Panel or has no content: {CurrentPanel.Content?.GetType()}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error popping out panel: {ex.Message}");
                }
            };
            this.SuspendLayout();
            var Inner = new StackLayout(
                new Scrollable()
                {
                    Content = new StackLayout(
                        ButtonsContainer
                            .SelectMany(b =>
                                new[]
                                {
                                    new StackLayoutItem(b),
                                    new StackLayoutItem(
                                        new Panel()
                                        {
                                            Height = 1,
                                            BackgroundColor = ColorSettings.LesserBackgroundColor,
                                        }
                                    ),
                                }
                            )
                            .ToArray()
                    )
                    {
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        Spacing = 0,
                    },
                    Padding = new Eto.Drawing.Padding(),
                    Height = -1,
                    Border = BorderType.None,
                    Width = Program.PanelWidth ?? -1,
                    ExpandContentHeight = false,
                    ExpandContentWidth = true,
                    //BackgroundColor = ColorSettings.RotateAllToPanelSettings(60).BackgroundColor
                    //BackgroundColor = ColorSettings.BackgroundColor,
                    //ScrollSize = new Eto.Drawing.Size(10, 10),
                },
                EmptyPanelOuter,
                new StackLayoutItem(CurrentPanel, true)
            )
            {
                Orientation = Orientation.Horizontal,
                Spacing = 0,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                BackgroundColor = ColorSettings.BackgroundColor,
            };
            var TopPanel = new StackLayout(
                EnableAccessibilityButton,
                null,
                null,
                RequestLogViewerButton,
                PopOutButton,
                QuitCurrentPanelButton
            )
            {
                Spacing = 10,
                Orientation = Orientation.Horizontal,
                Padding = 10,
            };

            Content = new StackLayout(
                new StackLayoutItem(TopPanel),
                new StackLayoutItem(
                    new Panel()
                    {
                        BackgroundColor = ColorSettings.LesserForegroundColor,
                        Height = 1,
                    }
                ),
                new StackLayoutItem(Inner, true)
            )
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Spacing = 1,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Height = -1,
            };
            this.ResumeLayout();
            Padding = 10;
            //Position
        }


    }
}
