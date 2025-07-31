/*using System;
using Eto.Drawing;
using Eto.Forms;
using RV.InvNew.Common;
using static Terminal.Gui.SpinnerStyle;

namespace YourApp.UI
{
    public class InvoiceHeaderPanel : Panel
    {
        public TextBox InvoiceCode { get; private set; }
        public DateTimePicker InvoiceDate { get; private set; }
        public ComboBox CustomerSelector { get; private set; }

        public InvoiceHeaderPanel()
        {
            InvoiceCode = new TextBox();
            InvoiceDate = new DateTimePicker { Value = DateTime.Today };
            CustomerSelector = new ComboBox();

            Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Padding = new Padding(10),
                Rows =
                {
                    new TableRow(new Label { Text = "Invoice #" }, InvoiceCode),
                    new TableRow(new Label { Text = "Date" }, InvoiceDate),
                    new TableRow(new Label { Text = "Customer" }, CustomerSelector),
                },
            };
        }

        public IssuedInvoice Serialize()
        {
            return new IssuedInvoice
            {
                InvoiceHumanFriendly = InvoiceCode.Text,
                InvoiceTime = InvoiceDate.Value ?? DateTime.MinValue,
                Customer = (Customer)CustomerSelector.SelectedValue,
            };
        }

        public void Deserialize(IssuedInvoice invoice)
        {
            InvoiceCode.Text = invoice.InvoiceHumanFriendly;
            InvoiceDate.Value = invoice.InvoiceTime;
            CustomerSelector.SelectedValue = invoice.Customer;
        }
    }
}
*/
