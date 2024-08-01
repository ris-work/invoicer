using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;

namespace EtoFE
{
    public class PosTerminal: Form
    {
        public PosTerminal()
        {
            TableLayout TL = new TableLayout();
            Label LabelPendingInvoices = new Label() { Text = "Pending invoices:" };
            ListBox PendingInvoices = new ListBox();
            Label LabelPastSuccessfulInvocies = new Label() { Text = "Successful invoices:" };
            ListBox PastSuccessfulInvoices = new ListBox() {  };
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

            TL.Rows.Add(new TableRow(LabelPendingInvoices, LabelPastSuccessfulInvocies));
            TL.Rows.Add(new TableRow(PendingInvoices, PastSuccessfulInvoices));
            TL.Rows.Add(new TableRow(LabelBarcode, Barcode, CurrentItem));
            TL.Rows.Add(new TableRow(LabelPrice, Price));
            TL.Rows.Add(new TableRow(LabelQuantity, Quantity));
            TL.Rows.Add(new TableRow(LabelVatCategory, VatCategory, CurrentVatRate, CurrentVatCategory));
            Content = TL;
        }
    }
}
