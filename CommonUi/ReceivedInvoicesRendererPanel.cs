using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using RV.InvNew.Common;

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
        public List<string> HiddenItems { get; set; } = new List<string>();

        /// <summary>
        /// Returns the currently selected row's index.
        /// </summary>
        public int SelectedItemIndex => _gridView.SelectedRow;

        public PurchasePanel()
        {
            _gridView = new GridView();
            _gridView.SelectionChanged += (sender, e) =>
            {
                _indexLabel.Text =
                    $"Selected Index: {(_gridView.SelectedRow >= 0 ? _gridView.SelectedRow.ToString() : "None")}";
            };

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
                Width = 50,
            };
            _gridView.Columns.Add(indexColumn);

            // Define columns in a logical order.
            var columns = new List<(string Title, Func<Purchase, string> Getter, int Width)>
            {
                // Although "ReceivedInvoiceId" is defined, auto-hide logic will remove it.
                ("ReceivedInvoiceId", purchase => purchase.ReceivedInvoiceId.ToString(), 80),
                // Core product and packaging fields.
                ("Itemcode", purchase => purchase.Itemcode.ToString(), 80),
                ("ProductName", purchase => purchase.ProductName, 150),
                (
                    "ManufacturerBatchId",
                    purchase => purchase.ManufacturerBatchId ?? string.Empty,
                    120
                ),
                ("PackSize", purchase => purchase.PackSize.ToString(), 80),
                ("PackQuantity", purchase => purchase.PackQuantity.ToString(), 80),
                (
                    "ReceivedAsUnitQuantity",
                    purchase => purchase.ReceivedAsUnitQuantity.ToString(),
                    80
                ),
                ("FreePacks", purchase => purchase.FreePacks.ToString(), 80),
                ("FreeUnits", purchase => purchase.FreeUnits.ToString(), 80),
                ("TotalUnits", purchase => purchase.TotalUnits.ToString("F2"), 80),
                (
                    "ManufacturingDate",
                    purchase => purchase.ManufacturingDate?.ToString("d") ?? string.Empty,
                    100
                ),
                ("ExpiryDate", purchase => purchase.ExpiryDate.ToString("d"), 100),
                ("AddedDate", purchase => purchase.AddedDate.ToString("g"), 120),
                // Pricing details.
                ("DiscountAbsolute", purchase => purchase.DiscountAbsolute.ToString("F2"), 80),
                (
                    "GrossMarkupPercentage",
                    purchase => purchase.GrossMarkupPercentage.ToString("F2"),
                    80
                ),
                (
                    "GrossMarkupAbsolute",
                    purchase => purchase.GrossMarkupAbsolute.ToString("F2"),
                    80
                ),
                ("CostPerUnit", purchase => purchase.CostPerUnit.ToString("F2"), 80),
                ("CostPerPack", purchase => purchase.CostPerPack.ToString("F2"), 80),
                ("GrossCostPerUnit", purchase => purchase.GrossCostPerUnit.ToString("F2"), 80),
                ("SellingPrice", purchase => purchase.SellingPrice.ToString("F2"), 80),
                // Final summary fields.
                ("NetTotalPrice", purchase => purchase.NetTotalPrice.ToString("F2"), 100),
                ("VatPercentage", purchase => purchase.VatPercentage.ToString("F2"), 80),
                ("VatAbsolute", purchase => purchase.VatAbsolute.ToString("F2"), 80),
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
                    HeaderText = col.Title,
                    DataCell = new TextBoxCell
                    {
                        Binding = new DelegateBinding<Purchase, string>(col.Getter),
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
