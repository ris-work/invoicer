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

        // Payment type constants
        private class PaymentType
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public Func<string> GetDefaultAccount { get; set; }

            public PaymentType(string key, string name, Func<string> getDefaultAccount)
            {
                Key = key;
                Name = name;
                GetDefaultAccount = getDefaultAccount;
            }
        }

        private static readonly List<PaymentType> PaymentTypes = new List<PaymentType>
        {
            new PaymentType("CASH", "Cash", () => $"CASH ${GlobalState.Terminal}"),
            new PaymentType("BANK", "Card/Bank", () => $"BANK ${GlobalState.Terminal}"),
            new PaymentType("LOYALTY", "Loyalty Points", () => "LOYALTY ACCOUNT"),
            new PaymentType("OTHER", "Other", () => "OTHER PAYMENT")
        };

        // Data storage
        List<Sale> sales;
        List<Receipt> receipts;
        IssuedInvoice invoice;

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

        // UI Controls - Summary
        Label lblQtyTot;
        Label lblSubTot;
        Label lblTaxTot;
        TextBox tbPaid;
        Label lblChange;

        // UI Controls - Receipts
        GridView rcptGrid;
        RadioButtonList rblPaymentType;
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
        List<Control> naFieldsInOrder;
        Dictionary<Control, int> naFieldIndex;
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
            sales = new List<Sale>();
            receipts = new List<Receipt>();
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
                HeaderText = TranslationHelper.Translate("Type"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Receipt, string>(r => GetPaymentTypeFromAccount(r.AccountId)) },
                Width = 100
            });
            rcptGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Account"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Receipt, string>(r => r.AccountId.ToString()) },
                Width = 100
            });
            rcptGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Account Name"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Receipt, string>(r => BackOfficeAccounting.LookupAccount(r.AccountId)) },
                Width = 150
            });
            rcptGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Amount"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Receipt, string>(r => r.Amount.ToString("C2")) },
                Width = 100
            });

            // Payment type selection
            rblPaymentType = new RadioButtonList
            {
                Orientation = Orientation.Horizontal,
                Spacing = new Eto.Drawing.Size(10, 10),
                Width = 400
            };

            rblPaymentType.ItemTextBinding = Binding.Delegate<PaymentType, string>(pt => pt.Name);
            rblPaymentType.ItemKeyBinding = Binding.Delegate<PaymentType, string>(pt => pt.Key);
            rblPaymentType.DataStore = PaymentTypes;
            rblPaymentType.SelectedIndex = 0; // Default to Cash

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
            // Include RadioButtonList in navigation
            naFieldsInOrder = new List<Control>
            {
                tbCustomer, btnCustomerSearch, tbSalesPerson, btnSalesPersonSearch, rblPaymentType,
                tbRcptAcct, btnRcptAcctSearch
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
                tbPaid, rblPaymentType, tbRcptAcct, btnRcptAcctSearch, tbRcptAmt, btnSaveRcpt, btnResetRcpt,
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
            var rcptForm = new StackLayout
            {
                Spacing = 5,
                Items =
                {
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items = { new Label { Text = "Payment Type:" }, rblPaymentType }
                    },
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items = {
                            new Label { Text = TranslationHelper.Translate("Acct:") },
                            tbRcptAcct,
                            btnRcptAcctSearch,
                            lblAcctName
                        },
                    },
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items = {
                            new Label { Text = TranslationHelper.Translate("Amt:") },
                            tbRcptAmt,
                            btnSaveRcpt,
                            btnResetRcpt
                        }
                    }
                }
            };

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
                            Items = { saleGrid, saleForm }
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

            // Payment type selection change
            rblPaymentType.SelectedIndexChanged += (s, e) =>
            {
                if (rblPaymentType.SelectedValue is PaymentType selectedType)
                {
                    if (selectedType.Key == "CASH" || selectedType.Key == "BANK")
                    {
                        var defaultAccountName = selectedType.GetDefaultAccount();
                        var accountId = BackOfficeAccounting.LookupAccountByName(defaultAccountName);
                        if (accountId > 0)
                        {
                            tbRcptAcct.Text = accountId.ToString();
                            lblAcctName.Text = defaultAccountName;
                            tbRcptAcct.ReadOnly = true;
                            btnRcptAcctSearch.Enabled = false;
                        }
                    }
                    else
                    {
                        tbRcptAcct.Text = "";
                        lblAcctName.Text = "";
                        tbRcptAcct.ReadOnly = false;
                        btnRcptAcctSearch.Enabled = true;
                    }
                }
            };

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
                    else if (lastFocused is RadioButtonList)
                    {
                        // Handle Enter on RadioButtonList - move to next control
                        MoveToNextEssentialControl();
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

        void MoveToNextNAField(Control currentField)
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

            // Get batches using SearchDialogEto
            var selectedBatchData = SelectBatchesWithSearchDialog(itemCode, quantity);
            if (selectedBatchData == null || selectedBatchData.Count == 0)
                return;

            // Calculate total available
            double totalAvailable = selectedBatchData.Sum(b => double.Parse(b[2])); // Units is at index 2
            if (quantity > totalAvailable)
            {
                MessageBox.Show(this, $"Not enough inventory. Available: {totalAvailable}, Required: {quantity}", "Error", MessageBoxType.Error);
                return;
            }

            // Distribute quantity across selected batches in order
            double remainingQuantity = quantity;
            var item = GlobalState.BAT.Cat.FirstOrDefault(c => c.Itemcode == itemCode);
            if (item == null) return;

            var vatCategory = GlobalState.BAT.VCat.FirstOrDefault(v => v.VatCategoryId == item.DefaultVatCategory);
            double vatRate = vatCategory?.VatPercentage ?? 0;

            double price = ParseDouble(tbPrice.Text);
            double discountRate = ParseDouble(tbDisc.Text);

            foreach (var batchData in selectedBatchData)
            {
                if (remainingQuantity <= 0)
                    break;

                long batchCode = long.Parse(batchData[1]); // Batchcode at index 1
                double batchQuantity = Math.Min(remainingQuantity, double.Parse(batchData[2])); // Units at index 2

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
                remainingQuantity -= batchQuantity;
            }

            RefreshSalesGrid();
            RecalculateAll();
            ResetSaleForm();
        }

        List<string[]> SelectBatchesWithSearchDialog(long itemCode, double requiredQuantity)
        {
            var batches = GlobalState.BAT.Inv.Where(b => b.Itemcode == itemCode).ToList();
            if (batches.Count == 0)
            {
                MessageBox.Show(this, "No available batches for this item", "Warning", MessageBoxType.Warning);
                return null;
            }

            // Prepare data for SearchDialogEto
            var searchItems = batches.Select(b => new[]
            {
                b.Itemcode.ToString(),
                b.Batchcode.ToString(),
                b.Units.ToString("F2"),
                b.MfgDate?.ToString("yyyy-MM-dd") ?? "",
                b.ExpDate?.ToString("yyyy-MM-dd") ?? "",
                //b.Cost?.ToString("F2") ?? ""
            }).ToList();

            var headerEntries = new List<(string Title, TextAlignment Alignment, bool)>
            {
                ("Itemcode", TextAlignment.Left, true),
                ("Batchcode", TextAlignment.Left, true),
                ("Units", TextAlignment.Right, true),
                ("MFG Date", TextAlignment.Left, false),
                ("Expiry Date", TextAlignment.Left, true),
                //("Cost", TextAlignment.Right, false)
            };
            var searchItemsWithColors = searchItems
    .Select(row => (Data: row, ForegroundColor: (Eto.Drawing.Color?)null, BackgroundColor: (Eto.Drawing.Color?)null))
    .ToList();

            var dialog = new SearchDialogEto(
                searchItemsWithColors,
                headerEntries,
                Debug: false
            );

            dialog.Title = $"Select Batches for {BackOfficeAccounting.LookupItem(itemCode)} (Required: {requiredQuantity:F2})";
            dialog.ReportSelectedButtonText = "Use Selected Order";
            dialog.CallbackWhenReportButtonIsClicked = (message, selected) =>
            {
                // The OutputList contains order from user selection
                var orderedBatchData = dialog.OutputList.ToList();
                dialog.Close();
            };

            dialog.ShowModal(this);

            // OutputList has the batches in the order selected by the user
            return dialog.OutputList.ToList();
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
            tbItem.Text = tbQty.Text = tbPrice.Text = tbDisc.Text = "";
            lblItemName.Text = "";
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
            RefreshReceiptsGrid();
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
            // Reset to default payment type (Cash)
            rblPaymentType.SelectedIndex = 0;
            tbRcptAmt.Text = "";

            // The account will be auto-filled by the SelectedIndexChanged event
        }

        void RefreshSalesGrid()
        {
            saleGrid.DataStore = null;
            saleGrid.DataStore = sales;
            saleGrid.Invalidate(true);
        }

        void RefreshReceiptsGrid()
        {
            rcptGrid.DataStore = null;
            rcptGrid.DataStore = receipts;
            rcptGrid.Invalidate(true);
        }

        string GetPaymentTypeFromAccount(long accountId)
        {
            var accountName = BackOfficeAccounting.LookupAccount(accountId);

            if (accountName.StartsWith("CASH"))
                return "Cash";
            if (accountName.StartsWith("BANK"))
                return "Card/Bank";
            if (accountName.Contains("LOYALTY"))
                return "Loyalty";

            return "Other";
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
            lblRcptTot.Text = $"{TranslationHelper.Translate("Rcpt")}: {receipts.Sum(r => r.Amount):C}";

            // Update paid field to match total receipts
            tbPaid.Text = receipts.Sum(r => r.Amount).ToString("F2");

            UpdateChange();
        }

        void UpdateChange()
        {
            double paid = receipts.Sum(r => r.Amount); // Use actual receipts total
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

            // Add cash receipt if paid field has value and no matching cash receipt exists
            double paidFromField = ParseDouble(tbPaid.Text);
            double totalReceipts = receipts.Sum(r => r.Amount);

            if (paidFromField > 0 && Math.Abs(paidFromField - totalReceipts) > 0.01)
            {
                var cashAccountName = $"CASH ${GlobalState.Terminal}";
                var cashAccountId = BackOfficeAccounting.LookupAccountByName(cashAccountName);

                if (cashAccountId > 0)
                {
                    receipts.Add(new Receipt
                    {
                        InvoiceId = invoice.InvoiceId,
                        AccountId = cashAccountId,
                        Amount = paidFromField - totalReceipts,
                        TimeReceived = DateTimeOffset.Now,
                    });
                    RefreshReceiptsGrid();
                }
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

            sales = new List<Sale>();
            RefreshSalesGrid();
            ResetSaleForm();

            receipts = new List<Receipt>();
            RefreshReceiptsGrid();
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

            if (receipts.Count == 0 || receipts.Sum(r => r.Amount) < invoice.GrandTotal)
                errs.Add("Receipt amount must cover the invoice total");

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