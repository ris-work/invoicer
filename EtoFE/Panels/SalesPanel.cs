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
        // Add these fields to the class
        private bool isProcessingBatchSelection = false;
        private bool itemCodeChanged = false;
        // Add this flag at class level
        private bool autoAddWithBatchPrices = false;


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

        // Class to hold batch info for the current item being entered
        private class SelectedBatch
        {
            public long Batchcode { get; set; }
            public double AvailableUnits { get; set; }
            public string MfgDate { get; set; }
            public string ExpDate { get; set; }
        }

        // Data storage
        List<Sale> sales;
        List<Receipt> receipts;
        IssuedInvoice invoice;
        List<SelectedBatch> selectedBatchesForCurrentItem;

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
        // UI Controls - Sales Items
        GridView saleGrid;
        GridView selectedBatchesGrid; // New grid for selected batches
        Label lblBatchSufficiency; // NEW: Label to show if batches are sufficient
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
        Button btnTempSave;

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
            isProcessingBatchSelection = false;
            itemCodeChanged = false;

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
            selectedBatchesForCurrentItem = new List<SelectedBatch>();

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

            // Selected Batches Grid (NEW)
            selectedBatchesGrid = new GridView
            {
                Width = 600,
                Height = 80,
                DataStore = selectedBatchesForCurrentItem,
                AllowMultipleSelection = false,
            };
            selectedBatchesGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Batchcode"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<SelectedBatch, string>(b => b.Batchcode.ToString()) },
                Width = 100
            });
            selectedBatchesGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Available Units"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<SelectedBatch, string>(b => b.AvailableUnits.ToString("F2")) },
                Width = 120,
                HeaderTextAlignment = TextAlignment.Right
            });
            selectedBatchesGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("MFG Date"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<SelectedBatch, string>(b => b.MfgDate) },
                Width = 100
            });
            selectedBatchesGrid.Columns.Add(new GridColumn
            {
                HeaderText = TranslationHelper.Translate("Exp Date"),
                DataCell = new TextBoxCell { Binding = Binding.Delegate<SelectedBatch, string>(b => b.ExpDate) },
                Width = 100
            });

            // NEW: Batch sufficiency label
            lblBatchSufficiency = new Label
            {
                Text = "",
                TextColor = Colors.Red,
                Font = SystemFonts.Default(10f)
            };

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
            btnTempSave = new Button { Text = TranslationHelper.Translate("Temp Save"), Width = cw, Height = ch };
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
                new StackLayoutItem(btnReset),
                new StackLayoutItem(btnTempSave)  // Add here
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
                Height = ColorSettings.MaxControlWidth ?? 900, // Increased height for new grid
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
                                new StackLayout
                                {
                                    Orientation = Orientation.Vertical,
                                    Spacing = 5,
                                    Items = {
                                        new GroupBox { Text = "Selected Batches for Item", Content = selectedBatchesGrid },
                                        lblBatchSufficiency  // NEW: Add the sufficiency label
                                    }
                                }, 
                                saleForm 
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
            btnTempSave.Click += (_, __) => TempSaveForm();

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
            btnItemSearch.Click += (_, __) => {
                HandleNASearch(tbItem, lblItemName,
                    BackOfficeAccounting.SearchItems, BackOfficeAccounting.LookupItem);
                HandleItemCodeComplete(); // Trigger now instead of on every change
            };
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
            //tbItem.LostFocus += (_, __) => HandleItemCodeComplete();
            // Add TextChanged for tbItem to track when item code changes
            tbItem.TextChanged += (_, __) =>
            {
                Log($"tbItem.TextChanged: New value = '{tbItem.Text}'");
                itemCodeChanged = true;
            };
            tbPrice.TextChanged += (_, __) =>
            {
                Log($"tbPrice.TextChanged: New value = '{tbPrice.Text}'");
                tbPrice_TextChanged(_, __);
            };
            // Add LostFocus for item code
            tbItem.LostFocus += (_, __) =>
            {
                Log($"tbItem.LostFocus triggered. Current value: '{tbItem.Text}', isProcessingBatchSelection: {isProcessingBatchSelection}, itemCodeChanged: {itemCodeChanged}");

                // Only process if we're not in the middle of batch selection and item code has actually changed
                if (!isProcessingBatchSelection && itemCodeChanged)
                {
                    HandleItemCodeComplete();
                    itemCodeChanged = false; // Reset the flag
                }
            };
            tbRcptAcct.TextChanged += (_, __) =>
            {
                var id = ParseLong(tbRcptAcct.Text);
                lblAcctName.Text = id > 0 ? BackOfficeAccounting.LookupAccount(id) : "";
            };
            tbPaid.TextChanged += (_, __) => UpdateChange();
            tbQty.TextChanged += (_, __) => UpdateBatchSufficiencyLabel();

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
            Log($"HandleNASearch called for {nameof(textBox)}");

            // Store state BEFORE search
            string originalText = textBox.Text;

            // Perform the lookup (this opens modal dialog)
            bool lookupSuccess = DoLookup(textBox, label, searchFunc, lookupFunc);
            Log($"HandleNASearch: After search - Success: {lookupSuccess}, Text: '{textBox.Text}'");

            // Check validity AFTER search
            bool isValidAfter = IsNAFieldValid(textBox, label);
            Log($"HandleNASearch: After search - Valid: {isValidAfter}");

            // Decide focus based on the result
            if (lookupSuccess && isValidAfter)
            {
                Log("HandleNASearch: Lookup successful and valid - moving to next NA field");
                MoveToNextNAField(textBox);
            }
            else if (!lookupSuccess)
            {
                Log("HandleNASearch: Lookup failed - focusing textbox");
                textBox.Focus();
            }
            else if (!isValidAfter)
            {
                Log("HandleNASearch: Lookup successful but invalid - focusing textbox");
                textBox.Focus();
            }
            else
            {
                Log("HandleNASearch: No action taken");
            }
        }

        // Helper method to check if an NA field is valid
        private bool IsNAFieldValid(TextBox textBox, Label humanLabel)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
                return false;

            string humanText = humanLabel.Text ?? "";
            return !humanText.ToLowerInvariant().Contains("unknown");
        }

        // Helper method to perform the lookup
        bool DoLookup(TextBox tb, Label human, Func<Control, string[]> search, Func<long, string> lookup)
        {
            var sel = search(this);
            if (sel?.Length > 0 && long.TryParse(sel[0], out var id))
            {
                tb.Text = id.ToString();
                human.Text = lookup(id) ?? "";
                return true; // Lookup successful
            }
            return false; // Lookup failed or cancelled
        }

        // First, let's see what properties are available on the batch object
        // Assuming GlobalState.BAT.Inv contains batch objects with price info

        void HandleItemCodeComplete()
        {
            Log($"HandleItemCodeComplete called with tbItem.Text = '{tbItem.Text}'");

            var itemCode = ParseLong(tbItem.Text);
            lblItemName.Text = itemCode > 0 ? BackOfficeAccounting.LookupItem(itemCode) : "";

            // Clear existing selections before populating
            selectedBatchesForCurrentItem.Clear();
            RefreshSelectedBatchesGrid();

            if (itemCode > 0)
            {
                isProcessingBatchSelection = true;
                Log($"Setting isProcessingBatchSelection = true");
            }

            try
            {
                PopulateSelectedBatchesGrid(itemCode);

                // Auto-select price if empty and we have batches selected
                if (selectedBatchesForCurrentItem.Count > 0 && (string.IsNullOrEmpty(tbPrice.Text) || ParseDouble(tbPrice.Text) <= 0))
                {
                    AutoSelectPrice(itemCode);
                }
            }
            finally
            {
                isProcessingBatchSelection = false;
                Log($"Resetting isProcessingBatchSelection = false");
            }
        }

        void AutoSelectPrice(long itemCode)
        {
            Log("AutoSelectPrice called - START");
            Log($"  tbPrice.Text before: '{tbPrice.Text}'");
            Log($"  selectedBatchesForCurrentItem.Count: {selectedBatchesForCurrentItem.Count}");

            if (selectedBatchesForCurrentItem.Count == 0)
            {
                Log("No batches selected, returning");
                return;
            }

            // Get the actual batch objects to check prices
            var batchObjects = GlobalState.BAT.Inv
                .Where(b => selectedBatchesForCurrentItem.Any(sb => sb.Batchcode == b.Batchcode))
                .ToList();

            Log($"Found {batchObjects.Count} batch objects");

            if (batchObjects.Count > 0)
            {
                var firstBatch = batchObjects.First();
                Log($"First batch details:");
                Log($"  Batchcode: {firstBatch.Batchcode}");
                Log($"  Units: {firstBatch.Units}");
                Log($"  SellingPrice: {firstBatch.SellingPrice}");

                // Check if selected batches have different prices
                var batchPrices = batchObjects.Select(b => b.SellingPrice).Where(p => p > 0).Distinct().ToList();

                if (batchPrices.Count > 1)
                {
                    Log($"Warning: Batches have different prices: {string.Join(", ", batchPrices)}");
                    // Don't auto-select, let user choose
                    return;
                }

                // Use the common price from first batch
                if (firstBatch.SellingPrice > 0)
                {
                    tbPrice.Text = firstBatch.SellingPrice.ToString("F2");
                    Log($"Auto-selected price: {firstBatch.SellingPrice} -> tbPrice.Text = '{tbPrice.Text}'");
                }
                else
                {
                    Log("No selling price found on first selected batch");

                    // Check other selected batches
                    var pricedBatch = batchObjects.FirstOrDefault(b => b.SellingPrice > 0);
                    if (pricedBatch != null)
                    {
                        tbPrice.Text = pricedBatch.SellingPrice.ToString("F2");
                        Log($"Found price on another batch: {pricedBatch.Batchcode} -> {pricedBatch.SellingPrice}");
                    }
                }
            }

            Log("AutoSelectPrice called - END");
        }








        void HandleItemCodeChange()
        {
            var itemCode = ParseLong(tbItem.Text);
            lblItemName.Text = itemCode > 0 ? BackOfficeAccounting.LookupItem(itemCode) : "";

            PopulateSelectedBatchesGrid(itemCode);
        }


        void PopulateSelectedBatchesGrid(long itemCode)
        {
            Log($"PopulateSelectedBatchesGrid called with itemCode = {itemCode}");

            selectedBatchesForCurrentItem.Clear();
            RefreshSelectedBatchesGrid();

            if (itemCode <= 0)
            {
                Log("Item code <= 0, returning");
                return;
            }

            var availableBatches = GlobalState.BAT.Inv.Where(b => b.Itemcode == itemCode && b.Units > 0).ToList();
            Log($"Found {availableBatches.Count} available batches for item {itemCode}");

            if (availableBatches.Count == 0)
            {
                MessageBox.Show(this, "No available batches for this item", "Warning", MessageBoxType.Warning);
                return;
            }

            if (availableBatches.Count == 1)
            {
                Log("Only one batch available, auto-selecting");
                var batch = availableBatches.First();
                selectedBatchesForCurrentItem.Add(new SelectedBatch
                {
                    Batchcode = batch.Batchcode,
                    AvailableUnits = batch.Units,
                    MfgDate = batch.MfgDate?.ToString("yyyy-MM-dd") ?? "",
                    ExpDate = batch.ExpDate?.ToString("yyyy-MM-dd") ?? ""
                });
                RefreshSelectedBatchesGrid();
                UpdateBatchSufficiencyLabel();
            }
            else
            {
                Log("Multiple batches available, showing selection dialog");
                // Multiple batches, show selection dialog
                SelectBatchesWithSearchDialog(itemCode);
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

                    // Smart NA field navigation
                    if (lastFocused is TextBox focusedTextBox && naFieldMap.TryGetValue(focusedTextBox, out var associatedButton))
                    {
                        Console.WriteLine($"KeyDown: TextBox -> Button focus: {focusedTextBox.ID} -> {associatedButton.Text}");
                        associatedButton.Focus();
                        e.Handled = true;
                    }
                    else if (lastFocused is Button focusedButton && naButtonToTextBoxMap.TryGetValue(focusedButton, out var naTextBox))
                    {
                        Console.WriteLine($"KeyDown: Button click triggered: {focusedButton.Text} for {naTextBox.ID}");
                        // Don't use AsyncInvoke here - trigger immediately
                        focusedButton.PerformClick();
                        e.Handled = true;
                    }
                    else if (lastFocused is TextBox && naFieldIndex.ContainsKey(lastFocused as TextBox))
                    {
                        // If we're on an NA field textbox but not the one that triggered the button
                        Console.WriteLine("KeyDown: Moving to next NA field");
                        MoveToNextNAField(lastFocused as TextBox);
                        e.Handled = true;
                    }
                    else
                    {
                        Console.WriteLine("KeyDown: Regular field navigation");
                        MoveToNextEssentialControl();
                        e.Handled = true;
                    }

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

        private void MoveToNextNAField(TextBox currentField)
        {
            if (!naFieldIndex.TryGetValue(currentField, out int currentIndex))
            {
                Console.WriteLine($"MoveToNextNAField: Current field not found in NA fields list");
                return;
            }

            int nextIndex = (currentIndex + 1) % naFieldsInOrder.Count;
            var nextField = naFieldsInOrder[nextIndex];

            Console.WriteLine($"MoveToNextNAField: Moving from {currentField.ID} to {nextField.ID}");
            nextField.Focus();
        }

        private void MoveToNextEssentialControl()
        {
            var currentIndex = essentialControls.IndexOf(lastFocused);
            Console.WriteLine($"MoveToNextEssentialControl: currentIndex = {currentIndex}, lastFocused = {lastFocused?.GetType().Name}");

            if (currentIndex > -1 && currentIndex < essentialControls.Count - 1)
            {
                var nextControl = essentialControls[currentIndex + 1];
                Console.WriteLine($"MoveToNextEssentialControl: Next control = {nextControl?.GetType().Name}");
                nextControl.Focus();
            }
            else if (currentIndex == essentialControls.Count - 1)
            {
                Console.WriteLine("MoveToNextEssentialControl: Already at last control");
                // Stay on Save button or wrap around to first
                essentialControls[0].Focus();
            }
            else
            {
                Console.WriteLine("MoveToNextEssentialControl: Current control not in list, focusing first");
                essentialControls[0].Focus();
            }
        }

        void AddSale()
        {
            if (!ValidateSaleForm())
                return;

            var itemCode = ParseLong(tbItem.Text);
            double quantity = ParseDouble(tbQty.Text);
            double discountRate = ParseDouble(tbDisc.Text);

            // Check against pre-selected batches
            double totalAvailable = selectedBatchesForCurrentItem.Sum(b => b.AvailableUnits);
            if (quantity > totalAvailable)
            {
                MessageBox.Show(this, $"Not enough inventory. Available: {totalAvailable:F2}, Required: {quantity:F2}", "Error", MessageBoxType.Error);
                return;
            }

            var item = GlobalState.BAT.Cat.FirstOrDefault(c => c.Itemcode == itemCode);
            if (item == null) return;

            var vatCategory = GlobalState.BAT.VCat.FirstOrDefault(v => v.VatCategoryId == item.DefaultVatCategory, new VatCategory() { Active = true, VatCategoryId = 0, VatName = "General Untaxed", VatPercentage = 0 });
            double vatRate = vatCategory?.VatPercentage ?? 0;

            // Get batch objects to use their individual prices
            var batchObjects = GlobalState.BAT.Inv
                .Where(b => selectedBatchesForCurrentItem.Any(sb => sb.Batchcode == b.Batchcode))
                .ToDictionary(b => b.Batchcode, b => b);

            // Distribute quantity across selected batches in order
            double remainingQuantity = quantity;
            foreach (var selectedBatch in selectedBatchesForCurrentItem)
            {
                if (remainingQuantity <= 0) break;

                double batchQuantity = Math.Min(remainingQuantity, selectedBatch.AvailableUnits);

                if (!batchObjects.TryGetValue(selectedBatch.Batchcode, out var batchObj))
                {
                    Log($"ERROR: Could not find batch object for batchcode {selectedBatch.Batchcode}");
                    continue;
                }

                // ALWAYS use batch-specific price
                double batchPrice = batchObj.SellingPrice;
                if (batchPrice <= 0)
                {
                    if (autoAddWithBatchPrices)
                    {
                        // Skip batches without price in auto-add mode
                        Log($"WARNING: Batch {selectedBatch.Batchcode} has no SellingPrice, skipping in auto-add mode");
                        continue;
                    }
                    else
                    {
                        Log($"WARNING: Batch {selectedBatch.Batchcode} has no SellingPrice, using user-entered price {tbPrice.Text}");
                        batchPrice = ParseDouble(tbPrice.Text);
                    }
                }
                else
                {
                    Log($"Using batch-specific price for batch {selectedBatch.Batchcode}: {batchPrice}");
                }

                var sale = new Sale
                {
                    Itemcode = itemCode,
                    Batchcode = selectedBatch.Batchcode,
                    Quantity = batchQuantity,
                    SellingPrice = batchPrice,
                    DiscountRate = discountRate,
                    ProductName = item.Description,
                    Discount = batchQuantity * batchPrice * discountRate / 100,
                    VatRatePercentage = vatRate
                };

                sale.VatAsCharged = (batchQuantity * batchPrice - sale.Discount) * vatRate / 100;
                sale.TotalEffectiveSellingPrice = batchQuantity * batchPrice - sale.Discount + sale.VatAsCharged;

                sales.Add(sale);
                remainingQuantity -= batchQuantity;

                Log($"Added sale: {batchQuantity} units from batch {selectedBatch.Batchcode} at price {batchPrice}");
            }

            // Check if we met the quantity requirement
            if (remainingQuantity > 0)
            {
                MessageBox.Show(
                    $"Could not fulfill complete quantity. Only {quantity - remainingQuantity:F2} of {quantity:F2} units were added (some batches have no price).",
                    "Incomplete",
                    MessageBoxType.Warning);
            }

            // Remove auto-added sales from the list if incomplete
            if (remainingQuantity > 0 && autoAddWithBatchPrices)
            {
                // Remove the partially added sales
                var lastSaleCount = selectedBatchesForCurrentItem.Count(sb =>
                    sales.Any(s => s.Batchcode == sb.Batchcode && s.Itemcode == itemCode));

                for (int i = sales.Count - 1; i >= sales.Count - lastSaleCount; i--)
                {
                    sales.RemoveAt(i);
                }

                MessageBox.Show(
                    $"Cannot auto-add: Some selected batches have no selling price. Please set prices for all batches or manually enter a price.",
                    "Cannot Auto-Add",
                    MessageBoxType.Error);
            }
            else
            {
                RefreshSalesGrid();
                RecalculateAll();
                ResetSaleForm();
            }

            autoAddWithBatchPrices = false; // Reset the flag
        }


        void SelectBatchesWithSearchDialog(long itemCode)
        {
            Log($"SelectBatchesWithSearchDialog called for item {itemCode}");

            var batches = GlobalState.BAT.Inv.Where(b => b.Itemcode == itemCode && b.Units > 0).ToList();
            Log($"Preparing dialog with {batches.Count} batches");

            // Prepare data for SearchDialogEto - ADD PRICE COLUMN
            var searchItems = batches.Select(b => new[]
            {
        b.Itemcode.ToString(),
        b.Batchcode.ToString(),
        b.Units.ToString("F2"),
        b.SellingPrice.ToString("F2"), // Add price
        b.MfgDate?.ToString("yyyy-MM-dd") ?? "",
        b.ExpDate?.ToString("yyyy-MM-dd") ?? "",
    }).ToList();

            var headerEntries = new List<(string Title, TextAlignment Alignment, bool)>
    {
        ("Itemcode", TextAlignment.Left, true),
        ("Batchcode", TextAlignment.Left, true),
        ("Units", TextAlignment.Right, true),
        ("Price", TextAlignment.Right, true), // Add price header
        ("MFG Date", TextAlignment.Left, false),
        ("Expiry Date", TextAlignment.Left, true),
    };
            var searchItemsWithColors = searchItems
                .Select(row => (Data: row, ForegroundColor: (Eto.Drawing.Color?)null, BackgroundColor: (Eto.Drawing.Color?)null))
                .ToList();

            var dialog = new SearchDialogEto(
                searchItemsWithColors,
                headerEntries,
                Debug: false
            );

            dialog.Title = $"Select Batches for {BackOfficeAccounting.LookupItem(itemCode)}";
            dialog.ReportSelectedButtonText = "Use Selected Order";

            // Show the dialog
            Log("About to ShowModal");
            dialog.ShowModal(this);
            Log($"Dialog closed with result:STUBBED");

            // Process the results
            Application.Instance.AsyncInvoke(() => {
                bool gotSelections = false;

                // Try OutputList first
                if (dialog.OutputList != null && dialog.OutputList.Count > 0)
                {
                    Log($"Processing {dialog.OutputList.Count} batches from OutputList");
                    selectedBatchesForCurrentItem.Clear();
                    foreach (var row in dialog.OutputList)
                    {
                        selectedBatchesForCurrentItem.Add(new SelectedBatch
                        {
                            Batchcode = long.Parse(row[1]),
                            AvailableUnits = double.Parse(row[2]),
                            MfgDate = row[4], // Mfg date is now at index 4
                            ExpDate = row[5]  // Exp date is now at index 5
                        });
                        Log($"Added batch {row[1]} with {row[2]} units and price {row[3]}");
                    }
                    RefreshSelectedBatchesGrid();
                    UpdateBatchSufficiencyLabel();
                    gotSelections = true;
                    Log("Batch grid refreshed from OutputList");
                }
                // Fallback to Selected if OutputList is empty
                else if (dialog.Selected != null && dialog.Selected.Length > 0)
                {
                    Log($"Processing selection from dialog.Selected");
                    // Convert single selection to full batch info
                    var batchCode = long.Parse(dialog.Selected[1]); // Assuming batchcode is at index 1
                    var batch = batches.FirstOrDefault(b => b.Batchcode == batchCode);
                    if (batch != null)
                    {
                        selectedBatchesForCurrentItem.Clear();
                        selectedBatchesForCurrentItem.Add(new SelectedBatch
                        {
                            Batchcode = batch.Batchcode,
                            AvailableUnits = batch.Units,
                            MfgDate = batch.MfgDate?.ToString("yyyy-MM-dd") ?? "",
                            ExpDate = batch.ExpDate?.ToString("yyyy-MM-dd") ?? ""
                        });
                        RefreshSelectedBatchesGrid();
                        UpdateBatchSufficiencyLabel();
                        gotSelections = true;
                        Log("Batch grid refreshed from Selected property");
                    }
                }

                if (!gotSelections)
                {
                    Log("No selections found in either OutputList or Selected property");
                    selectedBatchesForCurrentItem.Clear();
                    RefreshSelectedBatchesGrid();
                    UpdateBatchSufficiencyLabel();
                }

                // Move focus away regardless
                tbQty.Focus();
                Log("Focus moved to tbQty");
            });
        }

        void UpdateBatchSufficiencyLabel()
        {
            if (selectedBatchesForCurrentItem.Count == 0)
            {
                lblBatchSufficiency.Text = "";
                return;
            }

            double totalAvailable = selectedBatchesForCurrentItem.Sum(b => b.AvailableUnits);
            double requiredQty = ParseDouble(tbQty.Text);

            if (requiredQty <= 0)
            {
                lblBatchSufficiency.Text = $"Available: {totalAvailable:F2} units";
                lblBatchSufficiency.TextColor = Colors.Black;
            }
            else if (totalAvailable >= requiredQty)
            {
                lblBatchSufficiency.Text = $"✓ Sufficient: {totalAvailable:F2} units available (need: {requiredQty:F2})";
                lblBatchSufficiency.TextColor = Colors.Green;
            }
            else
            {
                lblBatchSufficiency.Text = $"✗ Insufficient: {totalAvailable:F2} units available (need: {requiredQty:F2})";
                lblBatchSufficiency.TextColor = Colors.Red;
            }
        }

        // Also add to tbQty.TextChanged




        bool ValidateSaleForm()
        {
            Log($"ValidateSaleForm called");
            Log($"  tbItem.Text = '{tbItem.Text}'");
            Log($"  selectedBatchesForCurrentItem.Count = {selectedBatchesForCurrentItem?.Count ?? -1}");
            Log($"  tbQty.Text = '{tbQty.Text}'");
            Log($"  tbPrice.Text = '{tbPrice.Text}'");
            Log($"  tbDisc.Text = '{tbDisc.Text}'");

            if (!long.TryParse(tbItem.Text, out var itemId) || itemId <= 0)
            {
                Log("Validation failed: Invalid item code");
                MessageBox.Show(this, "Please select a valid item", "Validation Error", MessageBoxType.Error);
                tbItem.Focus();
                return false;
            }

            if (selectedBatchesForCurrentItem == null || selectedBatchesForCurrentItem.Count == 0)
            {
                Log("Validation failed: No batches selected");
                MessageBox.Show(this, "No batches selected for this item. Please enter an item code.", "Validation Error", MessageBoxType.Error);
                tbItem.Focus();
                return false;
            }

            if (!double.TryParse(tbQty.Text, out double qty) || qty <= 0)
            {
                Log("Validation failed: Invalid quantity");
                MessageBox.Show(this, "Please enter a valid quantity", "Validation Error", MessageBoxType.Error);
                tbQty.Focus();
                return false;
            }

            // AUTO-ADD if price is empty - don't validate, let AddSale handle it
            if (string.IsNullOrEmpty(tbPrice.Text) || ParseDouble(tbPrice.Text) <= 0)
            {
                Log("Price is empty, will auto-add using batch prices");
                // Don't show messagebox, don't fail validation
                // Set a flag to indicate auto-add mode
                autoAddWithBatchPrices = true;
            }
            else if (!double.TryParse(tbPrice.Text, out double price) || price <= 0)
            {
                Log("Validation failed: Invalid price");
                MessageBox.Show(this, "Please enter a valid price", "Validation Error", MessageBoxType.Error);
                tbPrice.Focus();
                return false;
            }

            if (!double.TryParse(tbDisc.Text, out double disc) || disc < 0 || disc > 100)
            {
                Log("Validation failed: Invalid discount");
                MessageBox.Show(this, "Please enter a valid discount (0-100)", "Validation Error", MessageBoxType.Error);
                tbDisc.Focus();
                return false;
            }

            Log("Validation passed");
            return true;
        }

        // And in ResetSaleForm
        void ResetSaleForm()
        {
            Log("ResetSaleForm called");
            tbItem.Text = tbQty.Text = tbPrice.Text = tbDisc.Text = "";
            lblItemName.Text = "";
            selectedBatchesForCurrentItem.Clear();
            RefreshSelectedBatchesGrid();
            UpdateBatchSufficiencyLabel();
            itemCodeChanged = false; // Reset the flag
            Log("ResetSaleForm completed");
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

        void RefreshSelectedBatchesGrid()
        {
            Log($"RefreshSelectedBatchesGrid called. Count: {selectedBatchesForCurrentItem.Count}");
            selectedBatchesGrid.DataStore = null;
            selectedBatchesGrid.DataStore = selectedBatchesForCurrentItem;
            selectedBatchesGrid.Invalidate(true);
            Log("Batch grid invalidated");
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

        // Also need to handle the case where user enters price manually after batches are selected
        void tbPrice_TextChanged(object sender, EventArgs e)
        {
            if (isProcessingBatchSelection) return;

            if (!string.IsNullOrEmpty(tbPrice.Text))
            {
                // Clear auto-add flag if user enters a price
                autoAddWithBatchPrices = false;
            }
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
        void TempSaveForm()
        {
            var (isValid, errors) = ValidateForTempSave();

            if (!isValid)
            {
                MessageBox.Show(this, errors, TranslationHelper.Translate("Validation Error"), MessageBoxType.Error);
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

            MessageBox.Show(this, json, TranslationHelper.Translate("Temporary Invoice Data"), MessageBoxType.Information);
        }

        // Add validation for temporary save
        (bool IsValid, string ConsolidatedErrorList) ValidateForTempSave()
        {
            var errs = new List<string>();

            // Validate basic types exist (but not values)
            if (!long.TryParse(tbCustomer.Text, out _))
                errs.Add("Customer must be a number");

            if (!long.TryParse(tbSalesPerson.Text, out _))
                errs.Add("Sales Person must be a number");

            if (!decimal.TryParse(tbPrice.Text, out _))
                errs.Add("Price must be a valid number");

            if (!decimal.TryParse(tbQty.Text, out _))
                errs.Add("Quantity must be a valid number");

            // We don't need to validate amounts add up for temp save
            return (errs.Count == 0, string.Join(", ", errs));
        }
    }

    public class SalesData
    {
        public IssuedInvoice Invoice { get; set; }
        public List<Sale> Sales { get; set; }
        public List<Receipt> Receipts { get; set; }
    }
}