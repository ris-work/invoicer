using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Eto.Drawing;
using ScottPlot.Eto;
using System.Security;
using System.Drawing.Text;
using Microsoft.EntityFrameworkCore;
using ScottPlot.Rendering;
using ABI.System.Collections.Generic;
//using ScottPlot;
using System.Dynamic;
using System.Diagnostics;
using System.Globalization;


namespace HealthMonitor
{
    public class PingAverageByDecaminute
    {
        public string Decaminute;
        public double? LatencyAverage;
    }
    public class PingSuccessRateByDecaminute
    {
        public string Decaminute;
        public double? SuccessRate;
    }
    public class NetworkPingStatsForm: Form
    {
        public NetworkPingStatsForm() {
            Title = $"HealthMonitor Plots: by Decaminute [{Config.LogFile}]";
            Location = new Eto.Drawing.Point(50,50);
            ScottPlot.Eto.EtoPlot etoPlot = new() { Size = new Eto.Drawing.Size(1000, 300) };
            ScottPlot.Eto.EtoPlot etoPlotSuccessRates = new() { Size = new Eto.Drawing.Size(1000, 300) };
            etoPlot.Plot.Axes.Bottom.Label.FontSize = 18;
            etoPlot.Plot.Axes.Left.Label.FontSize = 18;
            etoPlot.Plot.Legend.FontSize = 10;
            etoPlotSuccessRates.Plot.Axes.Bottom.Label.FontSize = 18;
            etoPlotSuccessRates.Plot.Axes.Left.Label.FontSize = 18;
            etoPlotSuccessRates.Plot.Legend.FontSize = 10;
            //MovableByWindowBackground = true;

            var SaveButton = new Button() { Text = "💾 Save As ..." };
            SaveButton.Click += (e, a) =>
            {
                try
                {
                    var SaveDialog = new SaveFileDialog();
                    SaveDialog.Title = "Save stats as (please add PNG extension yourself)...";
                    SaveDialog.Filters.Add(Config.PNGFilter);
                    SaveDialog.Filters.Add(Config.SVGFilter);
                    SaveDialog.ShowDialog("");
                    var Path = SaveDialog.FileName;

                    if (SaveDialog.CurrentFilterIndex == 0)
                    {
                        etoPlot.Plot.Save(Path, 2560, 1440);
                    }
                    else
                    {
                        etoPlot.Plot.SaveSvg(Path, 2560, 1440);
                    }
                    MessageBox.Show($"Saved as: {Path}", "Saved!", MessageBoxType.Information);

                    var SaveDialogSuccessStats = new SaveFileDialog();
                    SaveDialogSuccessStats.Title = "Save success stats as (please add PNG extension yourself)...";
                    SaveDialogSuccessStats.Filters.Add(Config.PNGFilter);
                    SaveDialogSuccessStats.Filters.Add(Config.SVGFilter);
                    SaveDialogSuccessStats.ShowDialog("");
                    var PathSuccessStats = SaveDialogSuccessStats.FileName;
                    if (SaveDialogSuccessStats.CurrentFilterIndex == 0)
                    {
                        etoPlotSuccessRates.Plot.Save(PathSuccessStats, 2560, 1440);
                    }
                    else
                    {
                        etoPlotSuccessRates.Plot.SaveSvg(PathSuccessStats, 2560, 1440);
                    }

                    
                    MessageBox.Show($"Saved as: {PathSuccessStats}", "Saved!", MessageBoxType.Information);
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

            var ResetButton = new Button() { Text = "🔄 Reset" };
            ResetButton.Click += (e, a) => { 
                etoPlot.Plot.Axes.AutoScale(); 
                etoPlot.Refresh();
                etoPlotSuccessRates.Plot.Axes.AutoScale();
                etoPlotSuccessRates.Refresh();
            };

            var TopStackLayout = new StackLayout() { 
                Items = { 
                    null, 
                    ResetButton, 
                    ReloadButton, 
                    SaveButton, 
                    null 
                }, 
                Orientation = Eto.Forms.Orientation.Horizontal, 
                Spacing= 20, 
                Size=new Eto.Drawing.Size(-1, -1) 
            };

            
            List<String> series;
            List<String> Decaminutes = new List<string>();
            Dictionary<String, List < PingAverageByDecaminute >> PlotData = new();
            Dictionary<String, List<PingSuccessRateByDecaminute>> PlotDataSuccessRates = new();
            using (var logsContext = new LogsContext())
            {
                series = logsContext.Database.SqlQuery<String>($"SELECT DISTINCT(dest) FROM pings").ToList();
                
            }
            foreach (var item in series)
            {
                List<PingAverageByDecaminute> pingAveragesByDecaminute = new List<PingAverageByDecaminute>();
                List<PingSuccessRateByDecaminute> pingSuccessRatesByDecaminute = new List<PingSuccessRateByDecaminute>();
                using (var logsContext = new LogsContext())
                {
                    
                    var GroupedByDecaminute = logsContext.Pings.Where((x) => x.Dest == item).GroupBy((e) => e.TimeNow.Substring(0, 18)).ToList();
                    var GroupedByHour = logsContext.Pings.Where((x) => x.Dest == item).GroupBy((e) => e.TimeNow.Substring(0, 16)).ToList();
                    Decaminutes = GroupedByDecaminute.Select((e) => e.Key).ToList();
                    pingAveragesByDecaminute = GroupedByDecaminute.Select(e => new PingAverageByDecaminute { Decaminute = e.Key, LatencyAverage = e.Average((x) => x.Latency) }).ToList();
                    pingSuccessRatesByDecaminute = GroupedByDecaminute.Select(e => new PingSuccessRateByDecaminute { Decaminute = e.Key, SuccessRate = e.Average((x) => (x.WasItOkNotCorrupt == 1 || x.DidItSucceed == 1) ? 1 : 0 ) }).ToList();
                }
                PlotData.Add(item, pingAveragesByDecaminute);
                PlotDataSuccessRates.Add(item, pingSuccessRatesByDecaminute);

            }

            etoPlot.Plot.Axes.DateTimeTicksBottom();
            etoPlotSuccessRates.Plot.Axes.DateTimeTicksBottom();
            foreach (var item in series)
            {
                var p = etoPlot.Plot.Add.SignalXY(PlotData[item].Select(e => (DateTime.Parse(e.Decaminute+"0").ToLocalTime().ToOADate())).ToArray(), PlotData[item].Select(e => e.LatencyAverage??0).ToArray());
                var pSuccessRates = etoPlotSuccessRates.Plot.Add.SignalXY(PlotDataSuccessRates[item].Select(e => (DateTime.Parse(e.Decaminute + "0").ToLocalTime().ToOADate())).ToArray(), PlotDataSuccessRates[item].Select(e => e.SuccessRate * 100 ?? 0).ToArray());
                p.LegendText = item;
                p.MarkerSize = 8;
                p.MarkerLineWidth = 8;
                ScottPlot.MarkerShape MarkerShapeForItem = PlotUtils.GetRandomMarkerShape();
                ScottPlot.LinePattern LinePatternForItem = PlotUtils.GetRandomLinePattern();
                p.MarkerShape = MarkerShapeForItem;
                p.LinePattern = LinePatternForItem;
                pSuccessRates.LegendText = item;
                pSuccessRates.MarkerSize = 8;
                pSuccessRates.MarkerLineWidth = 8;
                pSuccessRates.MarkerShape = MarkerShapeForItem;
                pSuccessRates.LinePattern = LinePatternForItem;


            }

            etoPlot.Plot.ShowLegend();
            etoPlot.Plot.Legend.SymbolWidth = 40;
            etoPlot.Plot.Legend.FontName = "Courier";
            etoPlot.Plot.Legend.InterItemPadding = new ScottPlot.PixelPadding(1, 1, 1, 1);
            etoPlot.Plot.Legend.Alignment = ScottPlot.Alignment.UpperLeft;
            etoPlot.Plot.DataBackground.Color = ScottPlot.Colors.Transparent;
            etoPlot.Plot.Title("Ping stats");
            etoPlot.Plot.XLabel("Date/Time");
            etoPlot.Plot.YLabel("Ping [ms] (Response Time)");
            etoPlot.Plot.Axes.SetLimitsY(bottom: -5, top: double.PositiveInfinity);
            etoPlot.Refresh();

            etoPlotSuccessRates.Plot.ShowLegend();
            etoPlotSuccessRates.Plot.Legend.SymbolWidth = 40;
            etoPlotSuccessRates.Plot.Legend.FontName = "Courier";
            etoPlotSuccessRates.DisplayScale = 1;
            etoPlotSuccessRates.Plot.Legend.InterItemPadding = new ScottPlot.PixelPadding(1, 1, 1, 1);
            etoPlotSuccessRates.Plot.Legend.Alignment = ScottPlot.Alignment.UpperLeft;
            etoPlotSuccessRates.Plot.DataBackground.Color = ScottPlot.Colors.Transparent;
            etoPlotSuccessRates.Plot.Title("Ping stats (non-corrupt replies)");
            etoPlotSuccessRates.Plot.XLabel("Date/Time");
            etoPlotSuccessRates.Plot.YLabel("Ping Success Rate [%]");
            etoPlotSuccessRates.Plot.Axes.SetLimitsY(bottom: -5, top: 105);
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
