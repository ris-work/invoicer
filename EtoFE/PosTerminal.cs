using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using Microsoft.WindowsAPICodePack.Shell;
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
            Label CurrentTaxLabel = new Label();
            Label CurrentTaxRate = new Label() { TextAlignment = TextAlignment.Right };
            Label CurrentTotalLabel = new Label();
            Label CurrentTotal = new Label() { TextAlignment = TextAlignment.Right };
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
            TL.Rows.Add(new TableRow(CurrentTaxLabel, CurrentTaxRate));
            TL.Rows.Add(new TableRow(CurrentTotalLabel, CurrentTotal));
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
            List<(string, TextAlignment, bool)> HeaderEntriesBatchSelect = new()
            {
                ("Name", TextAlignment.Right, false),
                ("Batch Code", TextAlignment.Left, true),
                ("Price", TextAlignment.Right, false),
            };

            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SearchCatalogue = PR.Catalogue
                .Select<PosCatalogue, (string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>
                //(e => { return (e.ToStringArray(), Randomizers.GetRandomBgColor(), Randomizers.GetRandomFgColor()); })
                (e => { return (e.ToStringArray(), null, null); })
                .ToList();
            Barcode.KeyDown += (e, a) => {
                SearchDialog SD = new SearchDialog(SearchCatalogue, HeaderEntries);
                SD.ShowModal();
                MessageBox.Show(String.Concat(SD.Selected), "Selected", MessageBoxType.Information);
                long SelectedItemcode = long.Parse(SD.Selected[0]);
                var SelectedItem = PR.Catalogue.Where(x => x.itemcode == SelectedItemcode).First();
                long BatchCount = PR.Batches.Where(x => x.itemcode == long.Parse(SD.Selected[0])).Count();
                MessageBox.Show(BatchCount.ToString(), "Batch Count", MessageBoxType.Information);
                long batchcode;
                if (BatchCount > 1)
                {
                    List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> BatchSelectList = PR.Batches.Where(x => x.itemcode == long.Parse(SD.Selected[0])).Select<PosBatch, (string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>(x => { return (new string[] { SD.Selected[1], x.batchcode.ToString(), x.marked.ToString() }, null, null); }).ToList();
                    var BatchSelect = new SearchDialog(BatchSelectList, HeaderEntriesBatchSelect);
                    BatchSelect.ShowModal();
                    batchcode = long.Parse(BatchSelect.Selected[1]);
                    VatCategory.Text = SelectedItem.DefaultVatCategory.ToString();
                    VatCategory.Focus();
                }
                else if (BatchCount == 1)
                {
                    batchcode = PR.Batches.Where(x => x.itemcode == long.Parse(SD.Selected[0])).First().batchcode;
                    VatCategory.Text = SelectedItem.DefaultVatCategory.ToString();
                    VatCategory.Focus();
                }
                else return;
            };
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
