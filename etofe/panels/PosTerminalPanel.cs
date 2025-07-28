using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Eto.Forms;
using Eto.Drawing;
using RV.InvNew.Common;
using RV.InvNew.CommonUi;

public class PosPanel : Panel
{
    const int FWidth = 120, FHeight = 24;

    List<Sale> sales = new List<Sale>();
    List<Receipt> receipts = new List<Receipt>();
    IssuedInvoice invoice = new IssuedInvoice();

    TextBox tbCustomer, tbSalesPerson, tbCurrency;
    Label lblCustomerName, lblSalesPersonName;
    DateTimePicker dpInvoiceDate;
    CheckBox cbIsPosted;
    TextArea taRemarks;

    GridView saleGrid;
    TextBox tbItem, tbInventory, tbQty, tbPrice, tbDisc;
    Label lblItemName, lblInventoryName;
    Button btnSaveSale, btnResetSale;

    Label lblQtyTot, lblSubTot, lblTaxTot;
    TextBox tbPaid;
    Label lblChange;

    GridView rcptGrid;
    TextBox tbRcptAcct, tbRcptAmt;
    Label lblAcctName;
    Button btnSaveRcpt, btnResetRcpt;

    Label lblRcptTot;
    Button btnSaveAll;

    public PosPanel()
    {
        // Invoice
        tbCustomer = SearchField<Pii>(ListPii, out lblCustomerName, "Customer PII");
        tbSalesPerson = SearchField<Pii>(ListPii, out lblSalesPersonName, "Sales Person PII");
        tbCurrency = NewTextBox("Currency");
        dpInvoiceDate = new DateTimePicker { Value = DateTime.Now, Width = FWidth, Height = FHeight };
        cbIsPosted = new CheckBox { Text = "Posted" };
        taRemarks = new TextArea { Width = 300, Height = 60 };

        var invoiceForm = new TableLayout
        {
            Padding = 5,
            Spacing = new Size(5, 5),
            Rows =
            {
                new TableRow("Customer:",     new StackLayout { Orientation=Orientation.Horizontal, Spacing=5, Items={ tbCustomer, lblCustomerName } }),
                new TableRow("Sales Person:", new StackLayout { Orientation=Orientation.Horizontal, Spacing=5, Items={ tbSalesPerson, lblSalesPersonName } }),
                new TableRow("Currency:",     tbCurrency),
                new TableRow("Date:",         dpInvoiceDate),
                new TableRow(cbIsPosted,      null),
                new TableRow("Remarks:",      taRemarks)
            }
        };

        // Sales
        saleGrid = new GridView { Width = 600, Height = 120, DataStore = sales };
        saleGrid.Columns.Add(new GridColumn
        {
            HeaderText = "Name",
            DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.ProductName) }
        });
        saleGrid.Columns.Add(new GridColumn
        {
            HeaderText = "Item",
            DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.Itemcode.ToString()) }
        });
        saleGrid.Columns.Add(new GridColumn
        {
            HeaderText = "Inventory",
            DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.Batchcode.ToString()) }
        });
        saleGrid.Columns.Add(new GridColumn
        {
            HeaderText = "Qty",
            DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.Quantity.ToString("F2")) }
        });
        saleGrid.Columns.Add(new GridColumn
        {
            HeaderText = "Price",
            DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.SellingPrice.ToString("C2")) }
        });
        saleGrid.Columns.Add(new GridColumn
        {
            HeaderText = "Disc%",
            DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.DiscountRate.ToString("F2")) }
        });
        saleGrid.Columns.Add(new GridColumn
        {
            HeaderText = "VAT",
            DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.VatAsCharged.ToString("C2")) }
        });
        saleGrid.Columns.Add(new GridColumn
        {
            HeaderText = "Total",
            DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(s => s.TotalEffectiveSellingPrice.ToString("C2")) }
        });

        tbItem = SearchField<Catalogue>(() => ListCatalogue(), out lblItemName, "Item code");
        tbInventory = SearchField<Inventory>(() => ListInventory(ParseLong(tbItem.Text)), out lblInventoryName, "Inventory code");
        tbQty = NewTextBox("Quantity");
        tbPrice = NewTextBox("Price");
        tbDisc = NewTextBox("Discount %");
        btnSaveSale = new Button { Text = "Save", Width = FWidth, Height = FHeight };
        btnResetSale = new Button { Text = "Reset", Width = FWidth, Height = FHeight };
        btnSaveSale.Click += (_, __) => { AddSale(); ResetSaleForm(); };
        btnResetSale.Click += (_, __) => ResetSaleForm();

        var saleForm = new TableLayout
        {
            Padding = 5,
            Spacing = new Size(5, 5),
            Rows =
            {
                new TableRow("Item:",      new StackLayout { Orientation=Orientation.Horizontal, Spacing=5, Items={ tbItem, lblItemName } }),
                new TableRow("Inventory:", new StackLayout { Orientation=Orientation.Horizontal, Spacing=5, Items={ tbInventory, lblInventoryName } }),
                new TableRow("Qty:",       tbQty),
                new TableRow("Price:",     tbPrice),
                new TableRow("Disc%:",     tbDisc),
                null,
                new TableRow(btnSaveSale, btnResetSale)
            }
        };

        // Summary & Payment
        lblQtyTot = new Label(); lblSubTot = new Label(); lblTaxTot = new Label();
        tbPaid = NewTextBox("Paid"); tbPaid.TextChanged += (_, __) => UpdateChange();
        lblChange = new Label();

        var summaryPanel = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 20, Items = { lblQtyTot, lblSubTot, lblTaxTot } };
        var paymentPanel = new TableLayout
        {
            Padding = 5,
            Rows = { new TableRow("Paid:", tbPaid), new TableRow("Change:", lblChange) }
        };

        // Receipts
        rcptGrid = new GridView { Width = 600, Height = 100, DataStore = receipts };
        rcptGrid.Columns.Add(new GridColumn
        {
            HeaderText = "Account",
            DataCell = new TextBoxCell { Binding = Binding.Delegate<Receipt, string>(r => r.AccountId.ToString()) }
        });
        rcptGrid.Columns.Add(new GridColumn
        {
            HeaderText = "Amount",
            DataCell = new TextBoxCell { Binding = Binding.Delegate<Receipt, string>(r => r.Amount.ToString("C2")) }
        });

        tbRcptAcct = NewTextBox("Account ID");
        lblAcctName = new Label();
        tbRcptAcct.TextChanged += (_, __) => lblAcctName.Text = LookupHumanFriendlyAccount(tbRcptAcct.Text);
        tbRcptAmt = NewTextBox("Amount");
        btnSaveRcpt = new Button { Text = "Save", Width = FWidth, Height = FHeight };
        btnResetRcpt = new Button { Text = "Reset", Width = FWidth, Height = FHeight };
        btnSaveRcpt.Click += (_, __) => { AddReceipt(); ResetRcptForm(); };
        btnResetRcpt.Click += (_, __) => ResetRcptForm();

        var rcptForm = new TableLayout
        {
            Padding = 5,
            Spacing = new Size(5, 5),
            Rows =
            {
                new TableRow("Acct:", new StackLayout { Orientation=Orientation.Horizontal, Spacing=5, Items={ tbRcptAcct, lblAcctName } }),
                new TableRow("Amt:",  tbRcptAmt),
                null,
                new TableRow(btnSaveRcpt, btnResetRcpt)
            }
        };
        lblRcptTot = new Label();

        // Save
        btnSaveAll = new Button { Text = "Save Invoice", Width = 200, Height = 30 };
        btnSaveAll.Click += (_, __) => SaveAll();

        Content = new StackLayout
        {
            Width = 820,
            Height = 800,
            Orientation = Orientation.Vertical,
            Spacing = 10,
            Padding = 10,
            Items =
            {
                new GroupBox { Text="Invoice Info", Content=invoiceForm },
                new GroupBox { Text="Sales Items",  Content=new StackLayout { Spacing=5, Items={ saleGrid, saleForm } } },
                new GroupBox { Text="Summary",      Content=summaryPanel },
                new GroupBox { Text="Payment",      Content=paymentPanel },
                new GroupBox { Text="Receipts",     Content=new StackLayout { Spacing=5, Items={ rcptGrid, rcptForm, lblRcptTot } } },
                btnSaveAll
            }
        };

        RecalculateAll();
    }

    TextBox NewTextBox(string ph) => new TextBox { PlaceholderText = ph, Width = FWidth, Height = FHeight };

    TextBox SearchField<T>(Func<List<T>> fetch, out Label lbl, string ph)
    {
        var tb = NewTextBox(ph);
        var label = new Label();
        tb.GotFocus += (s, e) =>
        {
            tb.Text = "";
            var sel = CommonUi.SearchPanelUtility.GenerateSearchDialog(fetch(), this);
            if (sel?.Length > 0)
            {
                tb.Text = sel[0];
                label.Text = typeof(T) == typeof(Pii)
                    ? LookupHumanFriendlyPii(tb.Text)
                    : LookupHumanFriendlyItemcode(ParseLong(tb.Text));
            }
        };
        lbl = label;
        return tb;
    }


    bool TryNum(string txt, out double v) => double.TryParse(txt, out v);
    bool TryNum(string txt, out long v) => long.TryParse(txt, out v);

    void AddSale()
    {
        TryNum(tbItem.Text, out long ic);
        TryNum(tbInventory.Text, out long inv);
        TryNum(tbQty.Text, out double q);
        TryNum(tbPrice.Text, out double pr);
        TryNum(tbDisc.Text, out double dr);

        var s = new Sale
        {
            Itemcode = ic,
            Batchcode = inv,
            Quantity = q,
            SellingPrice = pr,
            DiscountRate = dr
        };
        s.ProductName = LookupHumanFriendlyInventory(ic, inv);
        s.Discount = q * pr * dr / 100;
        s.VatAsCharged = (q * pr - s.Discount) * s.VatRatePercentage / 100;
        s.TotalEffectiveSellingPrice = q * pr - s.Discount + s.VatAsCharged;

        sales.Add(s);
        saleGrid.DataStore = null; saleGrid.DataStore = sales;
        RecalculateAll();
    }

    void ResetSaleForm() => tbItem.Text = tbInventory.Text = tbQty.Text = tbPrice.Text = tbDisc.Text = "";

    void AddReceipt()
    {
        TryNum(tbRcptAcct.Text, out long ac);
        TryNum(tbRcptAmt.Text, out double am);

        var r = new Receipt
        {
            InvoiceId = invoice.InvoiceId,
            AccountId = ac,
            Amount = am,
            TimeReceived = DateTimeOffset.Now
        };
        receipts.Add(r);
        rcptGrid.DataStore = null; rcptGrid.DataStore = receipts;
        RecalculateAll();
    }

    void ResetRcptForm() => tbRcptAcct.Text = tbRcptAmt.Text = "";

    void RecalculateAll()
    {
        TryNum(tbCustomer.Text, out long cust); invoice.Customer = cust;
        TryNum(tbSalesPerson.Text, out long sp); invoice.SalesPersonId = sp;
        invoice.CurrencyCode = tbCurrency.Text;
        invoice.InvoiceTime = dpInvoiceDate.Value ?? DateTime.MinValue;
        invoice.IsPosted = cbIsPosted.Checked == true;
        invoice.InvoiceHumanFriendly = taRemarks.Text;

        invoice.SubTotal = sales.Sum(x => x.Quantity * x.SellingPrice);
        invoice.DiscountTotal = sales.Sum(x => x.Discount);
        invoice.TaxTotal = sales.Sum(x => x.VatAsCharged);
        invoice.GrandTotal = sales.Sum(x => x.TotalEffectiveSellingPrice);
        invoice.EffectiveDiscountPercentage = invoice.SubTotal > 0
            ? invoice.DiscountTotal / invoice.SubTotal * 100 : 0;

        lblQtyTot.Text = $"Qty: {sales.Sum(x => x.Quantity):F2}";
        lblSubTot.Text = $"Sub: {invoice.SubTotal:C}";
        lblTaxTot.Text = $"Tax: {invoice.TaxTotal:C}";
        lblRcptTot.Text = $"Rcpt: {receipts.Sum(r => r.Amount):C}";
        UpdateChange();
    }

    void UpdateChange()
    {
        TryNum(tbPaid.Text, out double paid);
        lblChange.Text = $"Change: {(paid - invoice.GrandTotal):C}";
    }

    void SaveAll()
    {
        var data = new PosData { Invoice = invoice, Sales = sales, Receipts = receipts };
        var opts = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(data, opts);
        MessageBox.Show(this, json, MessageBoxType.Information);
    }

    public void Load(string json)
    {
        var data = JsonSerializer.Deserialize<PosData>(json);
        if (data == null) return;
        invoice = data.Invoice;
        sales = data.Sales ?? new List<Sale>();
        receipts = data.Receipts ?? new List<Receipt>();

        tbCustomer.Text = invoice.Customer?.ToString() ?? "";
        lblCustomerName.Text = LookupHumanFriendlyPii(tbCustomer.Text);
        tbSalesPerson.Text = invoice.SalesPersonId.ToString();
        lblSalesPersonName.Text = LookupHumanFriendlyPii(tbSalesPerson.Text);
        tbCurrency.Text = invoice.CurrencyCode;
        dpInvoiceDate.Value = invoice.InvoiceTime;
        cbIsPosted.Checked = invoice.IsPosted;
        taRemarks.Text = invoice.InvoiceHumanFriendly;

        saleGrid.DataStore = null; saleGrid.DataStore = sales;
        rcptGrid.DataStore = null; rcptGrid.DataStore = receipts;
        RecalculateAll();
    }

    long ParseLong(string txt) => long.TryParse(txt, out var v) ? v : 0;

    // stubs
    string LookupHumanFriendlyPii(string id) => $"TBD PII {id}";
    string LookupHumanFriendlyItemcode(long code) => $"TBD Item {code}";
    string LookupHumanFriendlyInventory(long item, long inv) => $"TBD Inv {item}/{inv}";
    string LookupHumanFriendlyAccount(string id) => $"TBD Acct {id}";

    // data loaders
    List<Pii> ListPii() => new List<Pii> { new Pii()};
    List<Catalogue> ListCatalogue() => new List<Catalogue> { new Catalogue() };
    List<Inventory> ListInventory(long itemcode) => new List<Inventory> { new Inventory() };
}

public class PosData
{
    public IssuedInvoice Invoice { get; set; }
    public List<Sale> Sales { get; set; }
    public List<Receipt> Receipts { get; set; }
}
