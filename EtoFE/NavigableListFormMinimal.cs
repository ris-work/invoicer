using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommonUi;
using Eto;
using Eto.Containers;
using Eto.Forms;

namespace EtoFE
{
    public class NavigableListFormMinimal : Form
    {
        public NavigableListFormMinimal()
        {
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

            Panel CurrentPanel = new Panel();

            var loadOncePanels = new List<string>()
            {
                "Network Stats Panel",
                "Network Stats Panel 2",
                "Network Stats Panel 3",
                "Network Stats Panel 4",
            };
            LB.DataStore = loadOncePanels.Select(x => new List<string>() { x }).ToList();
            Content = new StackLayout(new StackLayoutItem(LB), new StackLayoutItem(CurrentPanel))
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
            };
            LB.GridLines = GridLines.None;
            //Styles.Add<System.Windows.Controls.GridView>(null, e => e.cell);

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
            BackgroundColor = ColorSettings.BackgroundColor;
            Padding = 10;
        }
    }
}
