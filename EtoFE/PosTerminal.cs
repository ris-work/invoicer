using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using RV.InvNew.Common;
using Wiry.Base32;

namespace EtoFE
{
    public class PosTerminal : Form
    {
        public PosTerminal()
        {
            var R = new AuthenticatedRequest<string>("Hey", LoginTokens.token);
            var PosData = Program.client.PostAsJsonAsync("/PosRefresh", R);
            TableLayout TL = new TableLayout();
            Label LabelPendingInvoices = new Label() { Text = "Pending invoices:" };
            ListBox PendingInvoices = new ListBox();
            Label LabelPastSuccessfulInvocies = new Label() { Text = "Successful invoices:" };
            ListBox PastSuccessfulInvoices = new ListBox() { };
            Label LabelBarcode = new Label() { Text = "Barcode: " };
            TextBox Barcode = new TextBox();
            Label CurrentItem = new Label();
            Label LabelPrice = new Label() { Text = "Price: " };
            TextBox Price = new TextBox();
            TextBox Quantity = new TextBox();
            Label LabelQuantity = new Label() { Text = "Quantity: " };
            Label LabelVatCategory = new Label() { Text = "Vat category:" };
            TextBox VatCategory = new TextBox();
            Label CurrentVatRate = new Label();
            Label CurrentVatCategory = new Label();
            Label LabelInvoiceLocalKey = new Label();
            string InvoiceLocalKey;
            Padding = new Eto.Drawing.Padding(10, 10);
            
            TL.Spacing = new Eto.Drawing.Size(20, 20);

            TL.Rows.Add(new TableRow(LabelInvoiceLocalKey));
            TL.Rows.Add(new TableRow(LabelPendingInvoices, LabelPastSuccessfulInvocies));
            TL.Rows.Add(new TableRow(PendingInvoices, PastSuccessfulInvoices));
            TL.Rows.Add(new TableRow(LabelBarcode, Barcode, CurrentItem));
            TL.Rows.Add(new TableRow(LabelPrice, Price));
            TL.Rows.Add(new TableRow(LabelQuantity, Quantity));
            TL.Rows.Add(new TableRow(LabelVatCategory, VatCategory, CurrentVatRate, CurrentVatCategory));
            Barcode.KeyDown += (e, a) => { new SearchBox(); };
            Content = new StackLayout(null, TL, null) { Orientation = Orientation.Horizontal, Spacing = 5, Padding = 5 };
            var Gen = new Random();
            byte[] IdempotencyPOS = new byte[5];
            Gen.NextBytes(IdempotencyPOS);
            InvoiceLocalKey = Base32Encoding.Standard.GetString(IdempotencyPOS);
            MessageBox.Show(InvoiceLocalKey, "Idempotency Key", MessageBoxType.Information);
            LabelInvoiceLocalKey.Text = InvoiceLocalKey;
            AutoSize = true;
            Resizable = false;
            
        }
    }
}
