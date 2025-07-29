using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Eto.Forms;
using Eto.Drawing;
using RV.InvNew.Common;

public class PosPanel : Scrollable
{
    const int FWidth = 120, FHeight = 24;
    const int Columns = 2;  // always two columns

    List<Sale> sales;
    List<Receipt> receipts;
    IssuedInvoice invoice;

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
        sales = new List<Sale> { new Sale() };
        receipts = new List<Receipt> { new Receipt() };
        invoice = new IssuedInvoice();

        InitializeFields();
        BuildLayout();
        RecalculateAll();
    }

    void InitializeFields()
    {
        // search fields now take a "next" control to focus after selection
        tbCustomer = SearchField<Pii>(ListPii, out lblCustomerName, "Customer PII", () => tbSalesPerson);
        tbSalesPerson = SearchField<Pii>(ListPii, out lblSalesPersonName, "Sales Person PII", () => tbCurrency);
        tbCurrency = NewTextBox("Currency");
        dpInvoiceDate = new DateTimePicker { Value = DateTime.Now, Width = FWidth, Height = FHeight };
        cbIsPosted = new CheckBox { Text = "Posted" };
        taRemarks = new TextArea { Width = 300, Height = 60 };

        // sales grid
        saleGrid = new GridView { Width = 600, Height = 120, DataStore = sales };
        foreach (var (hdr, bind) in new[]
        {
            ("Name",      (Func<Sale,string>)(s=>s.ProductName)),
            ("Item",      s=>s.Itemcode.ToString()),
            ("Inventory", s=>s.Batchcode.ToString()),
            ("Qty",       s=>s.Quantity.ToString("F2")),
            ("Price",     s=>s.SellingPrice.ToString("C2")),
            ("Disc%",     s=>s.DiscountRate.ToString("F2")),
            ("VAT",       s=>s.VatAsCharged.ToString("C2")),
            ("Total",     s=>s.TotalEffectiveSellingPrice.ToString("C2")),
        })
        {
            saleGrid.Columns.Add(new GridColumn
            {
                HeaderText = hdr,
                DataCell = new TextBoxCell { Binding = Binding.Delegate<Sale, string>(bind) }
            });
        }

        tbItem = SearchField<Catalogue>(() => ListCatalogue(), out lblItemName, "Item code", () => tbInventory);
        tbInventory = SearchField<Inventory>(() => ListInventory(ParseLong(tbItem.Text)), out lblInventoryName, "Inventory code", () => tbQty);
        tbQty = NewTextBox("Quantity");
        tbPrice = NewTextBox("Price");
        tbDisc = NewTextBox("Discount %");

        btnSaveSale = new Button { Text = "Save", Width = FWidth, Height = FHeight };
        btnResetSale = new Button { Text = "Reset", Width = FWidth, Height = FHeight };
        btnSaveSale.Click += (_, __) => { AddSale(); ResetSaleForm(); };
        btnResetSale.Click += (_, __) => ResetSaleForm();

        // summary & payment
        lblQtyTot = new Label();
        lblSubTot = new Label();
        lblTaxTot = new Label();
        tbPaid = NewTextBox("Paid"); tbPaid.TextChanged += (_, __) => UpdateChange();
        lblChange = new Label();

        // receipts grid
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
        lblRcptTot = new Label();

        // final save
        btnSaveAll = new Button { Text = "Save Invoice", Width = 200, Height = 30 };
        btnSaveAll.Click += (_, __) => SaveAll();

        WireEnterKeyChaining();
    }

    void BuildLayout()
    {
        var invoiceForm = BuildNColumnForm(
            ("Customer:", new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { tbCustomer, lblCustomerName } }),
            ("Sales Person:", new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { tbSalesPerson, lblSalesPersonName } }),
            ("Currency:", tbCurrency),
            ("Date:", dpInvoiceDate),
            ("Posted", cbIsPosted),
            ("Remarks:", taRemarks)
        );

        var saleForm = BuildNColumnForm(
            ("Item:", new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { tbItem, lblItemName } }),
            ("Inventory:", new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { tbInventory, lblInventoryName } }),
            ("Qty:", tbQty),
            ("Price:", tbPrice),
            ("Disc%:", tbDisc),
            ("Save", btnSaveSale),
            ("Reset", btnResetSale)
        );

        var summaryPanel = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 20,
            Items = { lblQtyTot, lblSubTot, lblTaxTot }
        };

        var paymentPanel = BuildNColumnForm(
            ("Paid:", tbPaid),
            ("Change:", lblChange)
        );

        var rcptForm = BuildNColumnForm(
            ("Acct:", tbRcptAcct),
            ("Amt:", tbRcptAmt),
            ("Save", btnSaveRcpt),
            ("Reset", btnResetRcpt)
        );

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
    }

    TableLayout BuildNColumnForm(params (string Label, Control Control)[] items)
    {
        var layout = new TableLayout
        {
            Padding = 5,
            Spacing = new Size(5, 5)
        };

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

    TextBox SearchField<T>(
        Func<List<T>> fetch,
        out Label lbl,
        string placeholder,
        Func<Control> nextControl
    )
    {
        var tb = new TextBox { PlaceholderText = placeholder, Width = FWidth, Height = FHeight };
        var label = new Label();
        tb.GotFocus += async (s, e) =>
        {
            tb.Text = "";
            var sel = CommonUi.SearchPanelUtility.GenerateSearchDialog(fetch(), this, false);
            if (sel?.Length > 0)
            {
                tb.Text = sel[0];
                label.Text = typeof(T) == typeof(Pii)
                    ? LookupHumanFriendlyPii(tb.Text)
                    : LookupHumanFriendlyItemcode(ParseLong(tb.Text));

                // ensure dialog has closed before focusing next field
                Application.Instance.AsyncInvoke(() => nextControl().Focus());
            }
        };

        lbl = label;
        return tb;
    }

    TextBox NewTextBox(string placeholder) =>
        new TextBox { PlaceholderText = placeholder, Width = FWidth, Height = FHeight };

    void WireEnterKeyChaining()
    {
        tbCustomer.KeyDown += (s, e) => { if (e.Key == Keys.Enter && TryParseLong(tbCustomer.Text, out _)) tbSalesPerson.Focus(); };
        tbSalesPerson.KeyDown += (s, e) => { if (e.Key == Keys.Enter && TryParseLong(tbSalesPerson.Text, out _)) tbCurrency.Focus(); };
        tbCurrency.KeyDown += (s, e) => { if (e.Key == Keys.Enter) dpInvoiceDate.Focus(); };
        dpInvoiceDate.KeyDown += (s, e) => { if (e.Key == Keys.Enter) cbIsPosted.Focus(); };
        cbIsPosted.KeyDown += (s, e) => { if (e.Key == Keys.Enter) taRemarks.Focus(); };
        taRemarks.KeyDown += (s, e) => { if (e.Key == Keys.Enter) tbItem.Focus(); };

        tbItem.KeyDown += (s, e) => { if (e.Key == Keys.Enter && TryParseLong(tbItem.Text, out _)) tbInventory.Focus(); };
        tbInventory.KeyDown += (s, e) => { if (e.Key == Keys.Enter && TryParseLong(tbInventory.Text, out _)) tbQty.Focus(); };
        tbQty.KeyDown += (s, e) => { if (e.Key == Keys.Enter && TryParseDouble(tbQty.Text, out _)) tbPrice.Focus(); };
        tbPrice.KeyDown += (s, e) => { if (e.Key == Keys.Enter && TryParseDouble(tbPrice.Text, out _)) tbDisc.Focus(); };
        tbDisc.KeyDown += (s, e) => { if (e.Key == Keys.Enter && TryParseDouble(tbDisc.Text, out _)) btnSaveSale.Focus(); };

        tbRcptAcct.KeyDown += (s, e) => { if (e.Key == Keys.Enter && TryParseLong(tbRcptAcct.Text, out _)) tbRcptAmt.Focus(); };
        tbRcptAmt.KeyDown += (s, e) => { if (e.Key == Keys.Enter && TryParseDouble(tbRcptAmt.Text, out _)) btnSaveRcpt.Focus(); };
    }

    bool TryParseLong(string txt, out long v) => long.TryParse(txt, out v);
    bool TryParseDouble(string txt, out double v) => double.TryParse(txt, out v);

    void AddSale()
    {
        TryParseLong(tbItem.Text, out long ic);
        TryParseLong(tbInventory.Text, out long inv);
        TryParseDouble(tbQty.Text, out double q);
        TryParseDouble(tbPrice.Text, out double pr);
        TryParseDouble(tbDisc.Text, out double dr);

        var s = new Sale
        {
            Itemcode = ic,
            Batchcode = inv,
            Quantity = q,
            SellingPrice = pr,
            DiscountRate = dr,
            ProductName = LookupHumanFriendlyInventory(ic, inv),
            Discount = q * pr * dr / 100
        };
        s.VatAsCharged = (q * pr - s.Discount) * s.VatRatePercentage / 100;
        s.TotalEffectiveSellingPrice = q * pr - s.Discount + s.VatAsCharged;

        sales.Add(s);
        saleGrid.DataStore = null;
        saleGrid.DataStore = sales;
        RecalculateAll();
    }

    void ResetSaleForm()
    {
        tbItem.Text = tbInventory.Text = tbQty.Text = tbPrice.Text = tbDisc.Text = "";
    }

    void AddReceipt()
    {
        TryParseLong(tbRcptAcct.Text, out long ac);
        TryParseDouble(tbRcptAmt.Text, out double am);

        var r = new Receipt
        {
            InvoiceId = invoice.InvoiceId,
            AccountId = ac,
            Amount = am,
            TimeReceived = DateTimeOffset.Now
        };
        receipts.Add(r);
        rcptGrid.DataStore = null;
        rcptGrid.DataStore = receipts;
        RecalculateAll();
    }

    void ResetRcptForm()
    {
        tbRcptAcct.Text = tbRcptAmt.Text = "";
    }

    void RecalculateAll()
    {
        TryParseLong(tbCustomer.Text, out long cust);
        invoice.Customer = cust;
        TryParseLong(tbSalesPerson.Text, out long sp);
        invoice.SalesPersonId = sp;
        invoice.CurrencyCode = tbCurrency.Text;
        invoice.InvoiceTime = dpInvoiceDate.Value ?? DateTime.Now;
        invoice.IsPosted = cbIsPosted.Checked == true;
        invoice.InvoiceHumanFriendly = taRemarks.Text;

        invoice.SubTotal = sales.Sum(x => x.Quantity * x.SellingPrice);
        invoice.DiscountTotal = sales.Sum(x => x.Discount);
        invoice.TaxTotal = sales.Sum(x => x.VatAsCharged);
        invoice.GrandTotal = sales.Sum(x => x.TotalEffectiveSellingPrice);
        invoice.EffectiveDiscountPercentage =
            invoice.SubTotal > 0 ? invoice.DiscountTotal / invoice.SubTotal * 100 : 0;

        lblQtyTot.Text = $"Qty: {sales.Sum(x => x.Quantity):F2}";
        lblSubTot.Text = $"Sub: {invoice.SubTotal:C}";
        lblTaxTot.Text = $"Tax: {invoice.TaxTotal:C}";
        lblRcptTot.Text = $"Rcpt: {receipts.Sum(r => r.Amount):C}";
        UpdateChange();
    }

    void UpdateChange()
    {
        TryParseDouble(tbPaid.Text, out double paid);
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
        sales = data.Sales ?? new List<Sale> { new Sale() };
        receipts = data.Receipts ?? new List<Receipt> { new Receipt() };

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
    string LookupHumanFriendlyPii(string id) => $"TBD PII {id}";
    string LookupHumanFriendlyItemcode(long code) => $"TBD Item {code}";
    string LookupHumanFriendlyInventory(long item, long inv) => $"TBD Inv {item}/{inv}";
    string LookupHumanFriendlyAccount(string id) => $"TBD Acct {id}";

    List<Pii> ListPii() => new List<Pii> { new Pii() };
    List<Catalogue> ListCatalogue() => new List<Catalogue> { new Catalogue() };
    List<Inventory> ListInventory(long itemcode) => new List<Inventory> { new Inventory() };
}

public class PosData
{
    public IssuedInvoice Invoice { get; set; }
    public List<Sale> Sales { get; set; }
    public List<Receipt> Receipts { get; set; }
}
