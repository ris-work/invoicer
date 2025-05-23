﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
//using ScottPlot;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using ABI.System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using HealthMonitorLogViewer;
using Microsoft.EntityFrameworkCore;
using ScottPlot;
using ScottPlot.Eto;
using ScottPlot.Rendering;

namespace HealthMonitor
{
    public class ProcessStatsFormHourlyPanel : Panel
    {
        public ProcessStatsFormHourlyPanel()
            : this(null) { }

        public ProcessStatsFormHourlyPanel(string ProcessName)
        {
            //Title = $"HealthMonitor Process Plots [{ProcessName}]: by Hour [{Config.LogFile}]";
            //Location = new Eto.Drawing.Point(50,50);
            ScottPlot.Eto.EtoPlot etoPlotCpu = new() { Size = new Eto.Drawing.Size(1080, 300) };
            ScottPlot.Eto.EtoPlot etoPlotMem = new() { Size = new Eto.Drawing.Size(1080, 300) };
            etoPlotCpu.Plot.Axes.Bottom.Label.FontSize = 18;
            etoPlotCpu.Plot.Axes.Left.Label.FontSize = 18;
            etoPlotCpu.Plot.Legend.FontSize = 10;
            etoPlotMem.Plot.Axes.Bottom.Label.FontSize = 18;
            etoPlotMem.Plot.Axes.Left.Label.FontSize = 18;
            etoPlotMem.Plot.Legend.FontSize = 10;
            //MovableByWindowBackground = true;

            var SaveButton = new Button() { Text = "💾 Save As ..." };
            SaveButton.Click += (e, a) =>
            {
                try
                {
                    var SaveDialogCPU = new SaveFileDialog();
                    SaveDialogCPU.Title = "Save stats as (please add PNG extension yourself)...";
                    SaveDialogCPU.Filters.Add(Config.PNGFilter);
                    SaveDialogCPU.Filters.Add(Config.SVGFilter);
                    SaveDialogCPU.ShowDialog("");
                    var PathCPUStats = SaveDialogCPU.FileName;

                    if (SaveDialogCPU.CurrentFilterIndex == 0)
                    {
                        etoPlotCpu.Plot.Save(PathCPUStats, 2560, 1440, quality: 100);
                    }
                    else
                    {
                        etoPlotCpu.Plot.SaveSvg(PathCPUStats, 2560, 1440);
                    }

                    MessageBox.Show(
                        $"Saved as: {PathCPUStats}",
                        "CPU stats saved",
                        MessageBoxType.Information
                    );

                    var SaveDialogRAM = new SaveFileDialog();
                    SaveDialogRAM.Title =
                        "Save success stats as (please add PNG extension yourself)...";
                    SaveDialogRAM.Filters.Add(Config.PNGFilter);
                    SaveDialogRAM.Filters.Add(Config.SVGFilter);
                    SaveDialogRAM.ShowDialog("");
                    var PathRAMStats = SaveDialogRAM.FileName;

                    if (SaveDialogRAM.CurrentFilterIndex == 0)
                    {
                        etoPlotMem.Plot.Save(PathRAMStats, 2560, 1440, quality: 100);
                    }
                    else
                    {
                        etoPlotMem.Plot.SaveSvg(PathRAMStats, 2560, 1440);
                    }
                    MessageBox.Show(
                        $"Saved as: {PathRAMStats}",
                        "RAM stats saved",
                        MessageBoxType.Information
                    );
                }
                catch (System.Exception E)
                {
                    MessageBox.Show(E.ToString(), MessageBoxType.Error);
                }
            };

            var ReloadButton = new Button() { Text = "⬇⬇ Reload" };
            ReloadButton.Click += (e, a) =>
            {
                MessageBox.Show(
                    "Not implemented!",
                    "😟😟 Not implemented!",
                    MessageBoxType.Warning
                );
            };

            var ResetButton = new Button() { Text = "🔄 Reset" };
            ResetButton.Click += (e, a) =>
            {
                etoPlotCpu.Plot.Axes.AutoScale();
                etoPlotCpu.Refresh();
                etoPlotMem.Plot.Axes.AutoScale();
                etoPlotMem.Refresh();
            };

            var FilterCriteria = new List<string>() { "Any", "Process name", "Window title" };
            var RadioProcessFilter = new RadioButtonList()
            {
                DataStore = FilterCriteria,
                Orientation = Eto.Forms.Orientation.Vertical,
                SelectedValue = FilterCriteria[0],
                BackgroundColor = Eto.Drawing.Colors.Black,
                TextColor = Eto.Drawing.Colors.White,
            };
            var FilterText = new TextBox()
            {
                BackgroundColor = Eto.Drawing.Colors.Black,
                TextColor = Eto.Drawing.Colors.White,
            };
            FilterText.PlaceholderText = ProcessName;

            var FilterTextStack = new StackLayout()
            {
                Items =
                {
                    null,
                    new Eto.Forms.Label()
                    {
                        Text = "Filter text:",
                        TextColor = Eto.Drawing.Colors.White,
                    },
                    new Eto.Forms.Label()
                    {
                        Text = "[ESC] to cancel all",
                        TextColor = Eto.Drawing.Colors.White,
                    },
                    FilterText,
                },
                Orientation = Eto.Forms.Orientation.Vertical,
                Spacing = 5,
                Size = new Eto.Drawing.Size(-1, -1),
                HorizontalContentAlignment = Eto.Forms.HorizontalAlignment.Stretch,
                VerticalContentAlignment = Eto.Forms.VerticalAlignment.Bottom,
            };
            var ProcessList = new ComboBox();
            ProcessList.BackgroundColor = Eto.Drawing.Colors.Black;
            ProcessList.TextColor = Eto.Drawing.Colors.White;
            ProcessList.ToolTip = "Choose one and press [Enter]";
            ProcessList.GotFocus += (e, a) =>
            {
                ProcessList.LaunchDropDown();
            };
            var ProcessSelectorPanel = new StackLayout() { };
            var GridMatchedProcessNames = new GridView()
            {
                Size = new Eto.Drawing.Size(400, 90),
                GridLines = GridLines.Both,
                ShowHeader = true,
                ToolTip = "[DEL] to kill, [SPACE] to see count",
            };
            GridMatchedProcessNames.CellFormatting += (a, b) =>
            {
                b.Font = new Eto.Drawing.Font("Courier New", 7, Eto.Drawing.FontStyle.Bold);
                if (b.Row == GridMatchedProcessNames.SelectedRow)
                {
                    b.BackgroundColor = Eto.Drawing.Color.FromArgb(50, 50, 50, 255);
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
            GridMatchedProcessNames.Columns.Add(
                new GridColumn
                {
                    HeaderText = "Process Name",
                    DataCell = new TextBoxCell(0),
                    Resizable = true,
                    AutoSize = true,
                }
            );
            GridMatchedProcessNames.Columns.Add(
                new GridColumn
                {
                    HeaderText = "Window Title",
                    DataCell = new TextBoxCell(1),
                    Resizable = true,
                    AutoSize = true,
                }
            );
            GridMatchedProcessNames.SelectionChanged += (e, a) =>
                GridMatchedProcessNames.Invalidate();
            GridMatchedProcessNames.KeyUp += (e, a) =>
            {
                try
                {
                    var SelectedRow = GridMatchedProcessNames.SelectedRow;
                    Object SelectedProcess;
                    if (SelectedRow == -1)
                    {
                        SelectedProcess = GridMatchedProcessNames.DataStore.ElementAt(0);
                    }
                    else
                    {
                        SelectedProcess = GridMatchedProcessNames.DataStore.ElementAt(SelectedRow);
                    }
                    string SelectedProcessName;
                    SelectedProcessName = ((string[])(SelectedProcess))[0];
                    if (a.Key == Keys.Delete)
                    {
                        var CandidateList = System
                            .Diagnostics.Process.GetProcesses()
                            .Where(e => e.ProcessName == SelectedProcessName)
                            .ToList();
                        var Choice = MessageBox.Show(
                            $"Do you want to kill {CandidateList.Count} processes named {SelectedProcessName}?",
                            MessageBoxButtons.YesNo,
                            MessageBoxType.Warning
                        );
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
                                    catch (System.Exception E)
                                    {
                                        MessageBox.Show(E.ToString(), MessageBoxType.Error);
                                    }
                                }
                            }
                            catch (System.Exception E)
                            {
                                MessageBox.Show(E.ToString(), MessageBoxType.Error);
                            }
                        }
                    }
                }
                catch (System.Exception E) { }
            };
            GridMatchedProcessNames.CellDoubleClick += (e, a) =>
            {
                try
                {
                    var SelectedProcessName = (string)
                        ((string[])(GridMatchedProcessNames.DataStore.ElementAt(a.Row))).ElementAt(
                            0
                        );
                    var result = MessageBox.Show(
                        $"View stats for: {SelectedProcessName}?",
                        "View stats (confirm)",
                        MessageBoxButtons.YesNo,
                        MessageBoxType.Information
                    );
                    if (result == DialogResult.Yes)
                        (
                            new SinglePanelFlyout(
                                new ProcessStatsFormHourlyPanel(SelectedProcessName),
                                $"Stats: {SelectedProcessName}"
                            )
                        ).Show();
                }
                catch (System.Exception E) { }
            };
            GridMatchedProcessNames.KeyDown += (e, a) =>
            {
                try
                {
                    if (a.Key == Keys.Enter)
                    {
                        var SelectedRow = GridMatchedProcessNames.SelectedRow;
                        Object SelectedProcess;
                        if (SelectedRow == -1)
                        {
                            SelectedProcess = GridMatchedProcessNames.DataStore.ElementAt(0);
                        }
                        else
                        {
                            SelectedProcess = GridMatchedProcessNames.DataStore.ElementAt(
                                SelectedRow
                            );
                        }
                        string SelectedProcessName;
                        SelectedProcessName = ((string[])(SelectedProcess))[0];
                        var result = MessageBox.Show(
                            $"View stats for: {SelectedProcessName}?",
                            "View stats (confirm)",
                            MessageBoxButtons.YesNo,
                            MessageBoxType.Information
                        );
                        if (result == DialogResult.Yes)
                            (
                                new SinglePanelFlyout(
                                    new ProcessStatsFormHourlyPanel(SelectedProcessName),
                                    $"Stats: {SelectedProcessName}"
                                )
                            ).Show();
                    }
                    else if (a.Key == Keys.Space)
                    {
                        MessageBox.Show(
                            $"Count: {GridMatchedProcessNames.DataStore.Count()}",
                            "Count",
                            MessageBoxType.Information
                        );
                    }
                }
                catch { }
            };
            var SortByRAM = new Button() { Text = "Sort by RAM 💾" };
            SortByRAM.Click += (e, a) =>
            {
                List<string[]> SortedByRAM = new List<string[]>();
                using (var ctx = new LogsContext())
                {
                    var ListedByRAM = ctx.MaxAvgWorkingSets.ToList();
                    SortedByRAM = ListedByRAM
                        .Select(e =>
                            new[]
                            {
                                e.ProcessName,
                                e.AvgWorkingSetValue.GetValueOrDefault(0.0).ToString("N0"),
                                e.MaxWorkingSetValue.GetValueOrDefault().ToString("N0"),
                            }
                        )
                        .ToList();
                }
                (
                    new ListerGridView(
                        new List<string> { "Process Name", "Avg", "Max" },
                        SortedByRAM
                    )
                ).ShowModal();
            };
            var SortByCPU = new Button() { Text = "Sort by CPU [last rec hour] ⌛" };
            SortByCPU.Click += (e, a) =>
            {
                try
                {
                    List<string[]> SortedByCPU = new List<string[]>();
                    using (var ctx = new LogsContext())
                    {
                        var lastAvailableHour = ctx.StatsHourlies.Max(e => e.Hour);
                        var lastAvailableHourDT = DateTime.Parse(lastAvailableHour + ":00");
                        MessageBox.Show(
                            $"From hour: {lastAvailableHourDT.ToString("o").Substring(0, 13)}",
                            "Time info",
                            MessageBoxType.Information
                        );
                        var curHour = lastAvailableHourDT.ToString("o").Substring(0, 13);
                        var ListedByCPU = ctx.StatsHourlies.Where(e => e.Hour == curHour).ToList();
                        SortedByCPU = ListedByCPU
                            .OrderByDescending(e => e.CpuPercent)
                            .Select(e =>
                                new[]
                                {
                                    e.ProcessName,
                                    (e.CpuPercent.GetValueOrDefault()).ToString(),
                                    e.ThreadCount.GetValueOrDefault(0.0).ToString("N0"),
                                }
                            )
                            .ToList();
                    }
                    (
                        new ListerGridView(
                            new List<string> { "Process Name", "CPU %", "TC" },
                            SortedByCPU
                        )
                    ).ShowModal();
                }
                catch (System.Exception E)
                {
                    MessageBox.Show(E.ToString(), MessageBoxType.Error);
                }
            };
            var SortByCPULH = new Button() { Text = "Sort by CPU [last rec - 1 hour] ⌛" };
            SortByCPULH.Click += (e, a) =>
            {
                try
                {
                    List<string[]> SortedByCPULH = new List<string[]>();
                    using (var ctx = new LogsContext())
                    {
                        var lastAvailableHour = ctx.StatsHourlies.Max(e => e.Hour);
                        var lastAvailableHourDT = DateTime.Parse(lastAvailableHour + ":00");
                        var lastHour = lastAvailableHourDT
                            .AddHours(-1)
                            .ToString("o")
                            .Substring(0, 13);
                        MessageBox.Show(
                            $"From hour: {lastHour}",
                            "Time info",
                            MessageBoxType.Information
                        );
                        var ListedByCPU = ctx.StatsHourlies.Where(e => e.Hour == lastHour).ToList();
                        SortedByCPULH = ListedByCPU
                            .OrderByDescending(e => e.CpuPercent)
                            .Select(e =>
                                new[]
                                {
                                    e.ProcessName,
                                    (e.CpuPercent.GetValueOrDefault()).ToString(),
                                    e.ThreadCount.GetValueOrDefault(0.0).ToString("N0"),
                                }
                            )
                            .ToList();
                    }
                    (
                        new ListerGridView(
                            new List<string> { "Process Name", "CPU %", "TC" },
                            SortedByCPULH
                        )
                    ).ShowModal();
                }
                catch (System.Exception E)
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
                Items =
                {
                    null,
                    FilterTextStack,
                    ProcessList,
                    RadioProcessFilter,
                    GridMatchedProcessNames,
                    SorterButtons,
                    null,
                },
                Orientation = Eto.Forms.Orientation.Horizontal,
                Spacing = 20,
                Size = new Eto.Drawing.Size(-1, -1),
                HorizontalContentAlignment = Eto.Forms.HorizontalAlignment.Stretch,
                VerticalContentAlignment = Eto.Forms.VerticalAlignment.Bottom,
            };

            var TopStackLayout = new StackLayout()
            {
                Items = { null, ResetButton, ReloadButton, SaveButton, null },
                Orientation = Eto.Forms.Orientation.Horizontal,
                Spacing = 20,
                Size = new Eto.Drawing.Size(-1, -1),
            };

            List<WindowTitle> ProcessCandidates;
            List<String> Decaminutes = new List<string>();
            Dictionary<String, List<CpuUsageByHour>> PlotData = new();
            Dictionary<String, List<MemoryUsageByHour>> PlotDataSuccessRates = new();
            using (var logsContext = new LogsContext())
            {
                ProcessCandidates = logsContext.WindowTitles.OrderBy(wt => wt.ProcessName).ToList();
            }
            ProcessList.DataStore = ProcessCandidates.Select(e => e.ProcessName).ToList();
            ProcessList.SelectedKey = ProcessName;
            ProcessList.Width = 200;
            //ProcessList.AutoComplete = true;
            ProcessList.TextInput += (e, a) =>
            {
                ProcessList.DataStore = ProcessCandidates
                    .Where(x => x.ProcessName.Contains(ProcessList.Text))
                    .Select(e => e.ProcessName)
                    .ToList();
                ProcessList.IsDataContextChanging = true;
            };
            ProcessList.KeyUp += (e, a) =>
            {
                if (a.Key == Keys.Escape)
                {
                    ProcessList.SelectedValue = null;
                    ProcessList.Invalidate();
                    FilterText.Text = "";
                    RadioProcessFilter.SelectedIndex = 0;
                    Filter(e, a);
                }
            };
            FilterText.KeyUp += (e, a) =>
            {
                if (a.Key == Keys.Escape)
                {
                    ProcessList.SelectedValue = null;
                    ProcessList.Invalidate();
                    FilterText.Text = "";
                    RadioProcessFilter.SelectedIndex = 0;
                    Filter(e, a);
                }
                else if (a.Key == Keys.Enter)
                {
                    GridMatchedProcessNames.Focus();
                }
            };

            if (ProcessCandidates.Where(x => x.ProcessName == ProcessName).ToList().Count == 0)
            {
                ProcessName = "explorer";
            }

            List<CpuUsageByHour> cpuUsageByHour = new List<CpuUsageByHour>();
            List<MemoryUsageByHour> memoryUseByHour = new List<MemoryUsageByHour>();
            List<MemoryUsageByHour> memoryUseByHourPeak = new List<MemoryUsageByHour>();
            try
            {
                using (var logsContext = new LogsContext())
                {
                    var GroupedByHour = logsContext
                        .StatsHourlies.Where(x => x.ProcessName == ProcessName)
                        .ToList();
                    cpuUsageByHour = GroupedByHour
                        .Select(e => new CpuUsageByHour
                        {
                            Hour = e.Hour,
                            CpuTimeDiff = (e.CpuPercent.GetValueOrDefault()),
                        })
                        .ToList();
                    memoryUseByHour = GroupedByHour
                        .Select(e => new MemoryUsageByHour
                        {
                            Hour = e.Hour,
                            WorkingSet = e.AvgWorkingSet,
                        })
                        .ToList();
                    memoryUseByHourPeak = GroupedByHour
                        .Select(e => new MemoryUsageByHour
                        {
                            Hour = e.Hour,
                            WorkingSet = double.Parse(e.MaxWorkingSetForOneInstance),
                        })
                        .ToList();
                }
            }
            catch (System.Exception E)
            {
                MessageBox.Show(E.ToString(), MessageBoxType.Error);
            }
            PlotData.Add("CPU %", cpuUsageByHour);
            PlotDataSuccessRates.Add("Mem [avg]", memoryUseByHour);
            PlotDataSuccessRates.Add("Mem [peak]", memoryUseByHourPeak);

            etoPlotCpu.Plot.Axes.DateTimeTicksBottom();
            etoPlotMem.Plot.Axes.DateTimeTicksBottom();
            var pCpu = etoPlotCpu.Plot.Add.Scatter(
                PlotData["CPU %"]
                    .Select(e => (DateTime.Parse(e.Hour + ":00").ToLocalTime().ToOADate()))
                    .ToArray(),
                PlotData["CPU %"].Select(e => e.CpuTimeDiff ?? 0).ToArray()
            );
            pCpu.LegendText = "CPU [core %]";
            var pRamAvg = etoPlotMem.Plot.Add.Scatter(
                PlotDataSuccessRates["Mem [avg]"]
                    .Select(e => (DateTime.Parse(e.Hour + ":00").ToLocalTime().ToOADate()))
                    .ToArray(),
                PlotDataSuccessRates["Mem [avg]"]
                    .Select(e => (e.WorkingSet ?? 0) / (1024 * 1024))
                    .ToArray()
            );
            pRamAvg.LegendText = "Mem [avg]";
            var pRamMax = etoPlotMem.Plot.Add.Scatter(
                PlotDataSuccessRates["Mem [peak]"]
                    .Select(e => (DateTime.Parse(e.Hour + ":00").ToLocalTime().ToOADate()))
                    .ToArray(),
                PlotDataSuccessRates["Mem [peak]"]
                    .Select(e => (e.WorkingSet ?? 0) / (1024 * 1024))
                    .ToArray()
            );
            pRamMax.LegendText = "Mem [peak, highest process]";
            pCpu.LegendText = "CPU %";
            pCpu.MarkerSize = 6;
            pCpu.MarkerLineWidth = 3;
            pRamMax.MarkerSize = 8;
            pRamMax.MarkerLineWidth = 4;
            pRamAvg.MarkerSize = 8;
            pRamAvg.MarkerLineWidth = 4;

            try
            {
                etoPlotCpu.Plot.Axes.SetLimitsY(bottom: 0, top: double.PositiveInfinity);
                etoPlotCpu.Plot.ShowLegend();
                etoPlotCpu.Plot.Legend.FontName = "Courier";
                etoPlotCpu.Plot.Legend.Alignment = Alignment.UpperLeft;
                etoPlotCpu.Plot.DataBackground.Color = ScottPlot.Colors.Transparent;
                etoPlotCpu.Plot.Title($"CPU usage [{ProcessName}]");
                etoPlotCpu.Plot.XLabel("Date/Time");
                etoPlotCpu.Plot.YLabel("CPU-Hours %");
                etoPlotCpu.Plot.Axes.Color(ScottPlot.Colors.White);
                etoPlotCpu.Plot.FigureBackground = new ScottPlot.BackgroundStyle()
                {
                    Color = ScottPlot.Colors.Black,
                };
                etoPlotCpu.BackgroundColor = Eto.Drawing.Colors.Black;
                etoPlotCpu.Plot.Grid.MajorLineColor = ScottPlot.Colors.White;
                etoPlotCpu.Plot.Grid.MinorLineColor = ScottPlot.Colors.DarkGoldenRod;
                etoPlotCpu.Refresh();

                etoPlotMem.Plot.Axes.SetLimitsY(bottom: 0, top: double.PositiveInfinity);
                etoPlotMem.Plot.ShowLegend();
                etoPlotMem.Plot.Legend.FontName = "Courier";
                etoPlotMem.Plot.Legend.Alignment = Alignment.UpperLeft;
                etoPlotMem.Plot.DataBackground.Color = ScottPlot.Colors.Transparent;
                etoPlotMem.Plot.Title($"Mem stats [{ProcessName}]");
                etoPlotMem.Plot.XLabel("Date/Time");
                etoPlotMem.Plot.YLabel("Memory (MiB)");
                etoPlotMem.Plot.Axes.Color(ScottPlot.Colors.White);
                etoPlotMem.Plot.FigureBackground = new ScottPlot.BackgroundStyle()
                {
                    Color = ScottPlot.Colors.Black,
                };
                etoPlotMem.BackgroundColor = Eto.Drawing.Colors.Black;
                etoPlotMem.Plot.Grid.MajorLineColor = ScottPlot.Colors.White;
                etoPlotMem.Plot.Grid.MinorLineColor = ScottPlot.Colors.DarkGoldenRod;
                etoPlotMem.Refresh();
            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.ToString(), "An exception has occured", MessageBoxType.Error);
            }

            FilterText.TextInput += Filter;
            RadioProcessFilter.SelectedKeyChanged += Filter;

            ProcessList.KeyDown += (e, a) =>
            {
                if (
                    a.Key == Keys.Enter
                    && ProcessList.SelectedKey != null
                    && ProcessList.SelectedKey.Length > 1
                )
                    (
                        new SinglePanelFlyout(
                            new ProcessStatsFormHourlyPanel(ProcessList.SelectedKey),
                            $"Stats: {ProcessList.SelectedKey}"
                        )
                    ).Show();
                else if (a.Key == Keys.Escape)
                {
                    ProcessList.DataStore = ProcessCandidates
                        .Select(e => e.ProcessName)
                        .OrderBy(e => e.ToLowerInvariant())
                        .ToList();
                    ProcessList.IsDataContextChanging = true;
                }
                /*TODO: else if (a.Key == Keys.Down)
                {
                    if(Eto.Platform.Instance.ToString() == Eto.Platforms.Wpf)
                    {
                        ProcessList.ToNative().DroppedDown = true;
                    }
                }*/
            };

            void Filter(Object e, EventArgs a)
            {
                var LowerCaseFilterText = FilterText.Text.ToLowerInvariant();
                switch (RadioProcessFilter.SelectedKey)
                {
                    case "Any":
                        ProcessList.DataStore = ProcessCandidates
                            .AsParallel()
                            .Where(x =>
                                x.ProcessName.ToLowerInvariant().Contains(LowerCaseFilterText)
                                || x.WindowName.ToLowerInvariant().Contains(LowerCaseFilterText)
                            )
                            .Select(e => e.ProcessName)
                            .AsSequential()
                            .OrderBy(e => e.ToLowerInvariant())
                            .ToList();
                        GridMatchedProcessNames.DataStore = ProcessCandidates
                            .AsParallel()
                            .Where(x =>
                                x.ProcessName.ToLowerInvariant().Contains(LowerCaseFilterText)
                                || x.WindowName.ToLowerInvariant().Contains(LowerCaseFilterText)
                            )
                            .Select(e => new string[] { e.ProcessName, e.WindowName })
                            .AsSequential()
                            .OrderBy(e => e[0])
                            .ToList();
                        break;
                    case "Process name":
                        ProcessList.DataStore = ProcessCandidates
                            .Where(x => x.ProcessName.ToLowerInvariant().Contains(FilterText.Text))
                            .Select(e => e.ProcessName)
                            .OrderBy(e => e.ToLowerInvariant())
                            .ToList();
                        GridMatchedProcessNames.DataStore = ProcessCandidates
                            .Where(x => x.ProcessName.ToLowerInvariant().Contains(FilterText.Text))
                            .Select(e => new string[] { e.ProcessName, e.WindowName })
                            .OrderBy(e => e[0])
                            .ToList();
                        break;
                    case "Window title":
                        ProcessList.DataStore = ProcessCandidates
                            .Where(x =>
                                x.WindowName.ToLowerInvariant()
                                    .Contains(FilterText.Text.ToLowerInvariant())
                            )
                            .Select(e => e.ProcessName)
                            .OrderBy(e => e.ToLowerInvariant())
                            .ToList();
                        GridMatchedProcessNames.DataStore = ProcessCandidates
                            .Where(x =>
                                x.WindowName.ToLowerInvariant()
                                    .Contains(FilterText.Text.ToLowerInvariant())
                            )
                            .Select(e => new string[] { e.ProcessName, e.WindowName })
                            .OrderBy(e => e[0])
                            .ToList();
                        break;
                }
                ProcessList.IsDataContextChanging = true;
                GridMatchedProcessNames.Invalidate();
            }

            var VerticalStackLayout = new StackLayout()
            {
                Items = { ProcessSelectorAndFilter, TopStackLayout, etoPlotCpu, etoPlotMem },
                Orientation = Eto.Forms.Orientation.Vertical,
                Spacing = 20,
            };
            Content = VerticalStackLayout;
            //Resizable = false;
            Filter(null, null);
            try
            {
                //Icon = new Eto.Drawing.Icon("time-view.ico");
            }
            catch (System.Exception E)
            {
                MessageBox.Show(
                    $"{E.ToString()} \r\n {E.Message}",
                    "Icon loading failed",
                    MessageBoxType.Error
                );
            }
        }
    }
}
