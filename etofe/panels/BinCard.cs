using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using Eto.Drawing;
using ScottPlot;
using ScottPlot.Eto;
using SPColor = ScottPlot.Color;     // Alias for ScottPlot’s Color type
using Colors = ScottPlot.Colors;    // Alias for ScottPlot’s static Colors class
using CommonUi;
using RV.InvNew.Common;
using EtoFE;

namespace RV.InvNew.EtoFE
{
    // A single bin‐card entry
    /*public class InventoryMovement
    {
        public DateTime Date { get; set; }
        public string RefType { get; set; } // e.g. "sale", "purchase", etc.
        public string RefNumber { get; set; }
        public double Quantity { get; set; } // + in, – out
        public double RunningBalance { get; set; }
    }*/

    // Wraps all movements for one SKU
    public class ItemMovements
    {
        public long ItemCode { get; set; }
        public List<InventoryMovement> BinCard { get; set; } = new();
        public override string ToString() => ItemCode.ToString();
    }

    // Main UI: pick an item, page through its bin‐card, and show a stacked bar chart
    public class BinCardVisualizerPanel : Scrollable
    {
        const int PageSize = 20;

        readonly List<ItemMovements> _items;
        int _currentItemIndex = 0;
        int _currentBinPage = 0;
        readonly string[] _refTypes = {
            "sale","consumption","issue","production",
            "salesreturn","purchase","purchasereturn",
            "adjustment","deprecated"
        };
        readonly Dictionary<string, CheckBox> _checkMap = new();
        readonly Dictionary<string, List<ScottPlot.Plottables.BarPlot>> _seriesMap = new();

        // Use ScottPlot.Colors.<Name> only
        static readonly SPColor[] _barColors = {
            Colors.Red,
            Colors.Orange,
            Colors.Yellow,
            Colors.Green,
            Colors.Blue,
            Colors.Purple,
            Colors.Pink,
            Colors.Gray,
            Colors.Brown
        };

        readonly SearchPanelEto _itemSearch;
        readonly Button _btnBinPrev, _btnBinNext;
        readonly Eto.Forms.Label _lblBinPage;
        readonly Panel _binCardContainer;
        readonly EtoPlot _plotView;

        public BinCardVisualizerPanel(
            List<(long ItemCode, List<InventoryMovement> BinCard)> data
        )
        {
            
            // Wrap input tuples
            _items = data
                .Select(x => new ItemMovements { ItemCode = x.ItemCode, BinCard = x.BinCard })
                .ToList();

            // 1) Search dropdown
            _itemSearch = new SearchPanelEto(
                _items.Select(im => (new[] { im.ItemCode.ToString() }, (Eto.Drawing.Color?)null, (Eto.Drawing.Color?)null)).ToList(),
                new List<(string Header, TextAlignment Alignment, bool NumericalSort)> {
                    ("ItemCode", TextAlignment.Left, true)
                },
                Debug: false, LocalColors: null
            );
            _itemSearch.OnSelectionMade = () =>
            {
                var sel = _itemSearch.Selected;
                if (sel != null && long.TryParse(sel[0], out var code))
                {
                    _currentItemIndex = _items.FindIndex(im => im.ItemCode == code);
                    _currentBinPage = 0;
                    RefreshBinCardPage();
                    PlotCurrent();
                }
            };

            // 2) Pagination
            _btnBinPrev = new Button { Text = "< Prev" };
            _btnBinNext = new Button { Text = "Next >" };
            _lblBinPage = new Eto.Forms.Label();

            _btnBinPrev.Click += (_, __) =>
            {
                if (_currentBinPage > 0)
                {
                    _currentBinPage--;
                    RefreshBinCardPage();
                }
            };
            _btnBinNext.Click += (_, __) =>
            {
                int total = GetTotalBinPages();
                if (_currentBinPage < total - 1)
                {
                    _currentBinPage++;
                    RefreshBinCardPage();
                }
            };

            var binPager = new StackLayout
            {
                Orientation = Eto.Forms.Orientation.Horizontal,
                Spacing = 5,
                Items = { _btnBinPrev, _lblBinPage, _btnBinNext }
            };
            _binCardContainer = new Panel();

            // 3) Plot + toggles
            _plotView = new EtoPlot { Width = 800, Height = 400 };

            var chkLayout = new StackLayout
            {
                Orientation = Eto.Forms.Orientation.Vertical,
                Spacing = 2
            };
            for (int i = 0; i < _refTypes.Length; i++)
            {
                string type = _refTypes[i];
                var cb = new CheckBox { Text = type, Checked = true };
                cb.CheckedChanged += (_, __) =>
                {
                    if (_seriesMap.TryGetValue(type, out var bars))
                    {
                        foreach (var bar in bars)
                            bar.IsVisible = cb.Checked == true;
                        _plotView.Refresh();
                    }
                };
                _checkMap[type] = cb;
                chkLayout.Items.Add(cb);
            }

            var bottomLayout = new TableLayout
            {
                Spacing = new Size(10, 0),
                Rows = {
                    new TableRow(
                        new TableCell(_plotView, scaleWidth: true),
                        new TableCell(new GroupBox
                        {
                            Text    = "Show Types",
                            Content = chkLayout
                        })
                    )
                }
            };

            Content = new TableLayout
            {
                Padding = 10,
                Spacing = new Size(10, 10),
                Rows = {
                    new TableRow(_itemSearch),
                    new TableRow(binPager),
                    new TableRow(_binCardContainer),
                    new TableRow(bottomLayout)
                }
            };

            // Initial load
            if (_items.Any())
            {
                //_itemSearch.SelectRow(0);
                RefreshBinCardPage();
                PlotCurrent();
            }
        }

        void RefreshBinCardPage()
        {
            var movs = _items[_currentItemIndex].BinCard;
            int totalPages = GetTotalBinPages();
            _lblBinPage.Text = $"Page {_currentBinPage + 1} of {totalPages}";

            var page = movs
                .Skip(_currentBinPage * PageSize)
                .Take(PageSize)
                .ToList();

            var grid = new GridView { DataStore = page };
            grid.Columns.Add(new GridColumn
            {
                HeaderText = "Date",
                DataCell = new TextBoxCell
                {
                    Binding = Binding.Delegate<InventoryMovement, string>(
                        x => x.EnteredTime.Date.ToShortDateString())
                }
            });
            grid.Columns.Add(new GridColumn
            {
                HeaderText = "Type",
                DataCell = new TextBoxCell
                {
                    Binding = Binding.Delegate<InventoryMovement, string>(
                        x => x.Reference.Split(':')[0])
                }
            });
            grid.Columns.Add(new GridColumn
            {
                HeaderText = "Ref No.",
                DataCell = new TextBoxCell
                {
                    Binding = Binding.Delegate<InventoryMovement, string>(
                        x => x.Reference)
                }
            });
            grid.Columns.Add(new GridColumn
            {
                HeaderText = "Qty",
                DataCell = new TextBoxCell
                {
                    Binding = Binding.Delegate<InventoryMovement, string>(
                        x => x.Units.ToString("0.##"))
                }
            });
            grid.Columns.Add(new GridColumn
            {
                HeaderText = "Balance",
                DataCell = new TextBoxCell
                {
                    Binding = Binding.Delegate<InventoryMovement, string>(
                        x => x.ToUnits.ToString("0.##"))
                }
            });

            _binCardContainer.Content = grid;
        }

        int GetTotalBinPages()
        {
            int count = _items[_currentItemIndex].BinCard.Count;
            return (count + PageSize - 1) / PageSize;
        }

        void PlotCurrent()
        {
            
                var movs = _items[_currentItemIndex].BinCard;
                int n = movs.Count;
                var plt = _plotView.Plot;
                plt.Clear();
                _seriesMap.Clear();

                // X positions and labels
                double[] positions = Enumerable.Range(1, n).Select(i => (double)i).ToArray();
                string[] labels = movs.Select(m => m.EnteredTime.ToString("MM-dd")).ToArray();

                // add one BarPlot per ref-type, stacking them
                for (int t = 0; t < _refTypes.Length; t++)
                {
                    string type = _refTypes[t];
                    double[] vals = movs
                        .Select(m => m.Reference.Split(':')[0] == type ? m.Units : 0)
                        .ToArray();

                    var bar = plt.Add.Bars(vals, positions);
                    //bar.StackGroup = 0;
                    //bar.Bars.ForEach(a => a.) = 0.8;
                    bar.Color = _barColors[t];
                    bar.LegendText = type;
                    bar.IsVisible = _checkMap[type].Checked == true;

                    _seriesMap[type] = new List<ScottPlot.Plottables.BarPlot> { bar };
                }

                // custom X-axis tick labels
                var tickGen = new ScottPlot.TickGenerators.NumericManual();
                for (int i = 0; i < positions.Length; i++)
                    tickGen.AddMajor(positions[i], labels[i]);
                plt.Axes.Bottom.TickGenerator = tickGen;

                // manual legend entries
                plt.Legend.ManualItems.Clear();
                for (int t = 0; t < _refTypes.Length; t++)
                    plt.Legend.ManualItems.Add(new LegendItem
                    {
                        LabelText = _refTypes[t],
                        FillColor = _barColors[t]
                    });
                plt.Legend.Orientation = ScottPlot.Orientation.Horizontal;
                plt.ShowLegend(ScottPlot.Alignment.UpperRight);

                // tighten margins and auto-scale
                plt.Axes.Margins(bottom: 0, top: .3);
                plt.Axes.AutoScale();

                _plotView.Refresh();


            
        }
    }
}
