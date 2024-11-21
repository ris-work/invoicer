using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Eto;
using HealthMonitor;
using EtoFE;
using System.IO;

namespace HealthMonitorLogViewer
{
    public class NavigableListForm: Form
    {
        public NavigableListForm()
        {
            Title = $"RV Health and Network Monitor, OSLv3 [{Config.LogFile}]";
            GridView LB = new GridView() { ShowHeader = false, GridLines = GridLines.None };
            LB.Size = new Eto.Drawing.Size(200, 600);
            LB.Columns.Add(new GridColumn() { HeaderText = "Navigate to...", DataCell = new TextBoxCell(0){ } });
            LB.BackgroundColor = Eto.Drawing.Colors.Black;
            //LB.TextColor = Eto.Drawing.Colors.White;
            //LB.Font = new Eto.Drawing.Font("Courier", 18);
            
            Panel CurrentPanel = new Panel() { MinimumSize = new Eto.Drawing.Size(1100,700) };
            
            var loadOncePanels = (new List<(string, object)>() {
                ($" 🕸 Network Ping Stats 💾{Environment.NewLine}    (by decaminute (10 minutes)) ", (new LoadOncePanel<NetworkPingStatsPanel>())),
                (" ⏱ Network Ping Stats (by hour) ", (new LoadOncePanel<NetworkPingStatsFormHourly>())),
                (" 📃 Process Stats ", (new LoadOncePanel<ProcessStatsFormHourlyPanel>())),
                (" ⚙ Process Stats (Advanced) ", (new LoadOncePanel<MainModuleProcessStatsFormHourlyPanel>())),
                (" 🖥 Machine Stats ", (new LoadOncePanel<MachineStatsPanel>())),
                (" 🎫 About ", (new LoadOncePanel<AboutPanel>()))
            }).ToArray();
            int SelectedButtonIndex = -1;
            Dictionary<string, object> Panels = new Dictionary<string, object>();
            foreach (var panel in loadOncePanels) {
                Panels.Add(panel.Item1, panel.Item2);
            }
            LB.DataStore = loadOncePanels.Select(x => new List<string>() { x.Item1  }).ToList();
            
            LB.GridLines = GridLines.None;

            IReadOnlyDictionary<string, object> ROD = Panels;
            LB.CellFormatting += (e, a) => {
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
                a.Font = new Eto.Drawing.Font("Segoe UI", 10, Eto.Drawing.FontStyle.Bold, Eto.Drawing.FontDecoration.None);
            };
            List<Label> Buttons = new();
            int i = 0;
            foreach ((string, object) LoadOncePanel in loadOncePanels)
            {
                Label B = new Label() { Text = LoadOncePanel.Item1 };
                B.VerticalAlignment = VerticalAlignment.Center;
                B.Height = 50;
                
                B.Font = new Eto.Drawing.Font("Gourier", 12) { };
                B.TextColor = Eto.Drawing.Colors.White;
                B.Enabled = true;
                B.BackgroundColor = Eto.Drawing.Colors.Black;
                //B.Click += (e, a) => { };
                B.MouseEnter += (e, a) => {
                    Label CurrentLabel = ((Label)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel)) {
                        B.TextColor = Eto.Drawing.Colors.DarkGoldenrod;
                        B.BackgroundColor = Eto.Drawing.Colors.Purple;
                    }
                };
                B.MouseLeave += (e, a) => {
                    Label CurrentLabel = ((Label)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel))
                    {
                        B.TextColor = Eto.Drawing.Colors.White;
                        B.BackgroundColor = Eto.Drawing.Colors.Black;
                    }
                    this.Invalidate();
                };
                B.TabIndex = i;
                B.MouseDown += (e, a) => {
                    Label ClickedLabel = ((Label)e);
                    //MessageBox.Show($"Clicked {ClickedLabel.Text}", MessageBoxType.Information);
                    
                    
                    CurrentPanel.Content = (Control)((ILoadOncePanel<object>)ROD.GetValueOrDefault<string, object?>((string)((string)ClickedLabel.Text), null)).GetInnerAsObject();
                    SelectedButtonIndex = Buttons.IndexOf(ClickedLabel);
                    foreach (Label L in Buttons)
                    {
                        L.TextColor = Eto.Drawing.Colors.White;
                        L.BackgroundColor = Eto.Drawing.Colors.Black;
                        L.Invalidate(true);
                    }
                    ClickedLabel.TextColor = Eto.Drawing.Colors.Black;
                    ClickedLabel.BackgroundColor = Eto.Drawing.Colors.White;
                    this.Size = new Eto.Drawing.Size(-1, -1);
                    this.Invalidate(true);
                    this.TriggerStyleChanged();
                    Title = $"\u300e{ClickedLabel.Text}\u300f RV Health and Network Monitor, OSLv3 [{Config.LogFile}]";
                };
                
                
                Buttons.Add(B);
                i++;
                
            }
            LB.SelectedItemsChanged += (sender, e) => {
                CurrentPanel.Content = (Control)((ILoadOncePanel<object>)ROD.GetValueOrDefault<string, object?>((string)((List<string>)LB.SelectedItem).First(), null)).GetInnerAsObject();
                Title = $"{ (string)((List<string>)LB.SelectedItem).First()}";
                this.Size = new Eto.Drawing.Size(-1, -1);
                this.Invalidate();
                this.UpdateLayout();
            };
            LB.DisableLines();
            //LB.DisableGridViewEnterKey();
            BackgroundColor = Eto.Drawing.Colors.Black;
            Padding = 10;
            Button EnableAccessibilityButton = new Button() { Text = "Enable Accessibility... ♿👓", BackgroundColor = Eto.Drawing.Colors.Black, TextColor = Eto.Drawing.Colors.White };
            EnableAccessibilityButton.Click += (sender, e) => {
                (new ListPanelOptionsAsButtons(loadOncePanels)).Show();
            };
            var Inner = new StackLayout(new StackLayout(Buttons.Select(b => new StackLayoutItem(b)).ToArray()) { HorizontalContentAlignment = HorizontalAlignment.Stretch, Spacing = 5 }, new Button() { Width = 3, BackgroundColor = Eto.Drawing.Colors.Beige }, new StackLayoutItem(CurrentPanel)) { Orientation = Orientation.Horizontal, Spacing = 10, VerticalContentAlignment = VerticalAlignment.Stretch };
            var TopPanel = new StackLayout(EnableAccessibilityButton) { Spacing = 10 };
            Content = new StackLayout(TopPanel, Inner);
            Padding = 10;
            //Position
        }
    }
}
