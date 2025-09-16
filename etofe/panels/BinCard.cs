using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using CommonUi;
using Eto.Drawing;
using Eto.Forms;
using EtoFE;
using EtoFE.Panels;
//using ImageMagick;
using RV.InvNew.Common;
using ScottPlot;
using ScottPlot.Eto;
using Colors = ScottPlot.Colors; // Alias for ScottPlot’s static Colors class
using SPColor = ScottPlot.Color; // Alias for ScottPlot’s Color type

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
        public string Description { get; set; }
        public List<InventoryMovement> BinCard { get; set; } = new();

        public override string ToString() => ItemCode.ToString();
    }

    // Main UI: pick an item, page through its bin‐card, and show a stacked bar chart
    public class BinCardVisualizerPanel : Scrollable
    {


        public List<InventoryMovement> _curMovs;


            // Map each action to its desired background color.
            // Sale and Purchase now have distinct hues.
            new Dictionary<string, Eto.Drawing.Color> actionColors = new Dictionary<
            string,
            Eto.Drawing.Color
        >(StringComparer.OrdinalIgnoreCase)
        {
            // Good actions
            { "sale", Eto.Drawing.Colors.LightGreen },
            { "purchase", Eto.Drawing.Colors.LightSkyBlue },
            { "production", Eto.Drawing.Colors.LightGreen },
            // Bad or neutral actions
            { "consumption", Eto.Drawing.Colors.LightSalmon },
            { "issue", Eto.Drawing.Colors.LightSalmon },
            { "salesreturn", Eto.Drawing.Colors.LightSalmon },
            { "purchasereturn", Eto.Drawing.Colors.LightSalmon },
            { "adjustment", Eto.Drawing.Colors.LightGoldenrodYellow },
            { "deprecated", Eto.Drawing.Colors.LightGrey },
        };
        const int PageSize = 15;

        readonly List<ItemMovements> _items;
        int _currentItemIndex = 0;
        int _currentBinPage = 0;
        readonly string[] _refTypes =
        {
            "sale",
            "consumption",
            "issue",
            "production",
            "salesreturn",
            "purchase",
            "purchasereturn",
            "adjustment",
            "deprecated",
        };
        readonly Dictionary<string, CheckBox> _checkMap = new();
        readonly Dictionary<string, List<ScottPlot.Plottables.BarPlot>> _seriesMap = new();

        // Use ScottPlot.Colors.<Name> only
        static readonly SPColor[] _barColors =
        {
            Colors.Red,
            Colors.Orange,
            Colors.Yellow,
            Colors.Green,
            Colors.Blue,
            Colors.Purple,
            Colors.Pink,
            Colors.Gray,
            Colors.Brown,
        };

        readonly SearchPanelEto _itemSearch;
        readonly Button _btnBinPrev,
            _btnBinNext,
            _btnBinPopup, _btnBinExport, _btnBinExportPlot, _btnBinExportHtml;
        readonly Eto.Forms.Label _lblBinPage;
        readonly Panel _binCardContainer;
        readonly EtoPlot _plotView;
        private ScottPlot.Plot SP;

        public class InventoryMovementSummary
        {
            public string RefType { get; set; } = "";
            public DateTime Date { get; set; }
            public double TotalUnits { get; set; }
            public double TotalFrom { get; set; }
            public double TotalTo { get; set; }
        }

        public BinCardVisualizerPanel(
            List<(long ItemCode, string Description, List<InventoryMovement> BinCard)> data
        )
        {
            // Wrap input tuples
            _items = data.Select(x => new ItemMovements
                {
                    ItemCode = x.ItemCode,
                    Description = x.Description,
                    BinCard = x.BinCard,
                })
                .ToList();

            var flat = _items
                .Where(it => it.ItemCode >= 0)
                .SelectMany(it => it.BinCard)
                .OrderBy(m => m.EnteredTime)
                .ToList();

            var sum = new ItemMovements
            {
                ItemCode = -1,
                Description = "Sum",
                BinCard = flat.Select(m => new InventoryMovement
                    {
                        EnteredTime = m.EnteredTime,
                        Reference = m.Reference,
                        Units = m.Units,
                        FromUnits = m.FromUnits,
                        ToUnits = m.ToUnits,
                    })
                    .ToList(),
            };

            var avg = new ItemMovements
            {
                ItemCode = -2,
                Description = "Average",
                BinCard = flat.Select(m => new InventoryMovement
                    {
                        EnteredTime = m.EnteredTime,
                        Reference = m.Reference,
                        Units = m.Units,
                        FromUnits = m.FromUnits,
                        ToUnits = m.ToUnits,
                    })
                    .ToList(),
            };

            var gm = new ItemMovements
            {
                ItemCode = -3,
                Description = "Geometric mean",
                BinCard = flat.Select(m => new InventoryMovement
                    {
                        EnteredTime = m.EnteredTime,
                        Reference = m.Reference,
                        Units = m.Units,
                        FromUnits = m.FromUnits,
                        ToUnits = m.ToUnits,
                    })
                    .ToList(),
            };

            //_items.AddRange(new[] { sum, avg, gm });
            _items.AddRange(new[] { sum, avg });

            // 1) Search dropdown
            _itemSearch = new SearchPanelEto(
                _items
                    .Select(im =>
                        (
                            new[] { im.ItemCode.ToString(), im.Description },
                            (Eto.Drawing.Color?)null,
                            (Eto.Drawing.Color?)null
                        )
                    )
                    .ToList(),
                new List<(string Header, TextAlignment Alignment, bool NumericalSort)>
                {
                    ("ItemCode", TextAlignment.Left, true),
                    ("Description", TextAlignment.Left, false),
                },
                Debug: false,
                LocalColors: null,
                GWH: 120,
                GWW: 300
            )
            {
                Height = 120,
            };
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
            _itemSearch.Size = new Eto.Drawing.Size(300, 200);

            // 2) Pagination
            _btnBinPrev = new Button { Text = "< Prev" };
            _btnBinNext = new Button { Text = "Next >" };
            _btnBinPopup = new Button { Text = "Pop Up 📰" };
            _btnBinExport = new Button { Text = "Export" };
            _btnBinExportPlot = new Button { Text = "Export Plot 📊" };
            _btnBinExportHtml = new Button { Text = "Export HTML" };
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
            _btnBinPopup.Click += (_, __) =>
            {
                Console.WriteLine($"Popup Bin Card: {_currentItemIndex}");
                if (_currentItemIndex < 0)
                {
                    Console.WriteLine(
                        $"Popup Bin Card: {_currentItemIndex} <0, refusing to pop up"
                    );
                    return;
                }
                if (_currentItemIndex >= data.Count)
                {
                    Console.WriteLine(
                        $"Popup Bin Card: {_currentItemIndex} >={data.Count}, refusing to pop up"
                    );
                    return;
                }
                var item = data[_currentItemIndex];
                var frm = new BinCardPopoutData(item.ItemCode, item.Description, item.BinCard, 50);
                frm.Show();
            };
            _btnBinExport.Click += BtnBinExport_Click ;
            _btnBinExportHtml.Click += BtnBinExportHtml_Click;
            _btnBinExportPlot.Click += BtnBinExportPlot_Click;

            var binPager = new StackLayout
            {
                Orientation = Eto.Forms.Orientation.Horizontal,
                Spacing = 5,
                Items = { _btnBinPrev, _lblBinPage, _btnBinNext, _btnBinPopup, _btnBinExport, _btnBinExportPlot, _btnBinExportHtml },
                VerticalContentAlignment = Eto.Forms.VerticalAlignment.Center,
            };
            _binCardContainer = new Panel();

            // 3) Plot + toggles
            _plotView = new EtoPlot { Width = 800, Height = 400 };

            var chkLayout = new StackLayout
            {
                Orientation = Eto.Forms.Orientation.Vertical,
                Spacing = 2,
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
                Rows =
                {
                    new TableRow(
                        new TableCell(_plotView, scaleWidth: true),
                        new TableCell(new GroupBox { Text = "Show Types", Content = chkLayout })
                    ),
                },
            };

            Content = new TableLayout
            {
                Padding = 10,
                Spacing = new Size(10, 10),
                Rows =
                {
                    new TableRow(_itemSearch),
                    new TableRow(binPager),
                    new TableRow(_binCardContainer),
                    new TableRow(bottomLayout),
                },
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
            _curMovs = movs;
            Console.WriteLine($"currentItemIndex: {_currentItemIndex}, RowCount: {_items.Count}");
            int totalPages = GetTotalBinPages();
            _lblBinPage.Text = $"Page {_currentBinPage + 1} of {totalPages}";

            var page = movs.Skip(_currentBinPage * PageSize).Take(PageSize).ToList();

            var grid = new GridView()
            {
                Width = (ColorSettings.ControlWidth ?? 30) * 6,
                Height = (ColorSettings.ControlHeight ?? 30) * 11,
            };
            grid.DisableAutoSizing();


            grid.Columns.Add(
                new GridColumn
                {
                    HeaderText = "Date",
                    DataCell = new TextBoxCell
                    {
                        Binding = Binding.Delegate<InventoryMovement, string>(x =>
                            x.EnteredTime.Date.ToShortDateString()
                        ),
                    },
                }
            );
            grid.Columns.Add(
                new GridColumn
                {
                    HeaderText = "Type",
                    DataCell = new TextBoxCell
                    {
                        Binding = Binding.Delegate<InventoryMovement, string>(x =>
                            x.Reference.Split(':')[0]
                        ),
                    },
                }
            );
            grid.Columns.Add(
                new GridColumn
                {
                    HeaderText = "Ref No.",
                    DataCell = new TextBoxCell
                    {
                        Binding = Binding.Delegate<InventoryMovement, string>(x => x.Reference),
                    },
                }
            );
            grid.Columns.Add(
                new GridColumn
                {
                    HeaderText = "FromQty",
                    DataCell = new TextBoxCell
                    {
                        Binding = Binding.Delegate<InventoryMovement, string>(x =>
                            x.FromUnits.ToString("0.##")
                        ),
                    },
                }
            );
            grid.Columns.Add(
                new GridColumn
                {
                    HeaderText = "Qty",
                    DataCell = new TextBoxCell
                    {
                        Binding = Binding.Delegate<InventoryMovement, string>(x =>
                            x.Units.ToString("0.##")
                        ),
                    },
                }
            );
            grid.Columns.Add(
                new GridColumn
                {
                    HeaderText = "Balance (ToQty)",
                    DataCell = new TextBoxCell
                    {
                        Binding = Binding.Delegate<InventoryMovement, string>(x =>
                            x.ToUnits.ToString("0.##")
                        ),
                    },
                }
            );
            grid.RowFormatting += (sender, e) =>
            {
                // A) Inspect the raw e.Item
                var item = e.Item;
                if (item == null)
                {
                    Console.WriteLine($"RowFormatting: e.Item is null at row {e.Row}");
                    return;
                }

                // B) Ensure it’s an IList (string[] and List<string> both implement IList)
                if (!(item is IList rowItems))
                {
                    /*Console.WriteLine(
                        $"RowFormatting: e.Item is {item.GetType().Name}, not IList (remove this conditions later)"
                    );*/
                    //return;
                }

                if (!(item is InventoryMovement im))
                {
                    Console.WriteLine(
                        $"RowFormatting: e.Item is {item.GetType().Name}, not InventoryMovement"
                    );
                    return;
                }

                // C) Check we have at least 3 columns
                /*if (rowItems.Count < 3)
                {
                    Console.WriteLine($"RowFormatting: row {e.Row} has only {rowItems.Count} cols");
                    return;
                }*/

                // D) Pull out the raw RefNo
                var raw = im.Reference?.ToString() ?? string.Empty;

                // E) Split on ':' and normalize
                var key =
                    raw.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault()
                        ?.Trim()
                        .ToLowerInvariant() ?? string.Empty;

                // F) Lookup color
                if (!actionColors.TryGetValue(key, out var bg))
                {
                    Console.WriteLine($"RowFormatting: unknown action '{key}' at row {e.Row}");
                    return;
                }

                // G) Apply background
                e.BackgroundColor = bg;
            };

            var platformName = Eto.Platform.Instance.ToString();
            var winFormsName = Eto.Platform.Get(Eto.Platforms.WinForms).ToString();
            bool isWinForms = platformName.Equals(winFormsName, StringComparison.Ordinal);
            var osPlatform = Environment.OSVersion.Platform;
            bool isUnixLike = osPlatform == PlatformID.Unix || osPlatform == PlatformID.MacOSX;

            Console.WriteLine($"Eto backend: {platformName}");
            Console.WriteLine($"WinForms target: {winFormsName}");
            Console.WriteLine($"isWinForms={isWinForms}, OS={osPlatform}, isUnixLike={isUnixLike}");
            Console.WriteLine(
                $"Incoming page count: {(page == null ? "null" : page.Count.ToString())}"
            );

            if (isWinForms && isUnixLike)
            {
                Console.WriteLine("→ Clearing DataStore");
                grid.DataStore = null;

                if (page != null && page.Count > 0)
                {
                    try
                    {
                        Console.WriteLine("→ Assigning non-empty DataStore");
                        grid.DataStore = page;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"An exception has occured during the setting of DataContext: {ex.StackTrace}, {ex.ToString()}"
                        );
                    }
                }
                else
                {
                    Console.WriteLine("→ Page is null or empty; skipping assignment");
                }

                if (grid.ControlObject is System.Windows.Forms.DataGridView dg)
                {
                    Console.WriteLine("→ Disabling AutoSizeRowsMode");
                    dg.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.None;
                }
                else
                {
                    Console.WriteLine(
                        $"→ ControlObject is {grid.ControlObject?.GetType().Name}; no autosize tweak"
                    );
                }
            }
            else
            {
                Console.WriteLine("→ Standard DataStore assignment");
                grid.DataStore = page;
            }
            Console.WriteLine("→ Done with DataStore assignment");
            Console.WriteLine("Second attempt at setting DataStore");
            try
            {
                grid.DataStore = page;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"An exception has occured during the setting of DataContext: {ex.StackTrace}, {ex.ToString()}"
                );
            }

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
            var summary = movs
            // 1. Project out the refType and date
            .Select(im => new
                {
                    RefType = im.Reference != null && im.Reference.Contains(':')
                        ? im.Reference.Substring(0, im.Reference.IndexOf(':'))
                        : im.Reference,
                    Date = im.EnteredTime.Date,
                    im.Units,
                    im.FromUnits,
                    im.ToUnits,
                })
                // 2. Filter only the refTypes you care about
                .Where(x => _refTypes.Contains(x.RefType!))
                // 3. Group by refType + date
                .GroupBy(x => new { x.RefType, x.Date })
                // 4. Project your sums
                .Select(g => new InventoryMovementSummary
                {
                    RefType = g.Key.RefType!,
                    Date = g.Key.Date,
                    TotalUnits = g.Sum(y => y.Units),
                    TotalFrom = g.Sum(y => y.FromUnits),
                    TotalTo = g.Sum(y => y.ToUnits),
                })
                .ToList();

            if (_items[_currentItemIndex].ItemCode < 0)
            {
                var item = _items[_currentItemIndex];
                var perItem = _items
                    .Where(it => it.ItemCode >= 0)
                    .SelectMany(it =>
                        it.BinCard.Select(im =>
                        {
                            var r = im.Reference ?? "";
                            var i = r.IndexOf(':');
                            var refType = i >= 0 ? r.Substring(0, i) : r;
                            var refNo = i >= 0 ? r.Substring(i + 1) : null;
                            return new
                            {
                                it.ItemCode,
                                RefType = refType,
                                RefNo = refNo,
                                Date = im.EnteredTime.Date,
                                im.Units,
                                im.FromUnits,
                                im.ToUnits,
                            };
                        })
                    )
                    .Where(x => _refTypes.Contains(x.RefType))
                    .GroupBy(x => new
                    {
                        x.ItemCode,
                        x.RefType,
                        x.Date,
                    })
                    .Select(g => new
                    {
                        g.Key.ItemCode,
                        g.Key.RefType,
                        g.Key.Date,
                        Units = g.Sum(y => y.Units),
                        From = g.Sum(y => y.FromUnits),
                        To = g.Sum(y => y.ToUnits),
                    })
                    .ToList();

                switch (item.ItemCode)
                {
                    case -2: // Average of per-item totals
                        summary = perItem
                            .GroupBy(x => new { x.RefType, x.Date })
                            .Select(g => new InventoryMovementSummary
                            {
                                RefType = g.Key.RefType,
                                Date = g.Key.Date,
                                TotalUnits = g.Average(y => y.Units),
                                TotalFrom = g.Average(y => y.From),
                                TotalTo = g.Average(y => y.To),
                            })
                            .ToList();
                        break;

                    case -3: // Geometric mean of per-item totals
                        summary = perItem
                            .GroupBy(x => new { x.RefType, x.Date })
                            .Select(g =>
                            {
                                var u = g.Where(y => y.Units > 0).Select(y => Math.Log(y.Units));
                                var f = g.Where(y => y.From > 0).Select(y => Math.Log(y.From));
                                var t = g.Where(y => y.To > 0).Select(y => Math.Log(y.To));
                                return new InventoryMovementSummary
                                {
                                    RefType = g.Key.RefType,
                                    Date = g.Key.Date,
                                    TotalUnits = u.Any() ? Math.Exp(u.Average()) : 0d,
                                    TotalFrom = f.Any() ? Math.Exp(f.Average()) : 0d,
                                    TotalTo = t.Any() ? Math.Exp(t.Average()) : 0d,
                                };
                            })
                            .ToList();
                        break;

                    case -1: // Sum of per-item totals
                    default:
                        summary = perItem
                            .GroupBy(x => new { x.RefType, x.Date })
                            .Select(g => new InventoryMovementSummary
                            {
                                RefType = g.Key.RefType,
                                Date = g.Key.Date,
                                TotalUnits = g.Sum(y => y.Units),
                                TotalFrom = g.Sum(y => y.From),
                                TotalTo = g.Sum(y => y.To),
                            })
                            .ToList();
                        break;
                }
            }

            // X positions and labels
            double[] positions = Enumerable.Range(1, n).Select(i => (double)i).ToArray();
            string[] labels = movs.Select(m => m.EnteredTime.ToString("MM-dd")).ToArray();

            // add one BarPlot per ref-type, stacking them
            for (int t = 0; t < _refTypes.Length; t++)
            {
                string type = _refTypes[t];
                Console.WriteLine(type);
                double[] vals = movs.Select(m => m.Reference.Split(':')[0] == type ? m.Units : 0)
                    .ToArray();
                var Series = movs.Select(m => m.EnteredTime.ToString("MM-dd"));
                Bar[] barsCollection = summary
                    .Where(s => s.RefType == type)
                    .Select(s => new Bar { Value = s.TotalUnits, Position = s.Date.ToOADate() })
                    .ToArray();

                var bar = plt.Add.Bars(barsCollection);
                System.Console.WriteLine(
                    $"summary: {summary.Count()}, {type}: {barsCollection.Count()}"
                );
                //bar.StackGroup = 0;
                //bar.Bars.ForEach(a => a.) = 0.8;
                bar.Color = _barColors[t].WithAlpha(150);
                bar.LegendText = type;
                bar.IsVisible = _checkMap[type].Checked == true;

                _seriesMap[type] = new List<ScottPlot.Plottables.BarPlot> { bar };
            }
            //We do the actual stock count here
            double[] endStock = movs.Select(m => m.ToUnits).ToArray();

            //var movs = _items[_currentItemIndex].BinCard;
            var dailyEndOfDay = movs
            // Group by the calendar date of the movement
            .GroupBy(m => m.EnteredTime.Date)
                // For each date, pick the record with the most recent timestamp
                .Select(g => g.OrderByDescending(x => x.EnteredTime).First())
                // Project out the date + quantity
                .Select(m => new { Date = m.EnteredTime.Date, Quantity = m.ToUnits })
                .ToList();
            var line = plt.Add.Scatter(
                dailyEndOfDay.Select(d => d.Date.ToOADate()).ToArray(),
                dailyEndOfDay.Select(d => d.Quantity).ToArray()
            );
            line.LegendText = "Current quantity";
            line.IsVisible = true;

            // custom X-axis tick labels
            var tickGen = new ScottPlot.TickGenerators.NumericManual();
            for (int i = 0; i < positions.Length; i++)
                tickGen.AddMajor(positions[i], labels[i]);
            //plt.Axes.Bottom.TickGenerator = tickGen;

            // manual legend entries
            /*plt.Legend.ManualItems.Clear();
            for (int t = 0; t < _refTypes.Length; t++)
                plt.Legend.ManualItems.Add(new LegendItem
                {
                    LabelText = _refTypes[t],
                    FillColor = _barColors[t]
                });*/
            plt.Legend.Orientation = ScottPlot.Orientation.Horizontal;
            plt.ShowLegend(ScottPlot.Alignment.UpperRight);

            // tighten margins and auto-scale
            plt.Axes.Margins(bottom: 0, top: .3);
            plt.Axes.AutoScale();
            plt.Axes.DateTimeTicksBottom();

            _plotView.Refresh();
        }

        string GetRefType(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                return reference;

            var idx = reference.IndexOf(':');
            return idx >= 0 ? reference.Substring(0, idx) : reference;
        }


        // BtnBinExport_Click: CSV
        private void BtnBinExport_Click(object sender, EventArgs e)
        {
            try
            {
                var sfd = new SaveFileDialog { Title = "Export CSV" };
                sfd.Filters.Add(new FileFilter("CSV Files", ".csv"));
                var result = sfd.ShowDialog(this);
                if (result != DialogResult.Ok) return;

                var path = sfd.FileName;
                using var writer = new StreamWriter(path);

                // header
                writer.WriteLine("Date,Type,Ref No.,FromQty,Qty,Balance (ToQty)");

                // CSV-escape
                static string EscapeCsv(string s)
                {
                    if (string.IsNullOrEmpty(s)) return "";
                    var needsQuotes = s.Contains(',') || s.Contains('"') || s.Contains('\n');
                    s = s.Replace("\"", "\"\"");
                    return needsQuotes ? $"\"{s}\"" : s;
                }

                foreach (var m in _curMovs)
                {
                    var f = new[]
                    {
                EscapeCsv(m.EnteredTime.Date.ToShortDateString()),
                EscapeCsv(m.Reference.Split(':')[0]),
                EscapeCsv(m.Reference),
                m.FromUnits.ToString("0.##"),
                m.Units.ToString("0.##"),
                m.ToUnits.ToString("0.##")
            };
                    writer.WriteLine(string.Join(",", f));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting CSV: {ex}");
            }
        }

        // BtnBinExportHtml_Click: HTML
        private void BtnBinExportHtml_Click(object sender, EventArgs e)
        {
            try
            {
                var sfd = new SaveFileDialog { Title = "Export HTML" };
                sfd.Filters.Add(new FileFilter("HTML Files", ".html"));
                var result = sfd.ShowDialog(this);
                if (result != DialogResult.Ok) return;

                var path = sfd.FileName;
                using var writer = new StreamWriter(path);

                // basic HTML-escape
                static string HtmlEscape(string s)
                {
                    if (string.IsNullOrEmpty(s)) return "";
                    return s.Replace("&", "&amp;")
                            .Replace("<", "&lt;")
                            .Replace(">", "&gt;")
                            .Replace("\"", "&quot;")
                            .Replace("'", "&#39;");
                }

                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine("<html><head><meta charset=\"utf-8\"><title>Inventory Movements</title></head><body>");
                writer.WriteLine("<table border=\"1\">");
                writer.WriteLine("<thead><tr><th>Date</th><th>Type</th><th>Ref No.</th><th>FromQty</th><th>Qty</th><th>Balance (ToQty)</th></tr></thead>");
                writer.WriteLine("<tbody>");
                foreach (var m in _curMovs)
                {
                    writer.WriteLine("<tr>");
                    writer.WriteLine($"<td>{HtmlEscape(m.EnteredTime.Date.ToShortDateString())}</td>");
                    writer.WriteLine($"<td>{HtmlEscape(m.Reference.Split(':')[0])}</td>");
                    writer.WriteLine($"<td>{HtmlEscape(m.Reference)}</td>");
                    writer.WriteLine($"<td>{m.FromUnits:0.##}</td>");
                    writer.WriteLine($"<td>{m.Units:0.##}</td>");
                    writer.WriteLine($"<td>{m.ToUnits:0.##}</td>");
                    writer.WriteLine("</tr>");
                }
                writer.WriteLine("</tbody>");
                writer.WriteLine("</table>");
                writer.WriteLine("</body></html>");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting HTML: {ex}");
            }
        }

        // BtnBinExportPlot_Click: SVG/PNG/JPEG
        private void BtnBinExportPlot_Click(object sender, EventArgs e)
        {
            try
            {
                var sfd = new SaveFileDialog { Title = "Export Plot" };
                sfd.Filters.Add(new FileFilter("SVG Files", ".svg"));
                sfd.Filters.Add(new FileFilter("PNG Files", ".png"));
                sfd.Filters.Add(new FileFilter("JPEG Files", ".jpg;.jpeg"));
                var result = sfd.ShowDialog(this);
                if (result != DialogResult.Ok) return;

                var path = sfd.FileName;
                var ext = Path.GetExtension(path).ToLowerInvariant();
                var plt = _plotView.Plot;

                if (ext == ".svg")
                {
                    //File.WriteAllText(path, plt.GetSvg());
                    plt.SaveSvg(path, 1920, 1080);
                }
                else if (ext == ".jpg")
                {
                    plt.SaveJpeg(path, 1920, 1080);
                }
                else if (ext == ".bmp")
                {
                    plt.SaveBmp(path, 1920, 1080);
                }
                else {
                    plt.SavePng(path, 1920, 1080);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting plot: {ex}");
            }
        }
    }
}
