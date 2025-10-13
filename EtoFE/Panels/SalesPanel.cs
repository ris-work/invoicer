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
        List<Inventory> availableBatches = new List<Inventory>();

        // UI Controls - Invoice Information (NA fields need buttons)
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

        // UI Controls - Sales Items (NA fields need buttons)
        GridView saleGrid;
        TextBox tbItem;
        Button btnItemSearch;
        Label lblItemName;
        TextBox tbInventory;
        Button btnInventorySearch;
        Label lblInventoryName;
        TextBox tbQty;
        TextBox tbPrice;
        TextBox tbDisc;
        Button btnSaveSale;
        Button btnResetSale;

        // UI Controls - Summary
        Label lblQtyTot;
        Label lblSubTot;
        Label lblTaxTot;
        TextBox tbPaid;
        Label lblChange;

        // UI Controls - Receipts (NA fields need buttons)
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
            // Initialize with empty data
            sales = new List<Sale> { new Sale() };
            receipts = new List<Receipt> { new Receipt() };
            invoice = new IssuedInvoice();

            // Refresh data from backend
            GlobalState.RefreshBAT();
            GlobalState.RefreshPR();
        }

        void InitializeFields()
        {
            var cw = ColorSettings.ControlWidth ?? FWidth;
            var ch = ColorSettings.ControlHeight ?? FHeight;
            int hw = (int)Math.Floor(cw * 0.2);

            // Invoice Information Fields (with NA buttons)
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

            // Sales Items Fields (with NA buttons)
            saleGrid = new GridView
            {
                Width = 600,
                Height = 120,
                DataStore = sales,
            };

            // Add columns to sales grid
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Name"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.ProductName) },
            });
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Item"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.Itemcode.ToString()) },
            });
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Inventory"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.Batchcode.ToString()) },
            });
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Qty"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.Quantity.ToString("F2")) },
            });
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Price"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.SellingPrice.ToString("C2")) },
            });
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Disc%"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.DiscountRate.ToString("F2")) },
            });
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("VAT"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.VatAsCharged.ToString("C2")) },
            });
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Total"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.TotalEffectiveSellingPrice.ToString("C2")) },
            });

            tbItem = new TextBox { Width = cw, Height = ch };
            btnItemSearch = new Button { Text = "...", Height = ch, Width = hw };
            lblItemName = new Label();

            tbInventory = new TextBox { Width = cw, Height = ch };
            btnInventorySearch = new Button { Text = "...", Height = ch, Width = hw };
            lblInventoryName = new Label();

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

            // Receipts Fields (with NA button)
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

            // Setup navigation
            SetupNavigation();
        }

        void SetupNavigation()
        {
            // Define NA fields in order - these are the fields that need search
            naFieldsInOrder = new List<TextBox>
            {
                tbCustomer, tbSalesPerson, tbItem, tbInventory, tbRcptAcct
            };

            naFieldIndex = naFieldsInOrder.Select((field, index) => new { field, index })
                .ToDictionary(x => x.field, x => x.index);

            // Map NA textboxes to their search buttons
            naFieldMap = new Dictionary<TextBox, Button>
            {
                { tbCustomer, btnCustomerSearch },
                { tbSalesPerson, btnSalesPersonSearch },
                { tbItem, btnItemSearch },
                { tbInventory, btnInventorySearch },
                { tbRcptAcct, btnRcptAcctSearch }
            };

            naButtonToTextBoxMap = naFieldMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            // Define essential controls for Enter key navigation
            essentialControls = new List<Control>
            {
                tbCustomer, btnCustomerSearch, tbSalesPerson, btnSalesPersonSearch,
                tbCurrency, dpInvoiceDate, cbIsPosted, taRemarks,
                tbItem, btnItemSearch, tbInventory, btnInventorySearch,
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
                (
                    TranslationHelper.Translate("Inventory:"),
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items = { tbInventory, btnInventorySearch, lblInventoryName },
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
                        Content = new StackLayout { Spacing = 5, Items = { saleGrid, saleForm } },
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
            btnInventorySearch.Click += (_, __) => HandleNASearchWithParam(tbInventory, lblInventoryName,
                () => BackOfficeAccounting.SearchBatches(this, ParseLong(tbItem.Text)));
            btnRcptAcctSearch.Click += (_, __) => HandleNASearch(tbRcptAcct, lblAcctName,
                BackOfficeAccounting.SearchAccounts, BackOfficeAccounting.LookupAccount);

            // Text change events - validate before lookup
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
                if (id > 0) LoadAvailableBatches();
            };
            tbInventory.TextChanged += (_, __) =>
            {
                var itemId = ParseLong(tbItem.Text);
                var batchId = ParseLong(tbInventory.Text);
                lblInventoryName.Text = (itemId > 0 && batchId > 0) ?
                    BackOfficeAccounting.LookupBatch(itemId, batchId) : "";
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

        void HandleNASearchWithParam(TextBox textBox, Label label, Func<string[]> searchFunc)
        {
            var sel = searchFunc();
            if (sel?.Length > 0 && long.TryParse(sel[0], out var id))
            {
                textBox.Text = id.ToString();
                var itemId = ParseLong(tbItem.Text);
                label.Text = itemId > 0 ? BackOfficeAccounting.LookupBatch(itemId, id) : "";
                MoveToNextNAField(textBox);
            }
        }

        void WireKeyNavigation()
        {
            // KeyDown for Enter key navigation
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

            // KeyUp for F-keys
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

        void LoadAvailableBatches()
        {
            long itemCode = ParseLong(tbItem.Text);
            if (itemCode <= 0)
            {
                availableBatches.Clear();
                return;
            }

            availableBatches = GlobalState.BAT.Inv
                .Where(b => b.Itemcode == itemCode)
                .OrderBy(b => b.ExpDate ?? DateTime.MaxValue)
                .ToList();
        }

        void AddSale()
        {
            if (!ValidateSaleForm())
                return;

            long itemCode = ParseLong(tbItem.Text);
            double quantity = ParseDouble(tbQty.Text);

            // Get available batches for this item
            LoadAvailableBatches();

            if (availableBatches.Count == 0)
            {
                MessageBox.Show(this, "No available batches for this item", "Error", MessageBoxType.Error);
                return;
            }

            // If there's only one batch, use it directly
            if (availableBatches.Count == 1)
            {
                var batch = availableBatches[0];
                tbInventory.Text = batch.Batchcode.ToString();
                AddSaleWithBatch(itemCode, batch.Batchcode, quantity);
                return;
            }

            // If there are multiple batches, select them automatically based on quantity
            List<(long batchCode, double quantity)> selectedBatches = new List<(long, double)>();
            double remainingQuantity = quantity;

            foreach (var batch in availableBatches)
            {
                if (remainingQuantity <= 0)
                    break;

                double batchQuantity = Math.Min(remainingQuantity, batch.Units);
                selectedBatches.Add((batch.Batchcode, batchQuantity));
                remainingQuantity -= batchQuantity;
            }

            if (remainingQuantity > 0)
            {
                MessageBox.Show(this, $"Not enough inventory. Available: {quantity - remainingQuantity}, Required: {quantity}", "Error", MessageBoxType.Error);
                return;
            }

            // Add sales for each selected batch
            foreach (var (batchCode, batchQuantity) in selectedBatches)
            {
                AddSaleWithBatch(itemCode, batchCode, batchQuantity);
            }

            ResetSaleForm();
        }

        void AddSaleWithBatch(long itemCode, long batchCode, double quantity)
        {
            double price = ParseDouble(tbPrice.Text);
            double discountRate = ParseDouble(tbDisc.Text);

            var batch = GlobalState.BAT.Inv.FirstOrDefault(b => b.Itemcode == itemCode && b.Batchcode == batchCode);
            if (batch == null) return;

            var item = GlobalState.BAT.Cat.FirstOrDefault(c => c.Itemcode == itemCode);
            if (item == null) return;

            var vatCategory = GlobalState.BAT.VCat.FirstOrDefault(v => v.VatCategoryId == item.DefaultVatCategory);
            double vatRate = vatCategory?.VatPercentage ?? 0;

            var sale = new Sale
            {
                Itemcode = itemCode,
                Batchcode = batchCode,
                Quantity = quantity,
                SellingPrice = price,
                DiscountRate = discountRate,
                ProductName = item.Description,
                Discount = quantity * price * discountRate / 100,
                VatRatePercentage = vatRate
            };

            sale.VatAsCharged = (quantity * price - sale.Discount) * vatRate / 100;
            sale.TotalEffectiveSellingPrice = quantity * price - sale.Discount + sale.VatAsCharged;

            sales.Add(sale);
            saleGrid.DataStore = null;
            saleGrid.DataStore = sales;
            RecalculateAll();
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

            if (!double.TryParse(tbQty.Text, out double qty) || qty <= 0)
            {
                MessageBox.Show(this, "Please enter a valid quantity", "Validation Error", MessageBoxType.Error);
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
            tbItem.Text = tbInventory.Text = tbQty.Text = tbPrice.Text = tbDisc.Text = "";
            lblItemName.Text = lblInventoryName.Text = "";
            availableBatches.Clear();
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
            // Update invoice data
            invoice.Customer = ParseLong(tbCustomer.Text);
            invoice.SalesPersonId = ParseLong(tbSalesPerson.Text);
            invoice.CurrencyCode = tbCurrency.Text;
            invoice.InvoiceTime = dpInvoiceDate.Value ?? DateTime.Now;
            invoice.IsPosted = cbIsPosted.Checked == true;
            invoice.InvoiceHumanFriendly = taRemarks.Text;

            // Calculate totals
            invoice.SubTotal = sales.Where(s => s.Itemcode > 0).Sum(x => x.Quantity * x.SellingPrice);
            invoice.DiscountTotal = sales.Where(s => s.Itemcode > 0).Sum(x => x.Discount);
            invoice.TaxTotal = sales.Where(s => s.Itemcode > 0).Sum(x => x.VatAsCharged);
            invoice.GrandTotal = sales.Where(s => s.Itemcode > 0).Sum(x => x.TotalEffectiveSellingPrice);
            invoice.EffectiveDiscountPercentage = invoice.SubTotal > 0 ? invoice.DiscountTotal / invoice.SubTotal * 100 : 0;

            // Update summary labels
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

            // Reset form with default values
            ResetForm();

            // In a real implementation, you would load the invoice data here
            // and populate the form fields
        }

        void EditForm()
        {
            // Enable editing
            tbCustomer.ReadOnly = false;
            tbSalesPerson.ReadOnly = false;
            tbCurrency.ReadOnly = false;
            dpInvoiceDate.Enabled = true;
            cbIsPosted.Enabled = true;
            taRemarks.ReadOnly = false;

            // Enable sales and receipts editing
            tbItem.ReadOnly = false;
            tbInventory.ReadOnly = false;
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

            // In a real implementation, you would send this data to the backend
        }

        void CancelForm()
        {
            // Reset to the last saved state
            // For now, just reset the form
            ResetForm();
        }

        void ResetForm()
        {
            // Reset invoice data
            invoice = new IssuedInvoice();

            // Reset form fields
            tbCustomer.Text = tbSalesPerson.Text = tbCurrency.Text = "";
            dpInvoiceDate.Value = DateTime.Now;
            cbIsPosted.Checked = false;
            taRemarks.Text = "";

            // Reset labels
            lblCustomerName.Text = lblSalesPersonName.Text = "";

            // Reset sales
            sales = new List<Sale> { new Sale() };
            saleGrid.DataStore = null;
            saleGrid.DataStore = sales;
            ResetSaleForm();

            // Reset receipts
            receipts = new List<Receipt> { new Receipt() };
            rcptGrid.DataStore = null;
            rcptGrid.DataStore = receipts;
            ResetRcptForm();

            // Reset summary
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