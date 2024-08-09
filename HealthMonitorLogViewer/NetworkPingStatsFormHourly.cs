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
using ScottPlot.Renderable;

namespace HealthMonitor
{
    public class PingAverageByHour
    {
        public string Decaminute;
        public double? LatencyAverage;
    }
    public class PingSuccessRateByHour
    {
        public string Decaminute;
        public double? SuccessRate;
    }
    public class NetworkPingStatsFormHourly: Form
    {
        public NetworkPingStatsFormHourly() {
            Title = "HealthMonitor Plots: by Hour";
            Location = new Eto.Drawing.Point(50,50);
            ScottPlot.Eto.PlotView etoPlot = new() { Size = new Eto.Drawing.Size(1000, 300) };
            ScottPlot.Eto.PlotView etoPlotSuccessRates = new() { Size = new Eto.Drawing.Size(1000, 300) };
            etoPlot.Plot.XAxis.LabelStyle(fontSize: 18);
            etoPlot.Plot.YAxis.LabelStyle(fontSize: 18);
            etoPlot.Plot.Legend().FontSize = 10;
            etoPlotSuccessRates.Plot.XAxis.LabelStyle(fontSize: 18);
            etoPlotSuccessRates.Plot.YAxis.LabelStyle(fontSize: 18);
            etoPlotSuccessRates.Plot.Legend().FontSize = 10;

            var SaveButton = new Button() { Text = "Save As ..." };
            SaveButton.Click += (e, a) =>
            {
                try
                {
                    var SaveDialog = new SaveFileDialog();
                    SaveDialog.Title = "Save stats as...";
                    SaveDialog.ShowDialog("");
                    var Path = SaveDialog.FileName;

                    etoPlot.Plot.SaveFig(Path, 2560, 1440, false, 4);
                    MessageBox.Show($"Saved as: {Path}");

                    var SaveDialogSuccessStats = new SaveFileDialog();
                    SaveDialogSuccessStats.Title = "Save success stats as...";
                    SaveDialogSuccessStats.ShowDialog("");
                    var PathSuccessStats = SaveDialogSuccessStats.FileName;

                    etoPlot.Plot.SaveFig(PathSuccessStats, 2560, 1440, false, 4);
                    MessageBox.Show($"Saved as: {PathSuccessStats}");
                }
                catch (System.Exception E)
                {
                    MessageBox.Show(E.ToString(), MessageBoxType.Error);
                }
            };

            var ReloadButton = new Button() { Text = "Reload" };
            ReloadButton.Click += (e, a) => {
                MessageBox.Show("Not implemented", MessageBoxType.Warning);
            };

            var ResetButton = new Button() { Text = "Reset" };
            ResetButton.Click += (e, a) => { 
                etoPlot.Plot.AxisAuto(); 
                etoPlot.Refresh();
                etoPlotSuccessRates.Plot.AxisAuto();
                etoPlotSuccessRates.Refresh();
            };

            var TopStackLayout = new StackLayout() { Items = { null, ResetButton, ReloadButton, SaveButton, null }, Orientation = Eto.Forms.Orientation.Horizontal, Spacing= 20 };

            
            List<String> series;
            List<String> Hours = new List<string>();
            Dictionary<String, List < PingAverageByHour >> PlotData = new();
            Dictionary<String, List<PingSuccessRateByHour>> PlotDataSuccessRates = new();
            using (var logsContext = new LogsContext())
            {
                series = logsContext.Database.SqlQuery<String>($"SELECT DISTINCT(dest) FROM pings").ToList();
                
            }
            foreach (var item in series)
            {
                List<PingAverageByHour> pingAveragesByHour = new List<PingAverageByHour>();
                List<PingSuccessRateByHour> pingSuccessRatesByHour = new List<PingSuccessRateByHour>();
                using (var logsContext = new LogsContext())
                {
                    var GroupedByHour = logsContext.Pings.Where((x) => x.Dest == item).GroupBy((e) => e.TimeNow.Substring(0, 13)).ToList();
                    Hours = GroupedByHour.Select((e) => e.Key).ToList();
                    pingAveragesByHour = GroupedByHour.Select(e => new PingAverageByHour { Decaminute = e.Key, LatencyAverage = e.Average((x) => x.Latency) }).ToList();
                    pingSuccessRatesByHour = GroupedByHour.Select(e => new PingSuccessRateByHour { Decaminute = e.Key, SuccessRate = e.Average((x) => (x.WasItOkNotCorrupt == 1 || x.DidItSucceed == 1) ? 1 : 0 ) }).ToList();
                }
                PlotData.Add(item, pingAveragesByHour);
                PlotDataSuccessRates.Add(item, pingSuccessRatesByHour);

            }

            etoPlot.Plot.XAxis.DateTimeFormat(true);
            etoPlotSuccessRates.Plot.XAxis.DateTimeFormat(true);
            foreach (var item in series)
            {
                var p = etoPlot.Plot.AddScatter(PlotData[item].Select(e => (DateTime.Parse(e.Decaminute+":00:00").ToLocalTime().ToOADate())).ToArray(), PlotData[item].Select(e => e.LatencyAverage??0).ToArray(), label: item);
                var pSuccessRates = etoPlotSuccessRates.Plot.AddScatter(PlotDataSuccessRates[item].Select(e => (DateTime.Parse(e.Decaminute + ":00:00").ToLocalTime().ToOADate())).ToArray(), PlotDataSuccessRates[item].Select(e => e.SuccessRate * 100 ?? 0).ToArray(), label: item);
                p.Label = item;
                p.MarkerSize = 6;
                p.MarkerLineWidth = 3;
                pSuccessRates.MarkerSize = 8;
                pSuccessRates.MarkerLineWidth = 4;

            }

            etoPlot.Plot.Legend();
            etoPlot.Plot.Legend().FontName = "Courier";
            etoPlot.Plot.Legend().Location = Alignment.UpperLeft;
            etoPlot.Plot.Style(dataBackground: System.Drawing.Color.Transparent);
            etoPlot.Plot.Title("Ping stats (Hourly averages)");
            etoPlot.Plot.XLabel("Date/Time");
            etoPlot.Plot.YLabel("Ping [ms] (Response Time)");
            etoPlot.Refresh();

            etoPlotSuccessRates.Plot.Legend();
            etoPlotSuccessRates.Plot.Legend().FontName = "Courier";
            etoPlotSuccessRates.Plot.Legend().Location = Alignment.UpperLeft;
            etoPlotSuccessRates.Plot.Style(dataBackground: System.Drawing.Color.Transparent);
            etoPlotSuccessRates.Plot.Title("Ping stats (Hourly averages) - Success Rates (non-corrupt replies)");
            etoPlotSuccessRates.Plot.XLabel("Date/Time");
            etoPlotSuccessRates.Plot.YLabel("Ping Success Rate [%]");
            etoPlotSuccessRates.Refresh();


            var VerticalStackLayout = new StackLayout() { 
                Items = { 
                    TopStackLayout, 
                    etoPlot, 
                    etoPlotSuccessRates 
                }, 
                Orientation = Eto.Forms.Orientation.Vertical, 
                Spacing = 20 
            };
            Content = VerticalStackLayout;
            Resizable = false;

        }
    }
}
