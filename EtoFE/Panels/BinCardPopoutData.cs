using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonUi;
using Eto.Drawing;
using Eto.Forms;
using RV.InvNew.Common;
using RV.InvNew.EtoFE;
using ScottPlot.Eto;

namespace EtoFE.Panels
{
    public class BinCardPopoutData : Form
    {
        public List<InventoryMovement> BinCard;
        public int PageSize;
        public long ItemCode;
        public string Description;

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

        readonly SearchPanelEto _itemSearch;
        readonly Button _btnBinPrev,
            _btnBinNext;
        readonly Eto.Forms.Label _lblBinPage;
        readonly Panel _binCardContainer;
        readonly EtoPlot _plotView;

        public BinCardPopoutData(
            long ItemCode,
            string Description,
            List<InventoryMovement> BinCard,
            int PageSize
        )
        {
            this.BinCard = BinCard;
            this.PageSize = PageSize;
            this.Description = Description;
            this.ItemCode = ItemCode;

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
                Items = { _btnBinPrev, _lblBinPage, _btnBinNext },
            };
            _binCardContainer = new Panel();

            Content = new TableLayout
            {
                Padding = 10,
                Spacing = new Size(10, 10),
                Rows = { new TableRow(binPager), new TableRow(_binCardContainer) },
            };
            RefreshBinCardPage();
        }

        void RefreshBinCardPage()
        {
            var movs = BinCard;
            int totalPages = GetTotalBinPages();
            _lblBinPage.Text = $"Page {_currentBinPage + 1} of {totalPages}";

            var page = movs.Skip(_currentBinPage * PageSize).Take(PageSize).ToList();

            var grid = new GridView {  };
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
                    HeaderText = "Balance",
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
                    Console.WriteLine(
                        $"RowFormatting: e.Item is {item.GetType().Name}, not IList (remove this conditions later)"
                    );
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
            grid.DataStore = page;

            _binCardContainer.Content = grid;
        }

        int GetTotalBinPages()
        {
            int count = BinCard.Count;
            return (count + PageSize - 1) / PageSize;
        }
    }
}
