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
using ABI.System.Collections.Generic;
using ScottPlot.SnapLogic;

namespace HealthMonitor
{
    public class MemoryUsageByHour
    {
        public string Hour;
        public double? WorkingSet;
    }
    public class CpuUsageByHour
    {
        public string Hour;
        public double? CpuTimeDiff;
    }
    public class ProcessStatsFormHourly: Form
    {
        public ProcessStatsFormHourly(string ProcessName) {
            Title = "HealthMonitor Process Plots: by Hour";
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

                    etoPlotSuccessRates.Plot.SaveFig(PathSuccessStats, 2560, 1440, false, 4);
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

            var FilterCriteria = new List<string>(){ "Any", "Process name", "Window title" };
            var RadioProcessFilter = new RadioButtonList() { DataStore = FilterCriteria, Orientation = Eto.Forms.Orientation.Vertical, SelectedValue = FilterCriteria[0], };
            var FilterText = new TextBox();
            FilterText.PlaceholderText = "explorer";
            
            var FilterTextStack = new StackLayout() { 
                Items = { null, new Label() { Text = "Filter text:" }, FilterText, null },
                Orientation = Eto.Forms.Orientation.Vertical,
                Spacing = 5,
                Size = new Eto.Drawing.Size(-1, -1),
                HorizontalContentAlignment = Eto.Forms.HorizontalAlignment.Stretch,
                VerticalContentAlignment = Eto.Forms.VerticalAlignment.Stretch,
            };
            var ProcessList = new ComboBox();
            var ProcessSelectorPanel = new StackLayout()
            {

            };
            var ProcessSelectorAndFilter = new StackLayout()
            {
                Items = { null, FilterTextStack, ProcessList, RadioProcessFilter, null },
                Orientation = Eto.Forms.Orientation.Horizontal,
                Spacing = 20,
                Size = new Eto.Drawing.Size(-1, -1),
                HorizontalContentAlignment = Eto.Forms.HorizontalAlignment.Stretch,
                VerticalContentAlignment = Eto.Forms.VerticalAlignment.Bottom,
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

            
            List<WindowTitle> ProcessCandidates;
            List<String> Decaminutes = new List<string>();
            Dictionary<String, List < CpuUsageByHour >> PlotData = new();
            Dictionary<String, List<MemoryUsageByHour>> PlotDataSuccessRates = new();
            using (var logsContext = new LogsContext())
            {
                ProcessCandidates = logsContext.WindowTitles.OrderBy(wt => wt.ProcessName).ToList();
                
            }
            ProcessList.DataStore = ProcessCandidates.Select(e => e.ProcessName).ToList();
            ProcessList.SelectedKey = "explorer";
            ProcessList.AutoComplete = true;
            ProcessList.Width = 200;
                
            List<CpuUsageByHour> cpuUsageByHour = new List<CpuUsageByHour>();   
            List<MemoryUsageByHour> memoryUseByHour = new List<MemoryUsageByHour>();
            List<MemoryUsageByHour> memoryUseByHourPeak = new List<MemoryUsageByHour>();
            using (var logsContext = new LogsContext())
            {
                    
                var GroupedByHour = logsContext.StatsHourlies.Where(x => x.ProcessName == ProcessName).ToList();
                cpuUsageByHour = GroupedByHour.Select(e => new CpuUsageByHour { Hour = e.Hour, CpuTimeDiff = double.Parse(Encoding.UTF8.GetString(e.CpuPercent ?? [((byte)'0')])) }).ToList();
                memoryUseByHour = GroupedByHour.Select(e => new MemoryUsageByHour { Hour = e.Hour, WorkingSet = e.AvgWorkingSet }).ToList();
                memoryUseByHourPeak = GroupedByHour.Select(e => new MemoryUsageByHour { Hour = e.Hour, WorkingSet = double.Parse(e.MaxWorkingSetForOneInstance) }).ToList();
            }
            PlotData.Add("CPU %", cpuUsageByHour);
            PlotDataSuccessRates.Add("Mem [avg]", memoryUseByHour);
            PlotDataSuccessRates.Add("Mem [peak]", memoryUseByHourPeak);

            etoPlot.Plot.XAxis.DateTimeFormat(true);
            etoPlotSuccessRates.Plot.XAxis.DateTimeFormat(true);
            var pCpu = etoPlot.Plot.AddScatter(PlotData["CPU %"].Select(e => (DateTime.Parse(e.Hour+":00").ToLocalTime().ToOADate())).ToArray(), PlotData["CPU %"].Select(e => e.CpuTimeDiff??0).ToArray(), label: "CPU %");
            var pRamAvg = etoPlotSuccessRates.Plot.AddScatter(PlotDataSuccessRates["Mem [avg]"].Select(e => (DateTime.Parse(e.Hour + ":00").ToLocalTime().ToOADate())).ToArray(), PlotDataSuccessRates["Mem [avg]"].Select(e => e.WorkingSet ?? 0).ToArray(), label: "Mem [avg]");
            var pRamMax = etoPlotSuccessRates.Plot.AddScatter(PlotDataSuccessRates["Mem [peak]"].Select(e => (DateTime.Parse(e.Hour + ":00").ToLocalTime().ToOADate())).ToArray(), PlotDataSuccessRates["Mem [peak]"].Select(e => e.WorkingSet ?? 0).ToArray(), label: "Mem [peak, highest process]");
            pCpu.Label = "CPU %";
            pCpu.MarkerSize = 6;
            pCpu.MarkerLineWidth = 3;
            pRamMax.MarkerSize = 8;
            pRamMax.MarkerLineWidth = 4;
            pRamAvg.MarkerSize = 8;
            pRamAvg.MarkerLineWidth = 4;

            etoPlot.Plot.Legend();
            etoPlot.Plot.Legend().FontName = "Courier";
            etoPlot.Plot.Legend().Location = Alignment.UpperLeft;
            etoPlot.Plot.Style(dataBackground: System.Drawing.Color.Transparent);
            etoPlot.Plot.Title("CPU usage");
            etoPlot.Plot.XLabel("Date/Time");
            etoPlot.Plot.YLabel("CPU-Hours %");
            etoPlot.Refresh();

            etoPlotSuccessRates.Plot.Legend();
            etoPlotSuccessRates.Plot.Legend().FontName = "Courier";
            etoPlotSuccessRates.Plot.Legend().Location = Alignment.UpperLeft;
            etoPlotSuccessRates.Plot.Style(dataBackground: System.Drawing.Color.Transparent);
            etoPlotSuccessRates.Plot.Title("Mem stats");
            etoPlotSuccessRates.Plot.XLabel("Date/Time");
            etoPlotSuccessRates.Plot.YLabel("Memory");
            etoPlotSuccessRates.Refresh();

            FilterText.TextInput += (e, a) => {
                switch (RadioProcessFilter.SelectedKey) {
                    case "Any":
                        ProcessList.DataContext = ProcessCandidates.Where(x => x.ProcessName.Contains(FilterText.Text) || x.WindowName.Contains(FilterText.Text)).Select(e => e.ProcessName).ToList();
                        break;
                    case "Process name":
                        ProcessList.DataContext = ProcessCandidates.Where(x => x.ProcessName.Contains(FilterText.Text)).Select(e => e.ProcessName).ToList();
                        break;
                    case "Window title":
                        ProcessList.DataContext = ProcessCandidates.Where(x => x.WindowName.Contains(FilterText.Text)).Select(e => e.ProcessName).ToList();
                        break;
                }
                MessageBox.Show(RadioProcessFilter.SelectedKey);
                ProcessList.IsDataContextChanging = true;
            };


            var VerticalStackLayout = new StackLayout() { 
                Items = { 
                    ProcessSelectorAndFilter,
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
