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
using System.Dynamic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata.Ecma335;

namespace HealthMonitor
{

    public class MainModuleProcessStatsFormHourly: Form
    {
        public MainModuleProcessStatsFormHourly(string MainModuleProcessName) {
            Title = $"HealthMonitor Process Plots [{MainModuleProcessName}]: by Hour";
            Location = new Eto.Drawing.Point(50,50);
            ScottPlot.Eto.PlotView etoPlotCpu = new() { Size = new Eto.Drawing.Size(1080, 300) };
            ScottPlot.Eto.PlotView etoPlotMem = new() { Size = new Eto.Drawing.Size(1080, 300) };
            etoPlotCpu.Plot.XAxis.LabelStyle(fontSize: 18);
            etoPlotCpu.Plot.YAxis.LabelStyle(fontSize: 18);
            etoPlotCpu.Plot.Legend().FontSize = 10;
            etoPlotMem.Plot.XAxis.LabelStyle(fontSize: 18);
            etoPlotMem.Plot.YAxis.LabelStyle(fontSize: 18);
            etoPlotMem.Plot.Legend().FontSize = 10;

            var SaveButton = new Button() { Text = "💾 Save As ..." };
            SaveButton.Click += (e, a) =>
            {
                try
                {
                    var SaveDialogCPU = new SaveFileDialog();
                    SaveDialogCPU.Title = "Save stats as (please add PNG extension yourself)...";
                    SaveDialogCPU.Filters.Add(Config.PNGFilter);
                    SaveDialogCPU.ShowDialog("");
                    var PathCPUStats = SaveDialogCPU.FileName;

                    etoPlotCpu.Plot.SaveFig(PathCPUStats, 2560, 1440, false, 4);
                    MessageBox.Show($"Saved as: {PathCPUStats}", "CPU stats saved", MessageBoxType.Information);

                    var SaveDialogRAM = new SaveFileDialog();
                    SaveDialogRAM.Title = "Save success stats as (please add PNG extension yourself)...";
                    SaveDialogRAM.Filters.Add(Config.PNGFilter);
                    SaveDialogRAM.ShowDialog("");
                    var PathRAMStats = SaveDialogRAM.FileName;

                    etoPlotMem.Plot.SaveFig(PathRAMStats, 2560, 1440, false, 4);
                    MessageBox.Show($"Saved as: {PathRAMStats}", "RAM stats saved", MessageBoxType.Information);
                }
                catch (System.Exception E)
                {
                    MessageBox.Show(E.ToString(), MessageBoxType.Error);
                }
            };

            var ReloadButton = new Button() { Text = "⬇⬇ Reload" };
            ReloadButton.Click += (e, a) => {
                MessageBox.Show("Not implemented!", "😟😟 Not implemented!", MessageBoxType.Warning);
            };

            var ResetButton = new Button() { Text = "🔄 Reset" };
            ResetButton.Click += (e, a) => { 
                etoPlotCpu.Plot.AxisAuto(); 
                etoPlotCpu.Refresh();
                etoPlotMem.Plot.AxisAuto();
                etoPlotMem.Refresh();
            };

            var FilterCriteria = new List<string>(){ "Any", "Process name/path", "Window title" };
            var RadioProcessFilter = new RadioButtonList() { DataStore = FilterCriteria, Orientation = Eto.Forms.Orientation.Vertical, SelectedValue = FilterCriteria[0], };
            var FilterText = new TextBox();
            FilterText.PlaceholderText = MainModuleProcessName;
            
            var FilterTextStack = new StackLayout() { 
                Items = { null, new Label() { Text = "Filter text:" }, new Label() { Text = "[ESC] to cancel all" }, FilterText },
                Orientation = Eto.Forms.Orientation.Vertical,
                Spacing = 5,
                Size = new Eto.Drawing.Size(-1, -1),
                HorizontalContentAlignment = Eto.Forms.HorizontalAlignment.Stretch,
                VerticalContentAlignment = Eto.Forms.VerticalAlignment.Bottom,
            };
            var ProcessList = new ComboBox();
            ProcessList.ToolTip = "Choose one and press [Enter]";
            var ProcessSelectorPanel = new StackLayout()
            {

            };
            var GridMatchedProcessNames = new GridView() { Size = new Eto.Drawing.Size(400, 90), GridLines = GridLines.Both, ShowHeader = true, ToolTip = "[DEL] to kill" };
            GridMatchedProcessNames.CellFormatting += (a, b) => {
                b.Font = new Eto.Drawing.Font("Courier New", 7, Eto.Drawing.FontStyle.Bold);
                if (b.Row == GridMatchedProcessNames.SelectedRow)
                {
                    b.BackgroundColor = Eto.Drawing.Color.FromArgb(50,50,50,255);
                    b.ForegroundColor = Eto.Drawing.Color.FromArgb(255, 255, 255, 255);
                }
                else if (b.Row % 2 == 0) 
                { 
                    b.BackgroundColor = Eto.Drawing.Color.FromArgb(255, 255, 200);
                } 
                else if (b.Column.DisplayIndex % 2 == 0)
                {
                    b.BackgroundColor = Eto.Drawing.Color.FromArgb(255, 200, 255);
                }
                else
                {
                    b.BackgroundColor = Eto.Drawing.Color.FromArgb(240, 240, 240);
                }
            };
            GridMatchedProcessNames.Columns.Add(new GridColumn { HeaderText = "Process Name/Path", DataCell = new TextBoxCell(0), Resizable = true, AutoSize = true });
            GridMatchedProcessNames.Columns.Add(new GridColumn { HeaderText = "Window Title", DataCell = new TextBoxCell(1), Resizable = true, AutoSize = true });
            GridMatchedProcessNames.SelectionChanged += (e, a) => GridMatchedProcessNames.Invalidate();
            GridMatchedProcessNames.KeyUp += (e, a) =>
            {
                var SelectedProcessName = (string)((string[])(GridMatchedProcessNames.DataStore.ElementAt(GridMatchedProcessNames.SelectedRow))).ElementAt(0);
                if (a.Key == Keys.Delete)
                {
                    var CandidateList = System.Diagnostics.Process.GetProcesses().Where(e => { try { return e.MainModule.FileName == SelectedProcessName; } catch (System.Exception) { return false; };  }).ToList();
                    var Choice = MessageBox.Show($"Do you want to kill {CandidateList.Count} processes named {SelectedProcessName}?", MessageBoxButtons.YesNo, MessageBoxType.Warning);
                    if (Choice == DialogResult.Yes)
                    {
                        try
                        {
                            foreach (var item in CandidateList)
                            {
                                try
                                {
                                    item.Kill();
                                }
                                catch(System.Exception E)
                                {
                                    MessageBox.Show(E.ToString(), MessageBoxType.Error);
                                }
                            }
                        }
                        catch(System.Exception E)
                        {
                            MessageBox.Show(E.ToString(), MessageBoxType.Error);
                        }
                    }
                }
                
            };
            GridMatchedProcessNames.CellDoubleClick += (e, a) => {
                var SelectedProcessName = (string)((string[])(GridMatchedProcessNames.DataStore.ElementAt(a.Row))).ElementAt(0);
                var result = MessageBox.Show(
                    $"View stats for: {SelectedProcessName}?", 
                    "View stats (confirm)",
                    MessageBoxButtons.YesNo, 
                    MessageBoxType.Information
                    );
                if (result == DialogResult.Yes) (new MainModuleProcessStatsFormHourly(SelectedProcessName)).Show();
            };
            GridMatchedProcessNames.KeyDown += (e, a) => {
                if (a.Key == Keys.Enter)
                {
                    var SelectedProcessName = (string)((string[])(GridMatchedProcessNames.DataStore.ElementAt(GridMatchedProcessNames.SelectedRow))).ElementAt(0);
                    var result = MessageBox.Show(
                        $"View stats for: {SelectedProcessName}?",
                        "View stats (confirm)",
                        MessageBoxButtons.YesNo,
                        MessageBoxType.Information
                        );
                    if (result == DialogResult.Yes) (new MainModuleProcessStatsFormHourly(SelectedProcessName)).Show();
                }
            };
            var SortByRAM = new Button() { Text = "Sort by RAM 💾" };
            SortByRAM.Click += (e, a) => {
                List<string[]> SortedByRAM = new List<string[]>();
                using (var ctx = new LogsContext())
                {
                    var ListedByRAM = ctx.MaxAvgWorkingSets.ToList();
                    SortedByRAM = ListedByRAM.Select(e => new [] { e.ProcessName, e.AvgWorkingSetValue.GetValueOrDefault(0.0).ToString("N0"), e.MaxWorkingSetValue.GetValueOrDefault().ToString("N0") }).ToList();
                    
                }
                (new ListerGridView(new List<string> { "Process Name", "Avg", "Max" }, SortedByRAM)).ShowModal();
            };
            var SortByCPU = new Button() { Text = "Sort by CPU [last rec hour] ⌛" };
            SortByCPU.Click += (e, a) => {
                try
                {
                    List<string[]> SortedByCPU = new List<string[]>();
                    using (var ctx = new LogsContext())
                    {
                        var lastAvailableHour = ctx.StatsHourlyMainModulePaths.Max(e => e.Hour);
                        var lastAvailableHourDT = DateTime.Parse(lastAvailableHour + ":00");
                        MessageBox.Show($"From hour: {lastAvailableHourDT.ToString("o").Substring(0, 13)}", "Time info", MessageBoxType.Information);
                        var curHour = lastAvailableHourDT.ToString("o").Substring(0, 13);
                        var ListedByCPU = ctx.StatsHourlyMainModulePaths.Where(e => e.Hour == curHour).ToList();
                        SortedByCPU = ListedByCPU.OrderByDescending(e => e.CpuPercent).Select(e => new[] { e.MainModulePath, (e.CpuPercent.GetValueOrDefault()).ToString(), e.ThreadCount.GetValueOrDefault(0.0).ToString("N0") }).ToList();

                    }
                (new ListerGridView(new List<string> { "Process Name/Path", "CPU %", "TC" }, SortedByCPU)).ShowModal();
                }
                catch(System.Exception E)
                {
                    MessageBox.Show(E.ToString(), MessageBoxType.Error);
                }
            };
            var SortByCPULH = new Button() { Text = "Sort by CPU [last rec - 1 hour] ⌛" };
            SortByCPULH.Click += (e, a) => {
                try
                {
                    List<string[]> SortedByCPULH = new List<string[]>();
                    using (var ctx = new LogsContext())
                    {
                        var lastAvailableHour = ctx.StatsHourlyMainModulePaths.Max(e => e.Hour);
                        var lastAvailableHourDT = DateTime.Parse(lastAvailableHour + ":00");
                        var lastHour = lastAvailableHourDT.AddHours(-1).ToString("o").Substring(0, 13);
                        MessageBox.Show($"From hour: {lastHour}", "Time info", MessageBoxType.Information);
                        var ListedByCPU = ctx.StatsHourlyMainModulePaths.Where(e => e.Hour == lastHour).ToList();
                        SortedByCPULH = ListedByCPU.OrderByDescending(e => e.CpuPercent).Select(e => new [] { e.MainModulePath, (e.CpuPercent.GetValueOrDefault()).ToString(), e.ThreadCount.GetValueOrDefault(0.0).ToString("N0") }).ToList();

                    }
                (new ListerGridView(new List<string> { "Process Name/Path", "CPU %", "TC" }, SortedByCPULH)).ShowModal();
                }
                catch(System.Exception E)
                {
                    MessageBox.Show(E.ToString(), MessageBoxType.Error);
                }
            };
            var SorterButtons = new StackLayout()
            {
                Items = { SortByRAM, SortByCPU, SortByCPULH },
                Orientation = Eto.Forms.Orientation.Vertical,
                Spacing = 5,
                HorizontalContentAlignment = Eto.Forms.HorizontalAlignment.Stretch,
                VerticalContentAlignment = Eto.Forms.VerticalAlignment.Center,
            };
            var ProcessSelectorAndFilter = new StackLayout()
            {
                Items = { null, FilterTextStack, ProcessList, RadioProcessFilter, GridMatchedProcessNames, SorterButtons, null },
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

            
            List<WindowTitlesMainModule> ProcessCandidates;
            List<String> Decaminutes = new List<string>();
            Dictionary<String, List < CpuUsageByHour >> PlotData = new();
            Dictionary<String, List<MemoryUsageByHour>> PlotDataSuccessRates = new();
            using (var logsContext = new LogsContext())
            {
                ProcessCandidates = logsContext.WindowTitlesMainModules.OrderBy(wt => wt.MainModulePath).ToList();
                
            }
            ProcessList.DataStore = ProcessCandidates.Select(e => e.MainModulePath).ToList();
            ProcessList.SelectedKey = MainModuleProcessName;
            ProcessList.Width = 200;
            ProcessList.AutoComplete = true;
            ProcessList.TextInput += (e, a) => {
                ProcessList.DataStore = ProcessCandidates.Where(x => x.MainModulePath.Contains(ProcessList.Text)).Select(e => e.MainModulePath).ToList(); 
                ProcessList.IsDataContextChanging = true; 
            };
            ProcessList.KeyUp += (e, a) =>
            {
                if(a.Key == Keys.Escape)
                {
                    ProcessList.SelectedValue = false;
                    ProcessList.Invalidate();
                }
            };

            if(ProcessCandidates.Where(x=>x.MainModulePath == MainModuleProcessName).ToList().Count == 0)
            {
                MainModuleProcessName = "C:\\Windows\\explorer.exe";
            }
                
            List<CpuUsageByHour> cpuUsageByHour = new List<CpuUsageByHour>();   
            List<MemoryUsageByHour> memoryUseByHour = new List<MemoryUsageByHour>();
            List<MemoryUsageByHour> memoryUseByHourPeak = new List<MemoryUsageByHour>();
            try
            {
                using (var logsContext = new LogsContext())
                {

                    var GroupedByHour = logsContext.StatsHourlyMainModulePaths.Where(x => x.MainModulePath.ToLower() == MainModuleProcessName.ToLower()).ToList();
                    cpuUsageByHour = GroupedByHour.Select(e => new CpuUsageByHour { Hour = e.Hour, CpuTimeDiff = (e.CpuPercent.GetValueOrDefault()) }).ToList();
                    memoryUseByHour = GroupedByHour.Select(e => new MemoryUsageByHour { Hour = e.Hour, WorkingSet = e.AvgWorkingSet }).ToList();
                    memoryUseByHourPeak = GroupedByHour.Select(e => new MemoryUsageByHour { Hour = e.Hour, WorkingSet = double.Parse(e.MaxWorkingSetForOneInstance) }).ToList();
                }
            }
            catch(System.Exception E)
            {
                MessageBox.Show(E.ToString(), MessageBoxType.Error);
            }
            PlotData.Add("CPU %", cpuUsageByHour);
            PlotDataSuccessRates.Add("Mem [avg]", memoryUseByHour);
            PlotDataSuccessRates.Add("Mem [peak]", memoryUseByHourPeak);

            etoPlotCpu.Plot.XAxis.DateTimeFormat(true);
            etoPlotMem.Plot.XAxis.DateTimeFormat(true);
            var pCpu = etoPlotCpu.Plot.AddScatter(PlotData["CPU %"].Select(e => (DateTime.Parse(e.Hour+":00").ToLocalTime().ToOADate())).ToArray(), PlotData["CPU %"].Select(e => e.CpuTimeDiff??0).ToArray(), label: "CPU %");
            var pRamAvg = etoPlotMem.Plot.AddScatter(PlotDataSuccessRates["Mem [avg]"].Select(e => (DateTime.Parse(e.Hour + ":00").ToLocalTime().ToOADate())).ToArray(), PlotDataSuccessRates["Mem [avg]"].Select(e => (e.WorkingSet ?? 0) / (1024*1024)).ToArray(), label: "Mem [avg]");
            var pRamMax = etoPlotMem.Plot.AddScatter(PlotDataSuccessRates["Mem [peak]"].Select(e => (DateTime.Parse(e.Hour + ":00").ToLocalTime().ToOADate())).ToArray(), PlotDataSuccessRates["Mem [peak]"].Select(e => (e.WorkingSet ?? 0)/(1024*1024)).ToArray(), label: "Mem [peak, highest process]");
            pCpu.Label = "CPU %";
            pCpu.MarkerSize = 6;
            pCpu.MarkerLineWidth = 3;
            pRamMax.MarkerSize = 8;
            pRamMax.MarkerLineWidth = 4;
            pRamAvg.MarkerSize = 8;
            pRamAvg.MarkerLineWidth = 4;

            try
            {
                etoPlotCpu.Plot.SetAxisLimits(yMin: 0);
                etoPlotCpu.Plot.Legend();
                etoPlotCpu.Plot.Legend().FontName = "Courier";
                etoPlotCpu.Plot.Legend().Location = Alignment.UpperLeft;
                etoPlotCpu.Plot.Style(dataBackground: System.Drawing.Color.Transparent);
                etoPlotCpu.Plot.Title($"CPU usage [{MainModuleProcessName}]");
                etoPlotCpu.Plot.XLabel("Date/Time");
                etoPlotCpu.Plot.YLabel("CPU-Hours %");
                etoPlotCpu.Refresh();

                etoPlotMem.Plot.SetAxisLimits(yMin: 0);
                etoPlotMem.Plot.Legend();
                etoPlotMem.Plot.Legend().FontName = "Courier";
                etoPlotMem.Plot.Legend().Location = Alignment.UpperLeft;
                etoPlotMem.Plot.Style(dataBackground: System.Drawing.Color.Transparent);
                etoPlotMem.Plot.Title($"Mem stats [{MainModuleProcessName}]");
                etoPlotMem.Plot.XLabel("Date/Time");
                etoPlotMem.Plot.YLabel("Memory (MiB)");
                etoPlotMem.Refresh();
            }
            catch (System.Exception E)
            {
                MessageBox.Show($"{E.ToString()} \r\n {E.Message} \r\n {E.StackTrace}", "Exception");
            }

            FilterText.TextInput += Filter;
            RadioProcessFilter.SelectedKeyChanged += Filter;

            ProcessList.KeyDown += (e, a) => { 
                if(a.Key == Keys.Enter && ProcessList.SelectedKey != null && ProcessList.SelectedKey.Length > 1) 
                    (new ProcessStatsFormHourly(ProcessList.SelectedKey)).Show();
                else if (a.Key == Keys.Escape)
                {
                    ProcessList.DataStore = ProcessCandidates.Select(e => e.MainModulePath).ToList();
                    ProcessList.IsDataContextChanging = true;
                }
            };

            void Filter(Object e, EventArgs a) {
                var LowerCaseFilterText = FilterText.Text.ToLowerInvariant();
                switch (RadioProcessFilter.SelectedKey)
                {
                    case "Any":
                        ProcessList.DataStore = ProcessCandidates.AsParallel().Where(x => x.MainModulePath.ToLowerInvariant().Contains(LowerCaseFilterText) || x.WindowName.ToLowerInvariant().Contains(LowerCaseFilterText)).Select(e => e.MainModulePath).AsSequential().ToList();
                        GridMatchedProcessNames.DataStore = ProcessCandidates.AsParallel().Where(x => x.MainModulePath.ToLowerInvariant().Contains(LowerCaseFilterText) || x.WindowName.ToLowerInvariant().Contains(LowerCaseFilterText)).Select(e => new string[] { e.MainModulePath, e.WindowName }).AsSequential().ToList();
                        break;
                    case "Process path/name":
                        ProcessList.DataStore = ProcessCandidates.Where(x => x.MainModulePath.ToLowerInvariant().Contains(FilterText.Text)).Select(e => e.MainModulePath).ToList();
                        GridMatchedProcessNames.DataStore = ProcessCandidates.Where(x => x.MainModulePath.ToLowerInvariant().Contains(FilterText.Text)).Select(e => new string[] { e.MainModulePath, e.WindowName }). ToList();
                        break;
                    case "Window title":
                        ProcessList.DataStore = ProcessCandidates.Where(x => x.WindowName.ToLowerInvariant().Contains(FilterText.Text.ToLowerInvariant())).Select(e => e.MainModulePath).ToList();
                        GridMatchedProcessNames.DataStore = ProcessCandidates.Where(x => x.WindowName.ToLowerInvariant().Contains(FilterText.Text.ToLowerInvariant())).Select(e => new string[] { e.MainModulePath, e.WindowName }).ToList();
                        break;
                }
                ProcessList.IsDataContextChanging = true;
                GridMatchedProcessNames.Invalidate();
            }

            

            var VerticalStackLayout = new StackLayout() { 
                Items = { 
                    ProcessSelectorAndFilter,
                    TopStackLayout, 
                    etoPlotCpu, 
                    etoPlotMem 
                }, 
                Orientation = Eto.Forms.Orientation.Vertical, 
                Spacing = 20 
            };
            Content = VerticalStackLayout;
            Resizable = false;
            Filter(null, null);
        }
    }

   
}
