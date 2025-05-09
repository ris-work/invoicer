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

        public NestableNavigableListPanel(List<(string Title, object InnerPanel)> loadOncePanels)
        {
            CurrentPanelName = $"RV InvNew Inventory Manager";
            GridView LB = new GridView() { ShowHeader = false, GridLines = GridLines.None };
            LB.Size = new Eto.Drawing.Size(200, 600);
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

            Panel CurrentPanel = new Panel() { MinimumSize = new Eto.Drawing.Size(1100, 700) };

            int SelectedButtonIndex = -1;
            Dictionary<string, object> Panels = new Dictionary<string, object>();
            foreach (var panel in loadOncePanels)
            {
                Panels.Add(panel.Item1, panel.Item2);
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
                    a.ForegroundColor = Eto.Drawing.Colors.Wheat;
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
            foreach ((string, object) LoadOncePanel in loadOncePanels)
            {
                Button B = new Button() { Text = LoadOncePanel.Item1 };
                B.ConfigureForPlatform();
                B.DisableHoverBackgroundChange(ColorSettings.SelectedColumnColor);
                //B.VerticalAlignment = VerticalAlignment.Center;
                B.Height = 35;
                B.MinimumSize = new Eto.Drawing.Size(0, 35);

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
                    this.Invalidate();
                };
                B.TabIndex = i;
                B.MouseDown += (e, a) =>
                {
                    Button ClickedLabel = ((Button)e);
                    var CurrentButtonHolderPanel = ButtonsContainer[Buttons.IndexOf(ClickedLabel)];
                    //MessageBox.Show($"Clicked {ClickedLabel.Text}", MessageBoxType.Information);

                    CurrentPanel.Content = (Control)
                        (
                            (ILoadOncePanel<object>)
                                ROD.GetValueOrDefault<string, object?>(
                                    (string)((string)ClickedLabel.Text),
                                    null
                                )
                        ).GetInnerAsObject();
                    SelectedButtonIndex = Buttons.IndexOf(ClickedLabel);
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

                        CurrentPanel.Content = (Control)
                            (
                                (ILoadOncePanel<object>)
                                    ROD.GetValueOrDefault<string, object?>(
                                        (string)((string)ClickedLabel.Text),
                                        null
                                    )
                            ).GetInnerAsObject();
                        SelectedButtonIndex = Buttons.IndexOf(ClickedLabel);
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
                };

                Buttons.Add(B);
                var BC = new Panel() { Content = new StackLayout(B, null) };
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
            BackgroundColor = ColorSettings.BackgroundColor;
            Padding = 10;
            Button EnableAccessibilityButton = new Button()
            {
                Text = " Enable Accessibility... ♿👓 ",
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
                MinimumSize = new Eto.Drawing.Size(30, 30),
                BackgroundColor = ColorSettings.BackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
            };
            Button QuitCurrentPanelButton = new Button()
            {
                Text = " X ",
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
                MinimumSize = new Eto.Drawing.Size(30, 30),
                BackgroundColor = Eto.Drawing.Colors.DarkRed,
                TextColor = ColorSettings.BackgroundColor,
            };
            EnableAccessibilityButton.DisableHoverBackgroundChange(ColorSettings.BackgroundColor);
            EnableAccessibilityButton.ConfigureForPlatform();
            //QuitCurrentPanelButton.DisableHoverBackgroundChange(Eto.Drawing.Colors.Red);
            EnableAccessibilityButton.Click += (sender, e) =>
            {
                (new ListPanelOptionsAsButtons(loadOncePanels.ToArray())).Show();
            };
            QuitCurrentPanelButton.Click += (sender, e) =>
            {
                (new ListPanelOptionsAsButtons(loadOncePanels.ToArray())).Show();
            };
            var Inner = new StackLayout(
                new Scrollable()
                {
                    Content = new StackLayout(
                        ButtonsContainer.Select(b => new StackLayoutItem(b)).ToArray()
                    )
                    {
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        Spacing = 2,
                    },
                    Height = 100,
                    Border = BorderType.None,
                    //BackgroundColor = ColorSettings.BackgroundColor,
                    //ScrollSize = new Eto.Drawing.Size(10, 10),
                },
                new Panel() { Width = 3, BackgroundColor = Eto.Drawing.Colors.Beige },
                new StackLayoutItem(CurrentPanel)
            )
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                BackgroundColor = ColorSettings.BackgroundColor,
            };
            var TopPanel = new StackLayout(
                EnableAccessibilityButton,
                null,
                null,
                QuitCurrentPanelButton,
                null
            )
            {
                Spacing = 10,
                Orientation = Orientation.Horizontal,
                Padding = 10,
            };
            Content = new StackLayout(TopPanel, Inner)
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
            Padding = 10;
            //Position
        }
    }
}
