using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Eto;
using HealthMonitor;

namespace HealthMonitorLogViewer
{
    public class NavigableListForm: Form
    {
        public NavigableListForm()
        {
            ListBox LB = new ListBox() { };
            Panel CurrentPanel = new Panel();
            
            var loadOncePanels = new List<(string, object)>() {
                ("Network Stats Panel", (new LoadOncePanel<NetworkPingStatsPanel>())),
                ("Network Stats Panel 2", (new LoadOncePanel<NetworkPingStatsPanel>()))
            };
            Dictionary<string, object> Panels = new Dictionary<string, object>();
            foreach (var panel in loadOncePanels) {
                Panels.Add(panel.Item1, panel.Item2);
            }
            LB.DataStore = loadOncePanels.Select(x => x.Item1).ToList();
            Content = new StackLayout(new StackLayoutItem(LB), new StackLayoutItem(CurrentPanel)) {Orientation = Orientation.Horizontal};
            IReadOnlyDictionary<string, object> ROD = Panels;
            LB.SelectedValueChanged += (sender, e) => {
                CurrentPanel.Content = (Control)((ILoadOncePanel<object>)ROD.GetValueOrDefault<string, object?>((string)LB.SelectedValue, null)).GetInnerAsObject();
            };
        }
    }
}
