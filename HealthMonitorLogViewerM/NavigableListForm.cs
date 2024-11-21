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
            GridView LB = new GridView() { ShowHeader = false, GridLines = GridLines.None };
            LB.Size = new Eto.Drawing.Size(200, 600);
            LB.Columns.Add(new GridColumn() { HeaderText = "Navigate to...", DataCell = new TextBoxCell(0){ } });
            LB.BackgroundColor = Eto.Drawing.Colors.Black;
            //LB.TextColor = Eto.Drawing.Colors.White;
            //LB.Font = new Eto.Drawing.Font("Courier", 18);
            
            Panel CurrentPanel = new Panel();
            
            var loadOncePanels = new List<(string, object)>() {
                ("Network Stats Panel", (new LoadOncePanel<NetworkPingStatsPanel>())),
                ("Network Stats Panel 2", (new LoadOncePanel<NetworkPingStatsPanel>())),
                ("Network Stats Panel 3", (new LoadOncePanel<NetworkPingStatsPanel>())),
                ("Network Stats Panel 4", (new LoadOncePanel<NetworkPingStatsPanel>()))
            };
            Dictionary<string, object> Panels = new Dictionary<string, object>();
            foreach (var panel in loadOncePanels) {
                Panels.Add(panel.Item1, panel.Item2);
            }
            LB.DataStore = loadOncePanels.Select(x => new List<string>() { x.Item1  }).ToList();
            Content = new StackLayout(new StackLayoutItem(LB), new StackLayoutItem(CurrentPanel)) { Orientation = Orientation.Horizontal, Spacing = 10 };
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
        }
    }
}
