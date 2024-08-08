using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Eto.Drawing;
using ScottPlot.Eto;
using ScottPlot;
using System.Security;
using System.Drawing.Text;
using Microsoft.EntityFrameworkCore;

namespace HealthMonitor
{
    public class PingAverageByDecaminute
    {
        public string Decaminute;
        public double? LatencyAverage;
    }
    public class NetworkPingStatsForm: Form
    {
        public NetworkPingStatsForm() {
            ScottPlot.Eto.PlotView etoPlot = new() { Size = new Eto.Drawing.Size(800, 600) };
            etoPlot.Plot.XAxis.LabelStyle(fontSize: 24);
            //etoPlot.Plot.XAxis.TickLabelStyle(fontSize: 24);
            etoPlot.Plot.YAxis.LabelStyle(fontSize: 24);
            //etoPlot.Plot.YAxis.TickLabelStyle(fontSize: 24);
            etoPlot.Plot.Legend().FontSize = 24;

            var SaveButton = new Button() { Text = "Save As ..." };
            SaveButton.Click += (e, a) =>
            {
                try
                {
                    var SaveDialog = new SaveFileDialog();
                    SaveDialog.ShowDialog("");
                    var Path = SaveDialog.FileName;

                    etoPlot.Plot.SaveFig(Path, 800, 600, false, 4);
                    MessageBox.Show($"Saved as: {Path}");
                }
                catch (System.Exception E)
                {
                    MessageBox.Show(E.ToString(), MessageBoxType.Error);
                }
            };
            var ReloadButton = new Button() { Text = "Reload" };
            ReloadButton.Click += (e, a) => {  };
            var ResetButton = new Button() { Text = "Reset" };
            ResetButton.Click += (e, a) => { etoPlot.Plot.AxisAuto(); etoPlot.Refresh(); };
            var TopStackLayout = new StackLayout() { Items = { null, ResetButton, ReloadButton, SaveButton, null }, Orientation = Eto.Forms.Orientation.Horizontal, Spacing= 20 };

            //etoPlot.Plot.AddSignal(ScottPlot.Generate.Sin());
            //etoPlot.Plot.AddSignal(ScottPlot.Generate.Cos());
            
            double[] xs = DataGen.Consecutive(51);
            double[] sin = DataGen.Sin(51);
            List<String> series;
            List <PingAverageByDecaminute> pingAveragesByDecaminute =new List<PingAverageByDecaminute>();
            List<String> Decaminutes = new List<string>();
            Dictionary<String, List < PingAverageByDecaminute >> PlotData = new();
            using (var logsContext = new LogsContext())
            {
                series = logsContext.Database.SqlQuery<String>($"SELECT DISTINCT(dest) FROM pings").ToList();
                
            }
            foreach (var item in series)
            {
                using (var logsContext = new LogsContext())
                {
                    var GroupedByDecaminute = logsContext.Pings.Where((x) => x.Dest == item).GroupBy((e) => e.TimeNow.Substring(0, 18)).ToList();
                    Decaminutes = GroupedByDecaminute.Select((e) => e.Key).ToList();
                    pingAveragesByDecaminute = GroupedByDecaminute.Select(e => new PingAverageByDecaminute { Decaminute = e.Key, LatencyAverage = e.Average((x) => x.Latency) }).ToList();
                }
                PlotData.Add(item, pingAveragesByDecaminute);

            }
            //pingAveragesByDecaminute.ForEach((e) => MessageBox.Show($"{e.Dest}, {e.Decaminute}, {e.LatencyAverage}"));


            //etoPlot.Plot.AddScatter(xs, sin);
            etoPlot.Plot.XAxis.DateTimeFormat(true);
            foreach (var item in series)
            {
                var p = etoPlot.Plot.AddScatter(PlotData[item].Select(e => (DateTime.Parse(e.Decaminute+"0").ToLocalTime().ToOADate())).ToArray(), PlotData[item].Select(e => e.LatencyAverage??0).ToArray(), label: item);
                p.Label = item;
                p.MarkerSize = 6;
                p.MarkerLineWidth = 3;
                
            }
            etoPlot.Plot.Legend();


            etoPlot.Plot.Title("Ping stats");
            etoPlot.Plot.XLabel("Date");
            etoPlot.Plot.YLabel("Ping (ms) [Response Time]");
            etoPlot.Refresh();
            var VerticalStackLayout = new StackLayout() { Items = { TopStackLayout, etoPlot }, Orientation = Eto.Forms.Orientation.Vertical, Spacing = 20 };
            Content = VerticalStackLayout;
            Resizable = false;


        }
    }
}
