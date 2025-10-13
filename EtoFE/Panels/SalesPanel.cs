using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Eto.Drawing;
using Eto.Forms;
using RV.InvNew.Common;
using EtoFE.Validation;
using EtoFE.Search;
using CommonUi;

namespace EtoFE.Panels
{
    public class SalesPanel : Scrollable
    {
        // Constants for field dimensions
        const int FWidth = 120;
        const int FHeight = 24;
        const int Columns = 2; // Default column count

        // Data storage
        List<Sale> sales;
        List<Receipt> receipts;
        IssuedInvoice invoice;
        List<Inventory> selectedBatches = new List<Inventory>(); // Changed from availableBatches

        // UI Controls - Invoice Information
        TextBox tbCustomer;
        Button btnCustomerSearch;
        Label lblCustomerName;
        TextBox tbSalesPerson;
        Button btnSalesPersonSearch;
        Label lblSalesPersonName;
        TextBox tbCurrency;
        DateTimePicker dpInvoiceDate;
        CheckBox cbIsPosted;
        TextArea taRemarks;

        // UI Controls - Sales Items
        GridView saleGrid;
        TextBox tbItem;
        Button btnItemSearch;
        Label lblItemName;
        TextBox tbQty;
        TextBox tbPrice;
        TextBox tbDisc;
        Button btnSaveSale;
        Button btnResetSale;

        // New gridview to show selected batches
        GridView batchGrid;

        // UI Controls - Summary
        Label lblQtyTot;
        Label lblSubTot;
        Label lblTaxTot;
        TextBox tbPaid;
        Label lblChange;

        // UI Controls - Receipts
        GridView rcptGrid;
        TextBox tbRcptAcct;
        Button btnRcptAcctSearch;
        Label lblAcctName;
        TextBox tbRcptAmt;
        Button btnSaveRcpt;
        Button btnResetRcpt;
        Label lblRcptTot;

        // UI Controls - Action Buttons
        Button btnNew;
        Button btnLoad;
        Button btnEdit;
        Button btnSave;
        Button btnCancel;
        Button btnReset;

        // Navigation helpers
        List<Control> essentialControls;
        List<TextBox> naFieldsInOrder;
        Dictionary<TextBox, int> naFieldIndex;
        Dictionary<TextBox, Button> naFieldMap;
        Dictionary<Button, TextBox> naButtonToTextBoxMap;
        Control lastFocused;
        bool isProcessingKey = false;

        // Constructor with ID parameter
        public SalesPanel(long? invoiceId = null)
        {
            InitializeData();
            InitializeFields();
            BuildLayout();
            WireEvents();

            if (invoiceId.HasValue)
            {
                LoadData(invoiceId.Value);
            }
            else
            {
                ResetForm();
            }

            RecalculateAll();
        }

        // Parameterless constructor
        public SalesPanel() : this(null) { }

        void InitializeData()
        {
            sales = new List<Sale> { new Sale() };
            receipts = new List<Receipt> { new Receipt() };
            invoice = new IssuedInvoice();

            GlobalState.RefreshBAT();
            GlobalState.RefreshPR();
        }

        void InitializeFields()
        {
            var cw = ColorSettings.ControlWidth ?? FWidth;
            var ch = ColorSettings.ControlHeight ?? FHeight;
            int hw = (int)Math.Floor(cw * 0.2);

            // Invoice Information Fields
            tbCustomer = new TextBox { Width = cw, Height = ch };
            btnCustomerSearch = new Button { Text = "...", Height = ch, Width = hw };
            lblCustomerName = new Label();

            tbSalesPerson = new TextBox { Width = cw, Height = ch };
            btnSalesPersonSearch = new Button { Text = "...", Height = ch, Width = hw };
            lblSalesPersonName = new Label();

            tbCurrency = NewTextBox("Currency");
            dpInvoiceDate = new DateTimePicker
            {
                Value = DateTime.Now,
                Width = cw,
                Height = ch,
            };
            cbIsPosted = new CheckBox { Text = TranslationHelper.Translate("Posted") };
            taRemarks = new TextArea
            {
                Width = 300,
                Height = 60,
                //PlaceholderText = TranslationHelper.Translate("Remarks")
            };

            // Sales grid
            saleGrid = new GridView
            {
                Width = 600,
                Height = 120,
                DataStore = sales,
            };

            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Name"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.ProductName) },
                Width = 150
            });
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Item"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.Itemcode.ToString()) },
                Width = 60
            });
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Batch"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.Batchcode.ToString()) },
                Width = 60
            });
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Qty"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.Quantity.ToString("F2")) },
                Width = 80
            });
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Price"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.SellingPrice.ToString("C2")) },
                Width = 80
            });
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Disc%"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.DiscountRate.ToString("F2")) },
                Width = 60
            });
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("VAT"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.VatAsCharged.ToString("C2")) },
                Width = 80
            });
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Total"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.TotalEffectiveSellingPrice.ToString("C2")) },
                Width = 80
            });

            // New batch selection grid
            batchGrid = new GridView
            {
                Width = 600,
                Height = 100,
                AllowMultipleSelection = true,
                DataStore = selectedBatches,
            };

            batchGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Batch Code"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Inventory, string>(b => b.Batchcode.ToString()) },
                Width = 80
            });
            batchGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Available"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Inventory, string>(b => b.Units.ToString("F2")) },
                Width = 100
            });
            batchGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("MFG Date"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Inventory, string>(b => b.MfgDate?.ToString("yyyy-MM-dd") ?? "") },
                Width = 100
            });
            batchGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Expiry Date"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Inventory, string>(b => b.ExpDate?.ToString("yyyy-MM-dd") ?? "") },
                Width = 100
            });
            /*batchGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Cost"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Inventory, string>(b => b.Cost?.ToString("C2") ?? "") },
                Width = 100
            });*/

            tbItem = new TextBox { Width = cw, Height = ch };
            btnItemSearch = new Button { Text = "...", Height = ch, Width = hw };
            lblItemName = new Label();

            tbQty = NewTextBox("Quantity");
            tbPrice = NewTextBox("Price");
            tbDisc = NewTextBox("Discount %");

            btnSaveSale = new Button
            {
                Text = TranslationHelper.Translate("Save"),
                Width = cw,
                Height = ch,
            };
            btnResetSale = new Button
            {
                Text = TranslationHelper.Translate("Reset"),
                Width = cw,
                Height = ch,
            };

            // Summary Fields
            lblQtyTot = new Label();
            lblSubTot = new Label();
            lblTaxTot = new Label();
            tbPaid = NewTextBox("Paid");
            lblChange = new Label();

            // Receipts Fields
            rcptGrid = new GridView
            {
                Width = 600,
                Height = 100,
                DataStore = receipts,
            };
            rcptGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Account"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Receipt, string>(r => r.AccountId.ToString()) },
            });
            rcptGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Amount"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Receipt, string>(r => r.Amount.ToString("C2")) },
            });

            tbRcptAcct = new TextBox { Width = cw, Height = ch };
            btnRcptAcctSearch = new Button { Text = "...", Height = ch, Width = hw };
            lblAcctName = new Label();
            tbRcptAmt = NewTextBox("Amount");
            btnSaveRcpt = new Button
            {
                Text = TranslationHelper.Translate("Save"),
                Width = cw,
                Height = ch,
            };
            btnResetRcpt = new Button
            {
                Text = TranslationHelper.Translate("Reset"),
                Width = cw,
                Height = ch,
            };
            lblRcptTot = new Label();

            // Action Buttons
            btnNew = new Button { Text = TranslationHelper.Translate("New"), Width = cw, Height = ch };
            btnLoad = new Button { Text = TranslationHelper.Translate("Load"), Width = cw, Height = ch };
            btnEdit = new Button { Text = TranslationHelper.Translate("Edit"), Width = cw, Height = ch };
            btnSave = new Button { Text = TranslationHelper.Translate("Save"), Width = cw, Height = ch };
            btnCancel = new Button { Text = TranslationHelper.Translate("Cancel"), Width = cw, Height = ch };
            btnReset = new Button { Text = TranslationHelper.Translate("Reset"), Width = cw, Height = ch };

            SetupNavigation();
        }

        void SetupNavigation()
        {
            // NA fields only include Customer, SalesPerson, and Receipt Account - Item selection triggers batch dialog
            naFieldsInOrder = new List<TextBox>
            {
                tbCustomer, tbSalesPerson, tbRcptAcct
            };

            naFieldIndex = naFieldsInOrder.Select((field, index) => new { field, index })
                .ToDictionary(x => x.field, x => x.index);

            naFieldMap = new Dictionary<TextBox, Button>
            {
                { tbCustomer, btnCustomerSearch },
                { tbSalesPerson, btnSalesPersonSearch },
                { tbRcptAcct, btnRcptAcctSearch }
            };

            naButtonToTextBoxMap = naFieldMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            // Define essential controls for Enter key navigation
            essentialControls = new List<Control>
            {
                tbCustomer, btnCustomerSearch, tbSalesPerson, btnSalesPersonSearch,
                tbCurrency, dpInvoiceDate, cbIsPosted, taRemarks,
                tbItem, btnItemSearch,
                tbQty, tbPrice, tbDisc, btnSaveSale, btnResetSale,
                tbPaid, tbRcptAcct, btnRcptAcctSearch, tbRcptAmt, btnSaveRcpt, btnResetRcpt,
                btnNew, btnLoad, btnEdit, btnSave, btnCancel, btnReset
            };
        }

        void BuildLayout()
        {
            // Create button panel
            var buttonPanel = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Items =
                {
                    new StackLayoutItem(btnNew),
                    new StackLayoutItem(btnLoad),
                    new StackLayoutItem(btnEdit),
                    new StackLayoutItem(btnSave),
                    new StackLayoutItem(btnCancel),
                    new StackLayoutItem(btnReset)
                }
            };

            // Create invoice form
            var invoiceForm = BuildNColumnForm(
                (
                    TranslationHelper.Translate("Customer:"),
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items = { tbCustomer, btnCustomerSearch, lblCustomerName },
                    }
                ),
                (
                    TranslationHelper.Translate("Sales Person:"),
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items = { tbSalesPerson, btnSalesPersonSearch, lblSalesPersonName },
                    }
                ),
                (TranslationHelper.Translate("Currency:"), tbCurrency),
                (TranslationHelper.Translate("Date:"), dpInvoiceDate),
                (TranslationHelper.Translate("Posted"), cbIsPosted),
                (TranslationHelper.Translate("Remarks:"), taRemarks)
            );

            // Create sales form
            var saleForm = BuildNColumnForm(
                (
                    TranslationHelper.Translate("Item:"),
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items = { tbItem, btnItemSearch, lblItemName },
                    }
                ),
                (TranslationHelper.Translate("Qty:"), tbQty),
                (TranslationHelper.Translate("Price:"), tbPrice),
                (TranslationHelper.Translate("Disc%:"), tbDisc),
                (TranslationHelper.Translate("Save"), btnSaveSale),
                (TranslationHelper.Translate("Reset"), btnResetSale)
            );

            // Create summary panel
            var summaryPanel = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 20,
                Items = { lblQtyTot, lblSubTot, lblTaxTot },
            };

            // Create payment panel
            var paymentPanel = BuildNColumnForm(
                (TranslationHelper.Translate("Paid:"), tbPaid),
                (TranslationHelper.Translate("Change:"), lblChange)
            );

            // Create receipt form
            var rcptForm = BuildNColumnForm(
                (
                    TranslationHelper.Translate("Acct:"),
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items = { tbRcptAcct, btnRcptAcctSearch, lblAcctName },
                    }
                ),
                (TranslationHelper.Translate("Amt:"), tbRcptAmt),
                (TranslationHelper.Translate("Save"), btnSaveRcpt),
                (TranslationHelper.Translate("Reset"), btnResetRcpt)
            );

            // Main layout
            Content = new StackLayout
            {
                Width = ColorSettings.MaxControlWidth ?? 820,
                Height = ColorSettings.MaxControlWidth ?? 800,
                Orientation = Orientation.Vertical,
                Spacing = 10,
                Padding = 10,
                Items =
                {
                    buttonPanel,
                    new GroupBox { Text = TranslationHelper.Translate("Invoice Info"), Content = invoiceForm },
                    new GroupBox
                    {
                        Text = TranslationHelper.Translate("Sales Items"),
                        Content = new StackLayout
                        {
                            Spacing = 5,
                            Items = {
                                saleGrid,
                                saleForm,
                                new GroupBox
                                {
                                    Text = TranslationHelper.Translate("Selected Batches (Ordered by Expiry)"),
                                    Content = batchGrid
                                }
                            }
                        },
                    },
                    new GroupBox { Text = TranslationHelper.Translate("Summary"), Content = summaryPanel },
                    new GroupBox { Text = TranslationHelper.Translate("Payment"), Content = paymentPanel },
                    new GroupBox
                    {
                        Text = TranslationHelper.Translate("Receipts"),
                        Content = new StackLayout
                        {
                            Spacing = 5,
                            Items = { rcptGrid, rcptForm, lblRcptTot },
                        },
                    },
                },
            };
        }

        TableLayout BuildNColumnForm(params (string Label, Control Control)[] items)
        {
            var layout = new TableLayout { Padding = 5, Spacing = new Size(5, 5) };

            for (int i = 0; i < items.Length; i += Columns)
            {
                var row = new TableRow();
                for (int c = 0; c < Columns; c++)
                {
                    int idx = i + c;
                    if (idx < items.Length)
                    {
                        row.Cells.Add(new TableCell(new Label { Text = items[idx].Label }));
                        row.Cells.Add(new TableCell(items[idx].Control));
                    }
                    else
                    {
                        row.Cells.Add(new TableCell());
                        row.Cells.Add(new TableCell());
                    }
                }
                layout.Rows.Add(row);
            }

            return layout;
        }

        TextBox NewTextBox(string placeholder) =>
            new TextBox
            {
                PlaceholderText = TranslationHelper.Translate(placeholder),
                Width = ColorSettings.ControlWidth ?? FWidth,
                Height = ColorSettings.ControlHeight ?? FHeight,
            };

        void WireEvents()
        {
            // Button events
            btnNew.Click += (_, __) => NewForm();
            btnLoad.Click += (_, __) => LoadForm();
            btnEdit.Click += (_, __) => EditForm();
            btnSave.Click += (_, __) => SaveForm();
            btnCancel.Click += (_, __) => CancelForm();
            btnReset.Click += (_, __) => ResetForm();

            btnSaveSale.Click += (_, __) => AddSale();
            btnResetSale.Click += (_, __) => ResetSaleForm();

            btnSaveRcpt.Click += (_, __) => AddReceipt();
            btnResetRcpt.Click += (_, __) => ResetRcptForm();

            // NA search button clicks
            btnCustomerSearch.Click += (_, __) => HandleNASearch(tbCustomer, lblCustomerName,
                BackOfficeAccounting.SearchCustomers, BackOfficeAccounting.LookupCustomerName);
            btnSalesPersonSearch.Click += (_, __) => HandleNASearch(tbSalesPerson, lblSalesPersonName,
                BackOfficeAccounting.SearchUsers, BackOfficeAccounting.LookupUser);
            btnItemSearch.Click += (_, __) => HandleNASearch(tbItem, lblItemName,
                BackOfficeAccounting.SearchItems, BackOfficeAccounting.LookupItem);
            btnRcptAcctSearch.Click += (_, __) => HandleNASearch(tbRcptAcct, lblAcctName,
                BackOfficeAccounting.SearchAccounts, BackOfficeAccounting.LookupAccount);

            // Text change events
            tbCustomer.TextChanged += (_, __) =>
            {
                var id = ParseLong(tbCustomer.Text);
                lblCustomerName.Text = id > 0 ? BackOfficeAccounting.LookupCustomerName(id) : "";
            };
            tbSalesPerson.TextChanged += (_, __) =>
            {
                var id = ParseLong(tbSalesPerson.Text);
                lblSalesPersonName.Text = id > 0 ? BackOfficeAccounting.LookupUser(id) : "";
            };
            tbItem.TextChanged += (_, __) =>
            {
                var id = ParseLong(tbItem.Text);
                lblItemName.Text = id > 0 ? BackOfficeAccounting.LookupItem(id) : "";
                if (id > 0) LoadAndSelectBatches(id);
            };
            tbRcptAcct.TextChanged += (_, __) =>
            {
                var id = ParseLong(tbRcptAcct.Text);
                lblAcctName.Text = id > 0 ? BackOfficeAccounting.LookupAccount(id) : "";
            };
            tbPaid.TextChanged += (_, __) => UpdateChange();

            // Key events for navigation
            WireKeyNavigation();

            // Focus tracking
            foreach (var control in essentialControls)
            {
                control.GotFocus += (s, e) => lastFocused = (Control)s;
            }
        }

        void LoadAndSelectBatches(long itemCode)
        {
            selectedBatches.Clear();

            var batches = GlobalState.BAT.Inv
                .Where(b => b.Itemcode == itemCode)
                .OrderBy(b => b.ExpDate ?? DateTime.MaxValue)
                .ToList();

            if (batches.Count == 0)
            {
                MessageBox.Show(this, "No available batches for this item", "Warning", MessageBoxType.Warning);
                return;
            }

            if (batches.Count == 1)
            {
                // Auto-select the single batch
                selectedBatches.Add(batches[0]);
                batchGrid.DataStore = null;
                batchGrid.DataStore = selectedBatches;
            }
            else
            {
                // Show batch selection dialog
                var selectedIndices = ShowBatchSelectionDialog(batches);
                if (selectedIndices != null && selectedIndices.Length > 0)
                {
                    foreach (var idx in selectedIndices)
                    {
                        if (idx >= 0 && idx < batches.Count)
                            selectedBatches.Add(batches[idx]);
                    }
                    batchGrid.DataStore = null;
                    batchGrid.DataStore = selectedBatches;
                }
            }
        }

        int[] ShowBatchSelectionDialog(List<Inventory> batches)
        {
            var dialog = new Dialog { Title = "Select Batches", Width = 700, Height = 400 };

            var grid = new GridView
            {
                AllowMultipleSelection = true,
                DataStore = batches,
            };

            grid.Columns.Add(new GridColumn
            {
                HeaderText = "Batch Code",
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Inventory, string>(b => b.Batchcode.ToString()) },
                Width = 80
            });
            grid.Columns.Add(new GridColumn
            {
                HeaderText = "Available",
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Inventory, string>(b => b.Units.ToString("F2")) },
                Width = 100
            });
            grid.Columns.Add(new GridColumn
            {
                HeaderText = "MFG Date",
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Inventory, string>(b => b.MfgDate?.ToString("yyyy-MM-dd") ?? "") },
                Width = 100
            });
            grid.Columns.Add(new GridColumn
            {
                HeaderText = "Expiry Date",
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Inventory, string>(b => b.ExpDate?.ToString("yyyy-MM-dd") ?? "") },
                Width = 100
            });
            /*grid.Columns.Add(new GridColumn
            {
                HeaderText = "Cost",
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Inventory, string>(b => b.Cost?.ToString("C2") ?? "") },
                Width = 100
            });*/

            var btnOK = new Button { Text = "OK" };
            var btnCancel = new Button { Text = "Cancel" };

            int[] result = null;

            btnOK.Click += (_, __) =>
            {
                var selected = grid.SelectedItems.Cast<Inventory>().ToList();
                result = selected.Select(b => batches.IndexOf(b)).ToArray();
                dialog.Close();
            };

            btnCancel.Click += (_, __) => dialog.Close();

            var buttonPanel = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Items = { btnOK, btnCancel }
            };

            dialog.Content = new StackLayout
            {
                Padding = 10,
                Spacing = 10,
                Items =
                {
                    new Label { Text = "Select one or more batches (ordered by expiry date):" },
                    grid,
                    buttonPanel
                }
            };

            dialog.ShowModal(this);
            return result;
        }

        void HandleNASearch(TextBox textBox, Label label, Func<Control, string[]> searchFunc,
            Func<long, string> lookupFunc)
        {
            var sel = searchFunc(this);
            if (sel?.Length > 0 && long.TryParse(sel[0], out var id))
            {
                textBox.Text = id.ToString();
                label.Text = lookupFunc(id) ?? "";
                MoveToNextNAField(textBox);
            }
        }

        void WireKeyNavigation()
        {
            this.KeyDown += (s, e) =>
            {
                if (isProcessingKey) return;

                if (e.Key == Keys.Enter)
                {
                    isProcessingKey = true;

                    if (lastFocused is TextBox focusedTextBox && naFieldMap.TryGetValue(focusedTextBox, out var associatedButton))
                    {
                        associatedButton.Focus();
                    }
                    else if (lastFocused is Button focusedButton && naButtonToTextBoxMap.TryGetValue(focusedButton, out var naTextBox))
                    {
                        focusedButton.PerformClick();
                    }
                    else
                    {
                        MoveToNextEssentialControl();
                    }

                    e.Handled = true;
                    isProcessingKey = false;
                }
            };

            this.KeyUp += (s, e) =>
            {
                if (isProcessingKey) return;

                Control keyControl = lastFocused;
                if (lastFocused is Button && lastFocused.Parent is StackLayout sl)
                {
                    keyControl = sl.Items.Select(i => i.Control).OfType<TextBox>().FirstOrDefault();
                }

                if (new[] { Keys.F1, Keys.F2, Keys.F3, Keys.F4 }.Contains(e.Key))
                {
                    if (keyControl is TextBox textBox)
                    {
                        isProcessingKey = true;
                        Application.Instance.AsyncInvoke(() =>
                        {
                            if (naFieldMap.TryGetValue(textBox, out var btn))
                                btn.PerformClick();
                            isProcessingKey = false;
                        });
                    }
                    e.Handled = true;
                }
                else if (new[] { Keys.F9, Keys.F10, Keys.F11, Keys.F12 }.Contains(e.Key))
                {
                    isProcessingKey = true;
                    Application.Instance.AsyncInvoke(() =>
                    {
                        SaveForm();
                        isProcessingKey = false;
                    });
                    e.Handled = true;
                }
                else if (new[] { Keys.F5, Keys.F6 }.Contains(e.Key))
                {
                    isProcessingKey = true;
                    Application.Instance.AsyncInvoke(() =>
                    {
                        EditForm();
                        isProcessingKey = false;
                    });
                    e.Handled = true;
                }
                else if (new[] { Keys.F7, Keys.F8 }.Contains(e.Key))
                {
                    isProcessingKey = true;
                    Application.Instance.AsyncInvoke(() =>
                    {
                        ResetForm();
                        isProcessingKey = false;
                    });
                    e.Handled = true;
                }
            };
        }

        void MoveToNextNAField(TextBox currentField)
        {
            if (!naFieldIndex.TryGetValue(currentField, out int currentIndex))
                return;

            int nextIndex = (currentIndex + 1) % naFieldsInOrder.Count;
            var nextField = naFieldsInOrder[nextIndex];
            nextField.Focus();
        }

        void MoveToNextEssentialControl()
        {
            var currentIndex = essentialControls.IndexOf(lastFocused);
            if (currentIndex >= 0 && currentIndex < essentialControls.Count - 1)
            {
                var nextControl = essentialControls[currentIndex + 1];
                nextControl.Focus();
            }
        }

        void AddSale()
        {
            if (!ValidateSaleForm())
                return;

            long itemCode = ParseLong(tbItem.Text);
            double quantity = ParseDouble(tbQty.Text);

            if (selectedBatches.Count == 0)
            {
                MessageBox.Show(this, "Please select batches for this item", "Error", MessageBoxType.Error);
                return;
            }

            // Distribute quantity across selected batches in order
            double remainingQuantity = quantity;
            List<(long batchCode, double quantity)> batchAllocations = new List<(long, double)>();

            foreach (var batch in selectedBatches)
            {
                if (remainingQuantity <= 0)
                    break;

                double batchQuantity = Math.Min(remainingQuantity, batch.Units);
                batchAllocations.Add((batch.Batchcode, batchQuantity));
                remainingQuantity -= batchQuantity;
            }

            if (remainingQuantity > 0)
            {
                MessageBox.Show(this, $"Not enough inventory. Available: {quantity - remainingQuantity}, Required: {quantity}", "Error", MessageBoxType.Error);
                return;
            }

            // Add sales for each allocated batch
            foreach (var (batchCode, batchQuantity) in batchAllocations)
            {
                var item = GlobalState.BAT.Cat.FirstOrDefault(c => c.Itemcode == itemCode);
                if (item == null) continue;

                var vatCategory = GlobalState.BAT.VCat.FirstOrDefault(v => v.VatCategoryId == item.DefaultVatCategory);
                double vatRate = vatCategory?.VatPercentage ?? 0;

                double price = ParseDouble(tbPrice.Text);
                double discountRate = ParseDouble(tbDisc.Text);

                var sale = new Sale
                {
                    Itemcode = itemCode,
                    Batchcode = batchCode,
                    Quantity = batchQuantity,
                    SellingPrice = price,
                    DiscountRate = discountRate,
                    ProductName = item.Description,
                    Discount = batchQuantity * price * discountRate / 100,
                    VatRatePercentage = vatRate
                };

                sale.VatAsCharged = (batchQuantity * price - sale.Discount) * vatRate / 100;
                sale.TotalEffectiveSellingPrice = batchQuantity * price - sale.Discount + sale.VatAsCharged;

                sales.Add(sale);
            }

            saleGrid.DataStore = null;
            saleGrid.DataStore = sales;
            RecalculateAll();
            ResetSaleForm();
        }

        bool ValidateSaleForm()
        {
            var itemId = ParseLong(tbItem.Text);
            if (itemId <= 0)
            {
                MessageBox.Show(this, "Please select a valid item", "Validation Error", MessageBoxType.Error);
                tbItem.Focus();
                return false;
            }

            if (selectedBatches.Count == 0)
            {
                MessageBox.Show(this, "Please select at least one batch", "Validation Error", MessageBoxType.Error);
                batchGrid.Focus();
                return false;
            }

            if (!double.TryParse(tbQty.Text, out double qty) || qty <= 0)
            {
                MessageBox.Show(this, "Please enter a valid quantity", "Validation Error", MessageBoxType.Error);
                tbQty.Focus();
                return false;
            }

            var totalAvailable = selectedBatches.Sum(b => b.Units);
            if (qty > totalAvailable)
            {
                MessageBox.Show(this, $"Requested quantity ({qty}) exceeds available stock ({totalAvailable})", "Validation Error", MessageBoxType.Error);
                tbQty.Focus();
                return false;
            }

            if (!double.TryParse(tbPrice.Text, out double price) || price <= 0)
            {
                MessageBox.Show(this, "Please enter a valid price", "Validation Error", MessageBoxType.Error);
                tbPrice.Focus();
                return false;
            }

            if (!double.TryParse(tbDisc.Text, out double disc) || disc < 0 || disc > 100)
            {
                MessageBox.Show(this, "Please enter a valid discount (0-100)", "Validation Error", MessageBoxType.Error);
                tbDisc.Focus();
                return false;
            }

            return true;
        }

        void ResetSaleForm()
        {
            tbItem.Text = tbQty.Text = tbPrice.Text = tbDisc.Text = "";
            lblItemName.Text = "";
            selectedBatches.Clear();
            batchGrid.DataStore = null;
        }

        void AddReceipt()
        {
            if (!ValidateReceiptForm())
                return;

            long accountId = ParseLong(tbRcptAcct.Text);
            double amount = ParseDouble(tbRcptAmt.Text);

            var receipt = new Receipt
            {
                InvoiceId = invoice.InvoiceId,
                AccountId = accountId,
                Amount = amount,
                TimeReceived = DateTimeOffset.Now,
            };

            receipts.Add(receipt);
            rcptGrid.DataStore = null;
            rcptGrid.DataStore = receipts;
            RecalculateAll();
            ResetRcptForm();
        }

        bool ValidateReceiptForm()
        {
            var accountId = ParseLong(tbRcptAcct.Text);
            if (accountId <= 0)
            {
                MessageBox.Show(this, "Please select a valid account", "Validation Error", MessageBoxType.Error);
                tbRcptAcct.Focus();
                return false;
            }

            if (!double.TryParse(tbRcptAmt.Text, out double amount) || amount <= 0)
            {
                MessageBox.Show(this, "Please enter a valid amount", "Validation Error", MessageBoxType.Error);
                tbRcptAmt.Focus();
                return false;
            }

            return true;
        }

        void ResetRcptForm()
        {
            tbRcptAcct.Text = tbRcptAmt.Text = "";
            lblAcctName.Text = "";
        }

        void RecalculateAll()
        {
            invoice.Customer = ParseLong(tbCustomer.Text);
            invoice.SalesPersonId = ParseLong(tbSalesPerson.Text);
            invoice.CurrencyCode = tbCurrency.Text;
            invoice.InvoiceTime = dpInvoiceDate.Value ?? DateTime.Now;
            invoice.IsPosted = cbIsPosted.Checked == true;
            invoice.InvoiceHumanFriendly = taRemarks.Text;

            invoice.SubTotal = sales.Where(s => s.Itemcode > 0).Sum(x => x.Quantity * x.SellingPrice);
            invoice.DiscountTotal = sales.Where(s => s.Itemcode > 0).Sum(x => x.Discount);
            invoice.TaxTotal = sales.Where(s => s.Itemcode > 0).Sum(x => x.VatAsCharged);
            invoice.GrandTotal = sales.Where(s => s.Itemcode > 0).Sum(x => x.TotalEffectiveSellingPrice);
            invoice.EffectiveDiscountPercentage = invoice.SubTotal > 0 ? invoice.DiscountTotal / invoice.SubTotal * 100 : 0;

            lblQtyTot.Text = $"{TranslationHelper.Translate("Qty")}: {sales.Where(s => s.Itemcode > 0).Sum(x => x.Quantity):F2}";
            lblSubTot.Text = $"{TranslationHelper.Translate("Sub")}: {invoice.SubTotal:C}";
            lblTaxTot.Text = $"{TranslationHelper.Translate("Tax")}: {invoice.TaxTotal:C}";
            lblRcptTot.Text = $"{TranslationHelper.Translate("Rcpt")}: {receipts.Where(r => r.ReceiptId > 0).Sum(r => r.Amount):C}";

            UpdateChange();
        }

        void UpdateChange()
        {
            double paid = ParseDouble(tbPaid.Text);
            lblChange.Text = $"{TranslationHelper.Translate("Change")}: {(paid - invoice.GrandTotal):C}";
        }

        void NewForm()
        {
            if (MessageBox.Show(this, TranslationHelper.Translate("Are you sure you want to create a new invoice? Any unsaved changes will be lost."),
                TranslationHelper.Translate("Confirm"), MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                ResetForm();
            }
        }

        void LoadForm()
        {
            var sel = BackOfficeAccounting.SearchIssuedInvoices(this);
            if (sel?.Length > 0)
            {
                long invoiceId = ParseLong(sel[0]);
                LoadData(invoiceId);
            }
        }

        void LoadData(long invoiceId)
        {
            Log($"Loading invoice with ID: {invoiceId}");
            ResetForm();
        }

        void EditForm()
        {
            tbCustomer.ReadOnly = false;
            tbSalesPerson.ReadOnly = false;
            tbCurrency.ReadOnly = false;
            dpInvoiceDate.Enabled = true;
            cbIsPosted.Enabled = true;
            taRemarks.ReadOnly = false;

            tbItem.ReadOnly = false;
            tbQty.ReadOnly = false;
            tbPrice.ReadOnly = false;
            tbDisc.ReadOnly = false;
            btnSaveSale.Enabled = true;

            tbRcptAcct.ReadOnly = false;
            tbRcptAmt.ReadOnly = false;
            btnSaveRcpt.Enabled = true;

            Log("Edit mode enabled");
        }

        void SaveForm()
        {
            var (isValid, errorList) = ValidateInputs();

            if (!isValid)
            {
                MessageBox.Show(this, errorList, TranslationHelper.Translate("Validation Error"), MessageBoxType.Error);
                return;
            }

            var data = new SalesData
            {
                Invoice = invoice,
                Sales = sales,
                Receipts = receipts,
            };

            var opts = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, opts);

            Log($"Saving invoice: {json}");

            MessageBox.Show(this, json, TranslationHelper.Translate("Invoice Data"), MessageBoxType.Information);
        }

        void CancelForm()
        {
            ResetForm();
        }

        void ResetForm()
        {
            invoice = new IssuedInvoice();

            tbCustomer.Text = tbSalesPerson.Text = tbCurrency.Text = "";
            dpInvoiceDate.Value = DateTime.Now;
            cbIsPosted.Checked = false;
            taRemarks.Text = "";

            lblCustomerName.Text = lblSalesPersonName.Text = "";

            sales = new List<Sale> { new Sale() };
            saleGrid.DataStore = null;
            saleGrid.DataStore = sales;
            ResetSaleForm();

            receipts = new List<Receipt> { new Receipt() };
            rcptGrid.DataStore = null;
            rcptGrid.DataStore = receipts;
            ResetRcptForm();

            RecalculateAll();

            Log("Form reset");
        }

        (bool IsValid, string ConsolidatedErrorList) ValidateInputs()
        {
            var errs = new List<string>();

            var customerId = ParseLong(tbCustomer.Text);
            if (customerId <= 0) errs.Add("Customer is required");

            if (sales.All(s => s.Itemcode <= 0))
                errs.Add("At least one sale item is required");

            if (receipts.All(r => r.AccountId <= 0 && r.Amount <= 0))
            {
                var paid = ParseDouble(tbPaid.Text);
                if (paid < invoice.GrandTotal)
                    errs.Add("Payment or receipt is required");
            }

            return (errs.Count == 0, string.Join(", ", errs));
        }

        long ParseLong(string txt) => long.TryParse(txt, out var v) ? v : 0;
        double ParseDouble(string txt) => double.TryParse(txt, out var v) ? v : 0;

        void Log(string message)
        {
            Console.WriteLine($"[SalesPanel] {DateTime.Now}: {message}");
        }
    }

    public class SalesData
    {
        public IssuedInvoice Invoice { get; set; }
        public List<Sale> Sales { get; set; }
        public List<Receipt> Receipts { get; set; }
    }
}