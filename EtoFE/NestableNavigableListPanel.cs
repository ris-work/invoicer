using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto;
using Eto.Forms;
using EtoFE;
using CommonUi;
using Eto.Containers;

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
            List<StackLayout> Buttons = new();
            int i = 0;
            foreach ((string, object) LoadOncePanel in loadOncePanels)
            {
                Button B = new Button() { Text = LoadOncePanel.Item1 };
                B.DisableHoverBackgroundChange(Eto.Drawing.Colors.PaleVioletRed);
                //B.VerticalAlignment = VerticalAlignment.Center;
                B.Height = 35;

                B.Font = new Eto.Drawing.Font("Gourier", 12) { };
                B.TextColor = Eto.Drawing.Colors.White;
                B.Enabled = true;
                B.Font = new Eto.Drawing.Font(Eto.Drawing.FontFamilies.Monospace, 10, Eto.Drawing.FontStyle.None, Eto.Drawing.FontDecoration.Strikethrough);
                B.BackgroundColor = Eto.Drawing.Colors.Black;
                //B.Click += (e, a) => { };
                
                B.TabIndex = i;
                

                StackLayout FullWidthB = new StackLayout(B);
                Buttons.Add(FullWidthB);
                FullWidthB.MouseEnter += (e, a) =>
                {
                    StackLayout CurrentLabel = ((StackLayout)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel))
                    {
                        B.TextColor = Eto.Drawing.Colors.DarkGoldenrod;
                        B.BackgroundColor = Eto.Drawing.Colors.Purple;
                        FullWidthB.BackgroundColor = Eto.Drawing.Colors.Purple;
                    }
                };
                FullWidthB.MouseLeave += (e, a) =>
                {
                    StackLayout CurrentLabel = ((StackLayout)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel))
                    {
                        B.TextColor = Eto.Drawing.Colors.White;
                        B.BackgroundColor = Eto.Drawing.Colors.Black;
                        FullWidthB.BackgroundColor = Eto.Drawing.Colors.Black;
                    }
                    this.Invalidate();
                };
                
                FullWidthB.MouseDown += (e, a) =>
                {
                    MessageBox.Show("Event", "Mouse down", MessageBoxType.Information);
                    StackLayout ClickedLabel = ((StackLayout)e);
                    //MessageBox.Show($"Clicked {ClickedLabel.Text}", MessageBoxType.Information);

                    CurrentPanel.Content = (Control)
                        (
                            (ILoadOncePanel<object>)
                                ROD.GetValueOrDefault<string, object?>(
                                    (string)((string)((Button)(ClickedLabel.Children.First())).Text),
                                    null
                                )
                        ).GetInnerAsObject();
                    SelectedButtonIndex = Buttons.IndexOf(ClickedLabel);
                    foreach (StackLayout L in Buttons)
                    {
                        ((Button)(L.Children.First())).TextColor = Eto.Drawing.Colors.White;
                        L.BackgroundColor = Eto.Drawing.Colors.Black;
                        L.Invalidate(true);
                    }
                    ((Button)ClickedLabel.Children.First()).TextColor = Eto.Drawing.Colors.Black;
                    ClickedLabel.BackgroundColor = Eto.Drawing.Colors.White;
                    this.Size = new Eto.Drawing.Size(-1, -1);
                    this.Invalidate(true);
                    this.TriggerStyleChanged();
                    CurrentPanelName =
                        $"\u300e{((Button)ClickedLabel.Children.First()).Text}\u300f RV InvNew Inventory Manager";
                };

                B.MouseDown += (e, a) =>
                {
                    MessageBox.Show("Event", "Mouse down", MessageBoxType.Information);
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
                    SelectedButtonIndex = Buttons.IndexOf(Buttons.Where(e => e.Children.First() == ClickedLabel).First());
                    foreach (StackLayout L in Buttons)
                    {
                        ((Button)(L.Children.First())).TextColor = Eto.Drawing.Colors.White;
                        L.BackgroundColor = Eto.Drawing.Colors.Black;
                        L.Invalidate(true);
                    }
                    ((Button)ClickedLabel).TextColor = Eto.Drawing.Colors.Black;
                    ClickedLabel.BackgroundColor = Eto.Drawing.Colors.White;
                    this.Size = new Eto.Drawing.Size(-1, -1);
                    this.Invalidate(true);
                    this.TriggerStyleChanged();
                    CurrentPanelName =
                        $"\u300e{((Button)ClickedLabel).Text}\u300f RV InvNew Inventory Manager";
                };
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
            EnableAccessibilityButton.Click += (sender, e) =>
            {
                (new ListPanelOptionsAsButtons(loadOncePanels.ToArray())).Show();
            };
            var Inner = new StackLayout(
                new DragScrollable(){Content = new StackLayout(Buttons.Select(b => new StackLayoutItem(Content  = new StackLayout(b) )).ToArray())
                {
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Spacing = 2,
                }, Height = 100, Border = BorderType.None },
                new Button() { Width = 3, BackgroundColor = Eto.Drawing.Colors.Beige },
                new StackLayoutItem(CurrentPanel)
            )
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
            var TopPanel = new StackLayout(EnableAccessibilityButton) { Spacing = 10 };
            Content = new StackLayout(TopPanel, Inner );
            Padding = 10;
            //Position
        }
    }
}
