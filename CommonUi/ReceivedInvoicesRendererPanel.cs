using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
//using MS.WindowsAPICodePack.Internal;
using RV.InvNew.Common;

//using MS.WindowsAPICodePack.Internal;

namespace CommonUi
{
    // The panel that displays a list of Purchase items and calculates totals.
    public class PurchasePanel : Panel
    {
        public Action<int> DeleteReceivedInvoiceItem;
        private readonly GridView _gridView;
        private readonly Label _indexLabel;
        private List<Purchase> _purchases = new List<Purchase>();
        public Action<string[]> AnythingHappened = (_) => { };

        /// <summary>
        /// When left empty, the panel automatically hides:
        ///   • "ReceivedInvoiceId"
        ///   • Any field whose title contains "percentage" (case-insensitive)
        ///     except "GrossMarkupPercentage" and "VatPercentage".
        /// You can also supply specific field names to hide via this list.
        /// </summary>
        public List<string> HiddenItems { get; set; } =
            new List<string>() { "ManufacturerBatchId", "GrossMarkupPercentage" };

        /// <summary>
        /// Returns the currently selected row's index.
        /// </summary>
        public int SelectedItemIndex => _gridView.SelectedRow;

        public PurchasePanel(PanelSettings? LocalColors = null)
        {
            var Colors = new PanelSettings()
            {
                AlternatingColor1 =
                    LocalColors?.AlternatingColor1 ?? ColorSettings.AlternatingColor1,
                AlternatingColor2 =
                    LocalColors?.AlternatingColor2 ?? ColorSettings.AlternatingColor2,
                SelectedColumnColor =
                    LocalColors?.SelectedColumnColor ?? ColorSettings.SelectedColumnColor,
                LesserForegroundColor =
                    LocalColors?.LesserForegroundColor ?? ColorSettings.LesserForegroundColor,
                LesserBackgroundColor =
                    LocalColors?.LesserBackgroundColor ?? ColorSettings.LesserBackgroundColor,
                ForegroundColor = LocalColors?.ForegroundColor ?? ColorSettings.ForegroundColor,
                BackgroundColor = LocalColors?.BackgroundColor ?? ColorSettings.BackgroundColor,
            };
            _gridView = new GridView() { Height = 300 };
            _gridView.SelectionChanged += (sender, e) =>
            {
                _indexLabel.Text =
                    $"Selected Index: {(_gridView.SelectedRow >= 0 ? _gridView.SelectedRow.ToString() : "None")}";
            };
            _gridView.CellFormatting += (e, a) =>
            {
                a.Font = Eto.Drawing.Fonts.Monospace(11);
                a.BackgroundColor = Colors.BackgroundColor;
                a.ForegroundColor = Colors.ForegroundColor;
                //Colour the column first
                if (a.Column.DisplayIndex % 2 == 1)
                {
                    a.BackgroundColor = Colors.AlternatingColor2;
                    a.ForegroundColor = Colors.ForegroundColor;
                }
                //Override with row colours
                if (a.Row % 2 == 0)
                {
                    a.BackgroundColor = Colors.AlternatingColor1;
                    a.ForegroundColor = Colors.ForegroundColor;
                }
                if (a.Row == _gridView.SelectedRow)
                {
                    a.Column.AutoSize = true;
                    a.BackgroundColor = Colors.SelectedColumnColor;
                    a.ForegroundColor = Colors.ForegroundColor;
                    a.Font = Eto.Drawing.Fonts.Monospace(9, FontStyle.Bold);
                }
            };
            _gridView.RowFormatting += (e, a) =>
            {
                a.BackgroundColor = Colors.LesserBackgroundColor;
            };
            _gridView.DisableLines();
            _gridView.ApplyDarkGridHeaders();
            _gridView.ConfigureForPlatform();

            _indexLabel = new Label { Text = "Selected Index: None" };

            // Stack the label above the grid.
            var layout = new DynamicLayout { Padding = 10, Spacing = new Eto.Drawing.Size(5, 5) };
            layout.Add(_indexLabel);
            layout.Add(_gridView, yscale: true);
            this.Content = layout;
        }

        /// <summary>
        /// Renders the list of Purchase items in the GridView.
        /// Before binding the list, each Purchase is updated by calling the
        /// CalculateNetTotal() and CalculateTotalAmountDue() extension methods.
        /// Columns are defined in a logical order:
        ///
        /// [Index column] - 50px
        /// Core product/packaging fields: Itemcode, ProductName, ManufacturerBatchId,
        /// PackSize, PackQuantity, ReceivedAsUnitQuantity, FreePacks, FreeUnits,
        /// TotalUnits (F2), ManufacturingDate (date-only), ExpiryDate (date-only), AddedDate.
        ///
        /// Pricing and totals fields:
        /// DiscountAbsolute, GrossMarkupPercentage, GrossMarkupAbsolute,
        /// CostPerUnit, CostPerPack, GrossCostPerUnit, SellingPrice,
        /// NetTotalPrice, VatPercentage, VatAbsolute, and finally TotalAmountDue.
        ///
        /// The auto-hide logic removes ReceivedInvoiceId and any fields matching the
        /// "percentage" rule—except GrossMarkupPercentage and VatPercentage.
        /// </summary>
        /// <param name="purchases">The list of Purchase items to display.</param>
        public void Render(List<Purchase> purchases)
        {
            _purchases = purchases;

            // Update each purchase using the calculation extension methods.
            // This call both updates the original Purchase and returns it for chaining.
            foreach (var p in _purchases)
            {
                p.CalculateNetTotal().CalculateTotalAmountDue();
            }

            _gridView.Columns.Clear();

            // Add an index column.
            var indexColumn = new GridColumn
            {
                HeaderText = "#",
                DataCell = new TextBoxCell
                {
                    Binding = new DelegateBinding<Purchase, string>(purchase =>
                        _purchases.IndexOf(purchase).ToString()
                    ),
                },
                Width = 20,
            };
            _gridView.Columns.Add(indexColumn);

            // Define columns in a logical order.
            var columns = new List<(string Title, Func<Purchase, string> Getter, int Width)>
            {
                // Although "ReceivedInvoiceId" is defined, auto-hide logic will remove it.
                ("ReceivedInvoiceId", purchase => purchase.ReceivedInvoiceId.ToString(), 80),
                // Core product and packaging fields.
                ("Itemcode", purchase => purchase.Itemcode.ToString(), 50),
                ("ProductName", purchase => purchase.ProductName, 150),
                (
                    "ManufacturerBatchId",
                    purchase => purchase.ManufacturerBatchId ?? string.Empty,
                    120
                ),
                ("PackSize", purchase => purchase.PackSize.ToString(), 30),
                ("PackQuantity", purchase => purchase.PackQuantity.ToString(), 30),
                (
                    "ReceivedAsUnitQuantity",
                    purchase => purchase.ReceivedAsUnitQuantity.ToString(),
                    80
                ),
                ("FreePacks", purchase => purchase.FreePacks.ToString(), 30),
                ("FreeUnits", purchase => purchase.FreeUnits.ToString(), 30),
                ("TotalUnits", purchase => purchase.TotalUnits.ToString("F2"), 50),
                (
                    "ManufacturingDate",
                    purchase => purchase.ManufacturingDate?.ToString("d") ?? string.Empty,
                    20
                ),
                ("ExpiryDate", purchase => purchase.ExpiryDate.ToString("d"), 50),
                ("AddedDate", purchase => purchase.AddedDate.ToString("g"), 50),
                // Pricing details.
                ("DiscountAbsolute", purchase => purchase.DiscountAbsolute.ToString("F2"), 50),
                (
                    "GrossMarkupPercentage",
                    purchase => purchase.GrossMarkupPercentage.ToString("F2"),
                    50
                ),
                (
                    "GrossMarkupAbsolute",
                    purchase => purchase.GrossMarkupAbsolute.ToString("F2"),
                    50
                ),
                ("CostPerUnit", purchase => purchase.CostPerUnit.ToString("F2"), 50),
                ("CostPerPack", purchase => purchase.CostPerPack.ToString("F2"), 50),
                ("GrossCostPerUnit", purchase => purchase.GrossCostPerUnit.ToString("F2"), 50),
                ("SellingPrice", purchase => purchase.SellingPrice.ToString("F2"), 50),
                // Final summary fields.
                ("NetTotalPrice", purchase => purchase.NetTotalPrice.ToString("F2"), 100),
                ("VatPercentage", purchase => purchase.VatPercentage.ToString("F2"), 20),
                ("VatAbsolute", purchase => purchase.VatAbsolute.ToString("F2"), 50),
                ("TotalAmountDue", purchase => purchase.TotalAmountDue.ToString("F2"), 100),
            };

            // Apply auto-hide logic. If HiddenItems is empty, then automatically hide:
            //   • "ReceivedInvoiceId"
            //   • Any field whose name contains "percentage" (case-insensitive)
            //     except "GrossMarkupPercentage" and "VatPercentage".
            foreach (var col in columns)
            {
                bool skipColumn = false;

                if (HiddenItems.Count == 0)
                {
                    if (col.Title.Equals("ReceivedInvoiceId", StringComparison.OrdinalIgnoreCase))
                        skipColumn = true;
                    else if (
                        col.Title.IndexOf("percentage", StringComparison.OrdinalIgnoreCase) >= 0
                        && !col.Title.Equals(
                            "GrossMarkupPercentage",
                            StringComparison.OrdinalIgnoreCase
                        )
                        && !col.Title.Equals("VatPercentage", StringComparison.OrdinalIgnoreCase)
                    )
                        skipColumn = true;
                }
                else
                {
                    // If a custom HiddenItems list is provided, hide matching columns.
                    if (
                        HiddenItems.Exists(x =>
                            string.Equals(x, col.Title, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                        skipColumn = true;
                }

                if (skipColumn)
                    continue;

                var gridColumn = new GridColumn
                {
                    HeaderText = TranslationHelper.Translate(
                        col.Title,
                        col.Title,
                        ColorSettings.Lang
                    ),
                    DataCell = new TextBoxCell
                    {
                        Binding = new DelegateBinding<Purchase, string>(col.Getter),
                        TextAlignment = double.TryParse(col.Getter(new Purchase()), out _)
                            ? TextAlignment.Right
                            : TextAlignment.Left,
                    },
                    Width = col.Width,
                };

                _gridView.Columns.Add(gridColumn);
            }

            _gridView.DataStore = _purchases;
            _gridView.KeyUp += (e, a) =>
            {
                if (a.Key == Keys.Delete && _gridView.SelectedRow > -1)
                {
                    DeleteReceivedInvoiceItem(_gridView.SelectedRow);
                }
            };
            AnythingHappened([]);
        }
    }
}
