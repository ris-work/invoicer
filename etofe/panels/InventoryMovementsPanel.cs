using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eto.Forms;
using Eto.Drawing;
using RV.InvNew.Common;

namespace YourAppNamespace
{
    public class InventoryMovementsPanel : Scrollable
    {
        // helper type for each row
        class MovRange
        {
            public string ItemCode { get; set; } = "";
            public string FriendlyName { get; set; } = "";
            public DateTime From { get; set; }
            public DateTime To { get; set; }

            // for grid display
            public string FromStr => From.ToString("yyyy-MM-dd");
            public string ToStr => To.ToString("yyyy-MM-dd");
        }

        readonly List<MovRange> _rows = new List<MovRange>();
        int _editingIndex = -1;

        // entry controls
        readonly TextBox _txtItemCode;
        readonly Label _lblFriendly;
        readonly DateTimePicker _dpFrom;
        readonly DateTimePicker _dpTo;
        readonly Button _btnSave;
        readonly Button _btnCancel;
        readonly Button _btnDelete;
        readonly Button _btnNew;
        readonly Button _btnFetch;
        readonly GridView _grid;

        public InventoryMovementsPanel()
        {
            // Initialize defaults
            _dpFrom = new DateTimePicker { Mode = DateTimePickerMode.Date, Value = DateTime.Now.AddMonths(-6) };
            _dpTo = new DateTimePicker { Mode = DateTimePickerMode.Date, Value = DateTime.Now };

            _txtItemCode = new TextBox { PlaceholderText = "Click to search…" };
            _lblFriendly = new Label();

            // launch search on focus
            _txtItemCode.GotFocus += (s, e) =>
            {
                _txtItemCode.Text = "";
                var sel = CommonUi.SearchPanelUtility
                            .GenerateSearchDialog<Catalogue>(
                                new List<Catalogue> { new Catalogue() },
                                this, debug: false
                            );
                if (sel?.Length > 0)
                {
                    _txtItemCode.Text = sel[0];
                    if (long.TryParse(sel[0], out var code))
                        _lblFriendly.Text = LookupHumanFriendlyItemcode(code);
                }
            };

            // New row button
            _btnNew = new Button { Text = "New" };
            _btnNew.Click += (_, __) => ResetEntry();

            // Save / Cancel / Delete
            _btnSave = new Button { Text = "Save" };
            _btnCancel = new Button { Text = "Cancel" };
            _btnDelete = new Button { Text = "Delete" };

            _btnSave.Click += (s, e) =>
            {
                var itemTxt = _txtItemCode.Text.Trim();
                if (!long.TryParse(itemTxt, out var code))
                    return;    // invalid code

                var row = new MovRange
                {
                    ItemCode = itemTxt,
                    FriendlyName = _lblFriendly.Text,
                    From = _dpFrom.Value ?? DateTime.MinValue,
                    To = _dpTo.Value ?? DateTime.Now
                };

                if (_editingIndex < 0)
                    _rows.Add(row);
                else
                    _rows[_editingIndex] = row;

                RefreshGrid();
                ResetEntry();
            };

            _btnCancel.Click += (_, __) =>
            {
                ResetEntry();
                _grid.SelectedRow = -1;
            };

            _btnDelete.Click += (s, e) =>
            {
                if (_editingIndex >= 0 && _editingIndex < _rows.Count)
                    _rows.RemoveAt(_editingIndex);

                RefreshGrid();
                ResetEntry();
            };

            // GridView (read-only)
            _grid = new GridView
            {
                DataStore = _rows,
                AllowMultipleSelection = false
            };
            _grid.Columns.Add(new GridColumn { HeaderText = "Item Code", DataCell = new TextBoxCell("ItemCode") });
            _grid.Columns.Add(new GridColumn { HeaderText = "Description", DataCell = new TextBoxCell("FriendlyName") });
            _grid.Columns.Add(new GridColumn { HeaderText = "From", DataCell = new TextBoxCell("FromStr") });
            _grid.Columns.Add(new GridColumn { HeaderText = "To", DataCell = new TextBoxCell("ToStr") });

            _grid.SelectionChanged += (s, e) =>
            {
                var idx = _grid.SelectedRow;
                if (idx >= 0 && idx < _rows.Count)
                {
                    _editingIndex = idx;
                    var r = _rows[idx];
                    _txtItemCode.Text = r.ItemCode;
                    _lblFriendly.Text = r.FriendlyName;
                    _dpFrom.Value = r.From;
                    _dpTo.Value = r.To;
                }
            };

            // Fetch Movements button
            _btnFetch = new Button { Text = "Fetch Movements" };
            _btnFetch.Click += async (s, e) =>
            {
                foreach (var r in _rows)
                {
                    if (long.TryParse(r.ItemCode, out var code))
                    {
                        var req = new GetInventoryMovementsRequest
                        {
                            ItemCode = code,
                            StartDate = r.From,
                            EndDate = r.To
                        };
                        var result = await FetchInventoryMovementsAsync(req);
                        // TODO: process `result`
                    }
                }
            };

            // Entry group
            var entryLayout = new TableLayout
            {
                Spacing = new Size(5, 5),
                Rows =
                {
                    new TableRow(new Label { Text="Item Code:" }, _txtItemCode, _lblFriendly),
                    new TableRow(new Label { Text="From:"      }, _dpFrom),
                    new TableRow(new Label { Text="To:"        }, _dpTo),
                    new TableRow(_btnNew, _btnSave, _btnCancel, _btnDelete)
                }
            };

            var group = new GroupBox { Text = "Entry", Content = entryLayout };

            // Main layout
            Content = new TableLayout
            {
                Padding = 10,
                Spacing = new Size(10, 10),
                Rows =
                {
                    group,
                    _grid,
                    _btnFetch
                }
            };

            // start with empty entry
            ResetEntry();
        }

        void ResetEntry()
        {
            _editingIndex = -1;
            _txtItemCode.Text = "";
            _lblFriendly.Text = "";
            _dpFrom.Value = DateTime.Now.AddMonths(-6);
            _dpTo.Value = DateTime.Now;
        }

        void RefreshGrid()
        {
            _grid.DataStore = null;
            _grid.DataStore = _rows;
        }

        // Stub search-friendly
        string LookupHumanFriendlyItemcode(long code) => $"TBD Item {code}";

        // Stubbed fetch method
        Task<List<InventoryMovement>> FetchInventoryMovementsAsync(GetInventoryMovementsRequest req)
        {
            return Task.FromResult(new List<InventoryMovement>
            {
                new InventoryMovement
                {
                    Itemcode        = req.ItemCode,
                    Batchcode       = 0,
                    BatchEnabled    = false,
                    MfgDate         = null,
                    ExpDate         = null,
                    PackedSize      = 0,
                    Units           = 0,
                    MeasurementUnit = "",
                    MarkedPrice     = 0,
                    SellingPrice    = 0,
                    CostPrice       = 0,
                    VolumeDiscounts = false,
                    Suppliercode    = 0,
                    UserDiscounts   = false,
                    LastCountedAt   = DateTime.UtcNow,
                    Remarks         = "",
                    Reference       = "",
                    EnteredTime     = DateTime.UtcNow
                }
            });
        }
    }
}
