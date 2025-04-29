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
            LB.BackgroundColor = Eto.Drawing.Colors.Black;
            //LB.TextColor = Eto.Drawing.Colors.White;
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
                    a.ForegroundColor = Eto.Drawing.Colors.Black;
                    a.BackgroundColor = Eto.Drawing.Colors.White;
                }
                else
                {
                    a.ForegroundColor = Eto.Drawing.Colors.Wheat;
                    a.BackgroundColor = Eto.Drawing.Colors.Black;
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
                B.DisableHoverBackgroundChange(Eto.Drawing.Colors.PaleVioletRed);
                //B.VerticalAlignment = VerticalAlignment.Center;
                B.Height = 50;

                B.Font = new Eto.Drawing.Font("Gourier", 12) { };
                B.TextColor = Eto.Drawing.Colors.White;
                B.Enabled = true;
                B.BackgroundColor = Eto.Drawing.Colors.Black;
                //B.Click += (e, a) => { };
                B.MouseEnter += (e, a) =>
                {
                    Button CurrentLabel = ((Button)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel))
                    {
                        B.TextColor = Eto.Drawing.Colors.DarkGoldenrod;
                        B.BackgroundColor = Eto.Drawing.Colors.Purple;
                    }
                };
                B.GotFocus += (e, a) =>
                {
                    Button CurrentLabel = ((Button)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel))
                    {
                        B.TextColor = Eto.Drawing.Colors.DarkGoldenrod;
                        B.BackgroundColor = Eto.Drawing.Colors.Purple;
                    }
                };
                B.LostFocus += (e, a) =>
                {
                    Button CurrentLabel = ((Button)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel))
                    {
                        B.TextColor = Eto.Drawing.Colors.White;
                        B.BackgroundColor = Eto.Drawing.Colors.Black;
                    }
                };
                B.MouseLeave += (e, a) =>
                {
                    Button CurrentLabel = ((Button)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel))
                    {
                        B.TextColor = Eto.Drawing.Colors.White;
                        B.BackgroundColor = Eto.Drawing.Colors.Black;
                    }
                    this.Invalidate();
                };
                B.TabIndex = i;
                B.MouseDown += (e, a) =>
                {
                    Button ClickedLabel = ((Button)e);
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
                        L.TextColor = Eto.Drawing.Colors.White;
                        L.BackgroundColor = Eto.Drawing.Colors.Black;
                        L.Invalidate(true);
                    }
                    foreach (Panel L in ButtonsContainer)
                    {
                        L.BackgroundColor = Eto.Drawing.Colors.Black;
                        L.Invalidate(true);
                    }
                    ClickedLabel.TextColor = Eto.Drawing.Colors.Black;
                    ClickedLabel.BackgroundColor = Eto.Drawing.Colors.White;
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
                            L.TextColor = Eto.Drawing.Colors.White;
                            L.BackgroundColor = Eto.Drawing.Colors.Black;
                            L.Invalidate(true);
                        }
                        foreach (Panel L in ButtonsContainer)
                        {
                            L.BackgroundColor = Eto.Drawing.Colors.Black;
                            L.Invalidate(true);
                        }
                        ClickedLabel.TextColor = Eto.Drawing.Colors.Black;
                        ClickedLabel.BackgroundColor = Eto.Drawing.Colors.White;
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
                        L.TextColor = Eto.Drawing.Colors.White;
                        L.BackgroundColor = Eto.Drawing.Colors.Black;
                        L.Invalidate(true);
                    }
                    foreach (Panel L in ButtonsContainer)
                    {
                        L.BackgroundColor = Eto.Drawing.Colors.Black;
                        L.Invalidate(true);
                    }
                    TargetButton.TextColor = Eto.Drawing.Colors.Black;
                    TargetButton.BackgroundColor = Eto.Drawing.Colors.White;
                    ClickedPanel.BackgroundColor = Eto.Drawing.Colors.White;
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
            BackgroundColor = Eto.Drawing.Colors.Black;
            Padding = 10;
            Button EnableAccessibilityButton = new Button()
            {
                Text = " Enable Accessibility... ♿👓 ",
                Font = new Eto.Drawing.Font("Gourier", 10),
                MinimumSize = new Eto.Drawing.Size(30, 30),
                BackgroundColor = Eto.Drawing.Colors.Black,
                TextColor = Eto.Drawing.Colors.White,
            };
            Button QuitCurrentPanelButton = new Button()
            {
                Text = " X ",
                Font = new Eto.Drawing.Font("Gourier", 10),
                MinimumSize = new Eto.Drawing.Size (30, 30),
                BackgroundColor = Eto.Drawing.Colors.DarkRed,
                TextColor = Eto.Drawing.Colors.Black,
                
            };
            EnableAccessibilityButton.DisableHoverBackgroundChange(Eto.Drawing.Colors.Black);
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
                        Spacing = 5,
                    },
                    Height = 100,
                    Border = BorderType.None,
                },
                new Button() { Width = 3, BackgroundColor = Eto.Drawing.Colors.Beige },
                new StackLayoutItem(CurrentPanel)
            )
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
            var TopPanel = new StackLayout(EnableAccessibilityButton, null, null, QuitCurrentPanelButton, null) { Spacing = 10, Orientation = Orientation.Horizontal, Padding = 10 };
            Content = new StackLayout(TopPanel, Inner) { HorizontalContentAlignment = HorizontalAlignment.Stretch};
            Padding = 10;
            //Position
        }
    }
}
