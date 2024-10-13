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
    public static class Randomizers
    {
        public static Eto.Drawing.Color GetRandomBgColor()
        {
            var Rand = new Random();
            ushort R = (ushort)(Rand.Next() % 128);
            ushort G = (ushort)(Rand.Next() % 128);
            ushort B = (ushort)(Rand.Next() % 128);
            return new Eto.Drawing.Color(R, G, B);
        }
        public static Eto.Drawing.Color GetRandomFgColor()
        {
            var Rand = new Random();
            ushort R = (ushort)(128 + Rand.Next() % 128);
            ushort G = (ushort)(128 + Rand.Next() % 128);
            ushort B = (ushort)(128 + Rand.Next() % 128);
            return new Eto.Drawing.Color(R, G, B);
        }
    }
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
            PosData.Wait();
            var PosDataResponse = PosData.Result;
            PosDataResponse.EnsureSuccessStatusCode();
            PosRefresh PR;
            var TPosDataResponse = PosDataResponse.Content.ReadAsAsync<PosRefresh>();
            //var strres = PosDataResponse.Content.ReadAsStringAsync();
            //strres.Wait();
            TPosDataResponse.Wait();
            //MessageBox.Show(strres.Result);

            PR = TPosDataResponse.Result;
            MessageBox.Show(PR.Catalogue.Count().ToString());

            List<(string, TextAlignment, bool)> HeaderEntries = new()
            {
                ("Itemcode", TextAlignment.Right, true),
                ("Name", TextAlignment.Left, false),
                ("Split 1", TextAlignment.Right, false),
                ("Split 2", TextAlignment.Center, false),
                ("Split 3", TextAlignment.Right, false),
                ("Split 4", TextAlignment.Left, true)
            };

            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SearchCatalogue = PR.Catalogue
                .Select<PosCatalogue, (string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>
                //(e => { return (e.ToStringArray(), Randomizers.GetRandomBgColor(), Randomizers.GetRandomFgColor()); })
                (e => { return (e.ToStringArray(), null, null); })
                .ToList();
            Barcode.KeyDown += (e, a) => { (new SearchDialog(SearchCatalogue, HeaderEntries)).ShowModal(); };
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
