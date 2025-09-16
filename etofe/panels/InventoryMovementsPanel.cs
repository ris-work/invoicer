using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommonUi;
using Eto.Drawing;
using Eto.Forms;
using EtoFE;
using RV.InvNew.Common;
using RV.InvNew.EtoFE;

namespace RV.Invnew.EtoFE
{
    public class InventoryMovementsPanel : Scrollable
    {
        const int FWidth = 200;
        const int FHeight = 25;

        // Make this public so JsonSerializer can see it
        public class MovRange
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
        PosRefresh PR;

        public InventoryMovementsPanel()
        {
            while (true)
            {
                var req = (
                    SendAuthenticatedRequest<string, PosRefresh>.Send(
                        "Refresh",
                        "/PosRefreshBearerAuth",
                        true
                    )
                );
                //req.ShowModal();
                if (req.Error == false)
                {
                    PR = req.Out;
                    //MessageBox.Show(req.Response.Catalogue.Count.ToString(), "Time", MessageBoxType.Information);
                    break;
                }
            }
            // date defaults
            _dpFrom = new DateTimePicker
            {
                Mode = DateTimePickerMode.Date,
                Value = DateTime.Now.AddMonths(-6),
            };
            _dpTo = new DateTimePicker { Mode = DateTimePickerMode.Date, Value = DateTime.Now };

            // item code + friendly label
            _txtItemCode = new TextBox { PlaceholderText = "Click to search…" };
            _lblFriendly = new Label();

            _txtItemCode.MouseUp += (s, e) =>
            {
                //_dpFrom.Focus();
                _txtItemCode.Text = "";
                var sel = CommonUi.SearchPanelUtility.GenerateSearchDialog<PosCatalogue>(
                    PR.Catalogue,
                    _dpFrom,
                    debug: false
                );
                if (sel?.Length > 0)
                {
                    _txtItemCode.Text = sel[0];
                    if (long.TryParse(sel[0], out var code))
                        _lblFriendly.Text = LookupHumanFriendlyItemcode(code);
                }
            };

            // new, save, cancel, delete
            _btnNew = new Button { Text = "New" };
            _btnSave = new Button { Text = "Save" };
            _btnCancel = new Button { Text = "Cancel" };
            _btnDelete = new Button { Text = "Delete" };

            _btnNew.Click += (_, __) => ResetEntry();

            _btnSave.Click += (s, e) =>
            {
                if (!long.TryParse(_txtItemCode.Text.Trim(), out var code))
                    return;

                var entry = new MovRange
                {
                    ItemCode = _txtItemCode.Text,
                    FriendlyName = _lblFriendly.Text,
                    From = _dpFrom.Value ?? DateTime.MinValue,
                    To = _dpTo.Value ?? DateTime.Now,
                };

                if (_editingIndex < 0)
                    _rows.Add(entry);
                else
                    _rows[_editingIndex] = entry;

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

            // grid (read‐only columns)
            _grid = new GridView { DataStore = _rows, AllowMultipleSelection = false };
            _grid.Columns.Add(
                new GridColumn { HeaderText = "Item Code", DataCell = new TextBoxCell("ItemCode") }
            );
            _grid.Columns.Add(
                new GridColumn
                {
                    HeaderText = "Description",
                    DataCell = new TextBoxCell("FriendlyName"),
                }
            );
            _grid.Columns.Add(
                new GridColumn { HeaderText = "From", DataCell = new TextBoxCell("FromStr") }
            );
            _grid.Columns.Add(
                new GridColumn { HeaderText = "To", DataCell = new TextBoxCell("ToStr") }
            );

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

            _grid.RowFormatting += (e, a) => { };

            // fetch button
            _btnFetch = new Button
            {
                Text = "Fetch Movements",
                Height = (ColorSettings.InnerControlHeight ?? 30) * 2,
            };
            _btnFetch.Click += (s, e) =>
            {
                List<(
                    long ItemCode,
                    string Description,
                    List<RV.InvNew.Common.InventoryMovement> BinCard
                )> Cards = new();
                if (_rows.Count() < 1)
                    return;
                foreach (var r in _rows)
                {
                    if (long.TryParse(r.ItemCode, out var code))
                    {
                        var req = new GetInventoryMovementsRequest
                        {
                            ItemCode = code,
                            StartDate = r.From,
                            EndDate = r.To,
                        };
                        var result = FetchInventoryMovementsAsync(req);
                        Cards.Add((code, LookupHumanFriendlyItemcode(code), result));
                        // TODO: handle result
                    }
                }
                var F = new Form() { Content = new BinCardVisualizerPanel(Cards) };
                F.Width = 800;
                F.Height = 600;
                F.Title = "Bin card";
                F.Show();
                Console.WriteLine("Bin card Show() was called.");
            };

            // entry UI
            var entryLayout = new TableLayout
            {
                Spacing = new Size(5, 5),
                Rows =
                {
                    new TableRow(new Label { Text = "Item Code:" }, _txtItemCode, _lblFriendly),
                    new TableRow(new Label { Text = "From:" }, _dpFrom),
                    new TableRow(new Label { Text = "To:" }, _dpTo),
                    new TableRow(_btnNew, _btnSave, _btnCancel, _btnDelete) { ScaleHeight = false },
                },
            };
            var group = new GroupBox { Text = "Entry", Content = entryLayout };
            entryLayout.Height = (ColorSettings.ControlHeight ?? 30) * 10;
            // main layout
            Content = new TableLayout
            {
                Padding = 10,
                Spacing = new Size(10, 10),
                Rows = { group, _grid, _btnFetch },
            };

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

        /// <summary>
        /// Serialize current rows to JSON.
        /// </summary>
        public string Serialize()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(_rows, options);
        }

        /// <summary>
        /// Replace current rows from a JSON string produced by Serialize().
        /// </summary>
        public void Deserialize(string json)
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<MovRange>>(json);
                if (list != null)
                {
                    _rows.Clear();
                    _rows.AddRange(list);
                    // Recompute friendly names if desired:
                    foreach (var r in _rows)
                    {
                        if (long.TryParse(r.ItemCode, out var code))
                            r.FriendlyName = LookupHumanFriendlyItemcode(code);
                    }
                    RefreshGrid();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Failed to load data: {ex.Message}",
                    MessageBoxButtons.OK,
                    MessageBoxType.Error
                );
            }
        }

        // stub for friendly lookup
        string LookupHumanFriendlyItemcode(long code) =>
            PR
                .Catalogue.Where(e => e.itemcode == code)
                .FirstOrDefault(new PosCatalogue() { itemdesc = "Unknown" })
                .itemdesc;

        // stub for fetch
        List<InventoryMovement> FetchInventoryMovementsAsync(GetInventoryMovementsRequest req_out)
        {
            List<InventoryMovement> PR;
            while (true)
            {
                var req = (
                    SendAuthenticatedRequest<
                        GetInventoryMovementsRequest,
                        List<InventoryMovement>
                    >.Send(req_out, "/GetInventoryMovements", true)
                );
                //req.ShowModal();
                if (req.Error == false)
                {
                    PR = req.Out;
                    var P = Eto.Platform.Instance.ToString();

                    if (P != Eto.Platform.Get(Eto.Platforms.WinForms).ToString())
                    {
                        MessageBox.Show(
                            JsonSerializer.Serialize(req.Out),
                            "Time",
                            MessageBoxType.Information
                        );
                    }
                    break;
                }
            }
            return PR;
        }
    }
}
