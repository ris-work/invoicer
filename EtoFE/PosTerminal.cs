using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using RV.InvNew.Common;
using Wiry.Base32;

//using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
            Button CheckTimeButton = new Button() { Text = "Check Server Time" };
            string InvoiceLocalKey;
            Padding = new Eto.Drawing.Padding(10, 10);

            TL.Spacing = new Eto.Drawing.Size(20, 20);

            TL.Rows.Add(new TableRow(LabelInvoiceLocalKey));
            TL.Rows.Add(new TableRow(LabelPendingInvoices, LabelPastSuccessfulInvocies));
            TL.Rows.Add(new TableRow(PendingInvoices, PastSuccessfulInvoices));
            TL.Rows.Add(new TableRow(LabelBarcode, Barcode, CurrentItem));
            TL.Rows.Add(new TableRow(LabelPrice, Price));
            TL.Rows.Add(new TableRow(LabelQuantity, Quantity));
            TL.Rows.Add(
                new TableRow(LabelVatCategory, VatCategory, CurrentVatRate, CurrentVatCategory)
            );
            TL.Rows.Add(new TableRow(CurrentTaxLabel, CurrentTaxRate));
            TL.Rows.Add(new TableRow(CurrentTotalLabel, CurrentTotal));
            TL.Rows.Add(new TableRow(CheckTimeButton));
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
                ("Split 4", TextAlignment.Left, true),
            };
            List<(string, TextAlignment, bool)> HeaderEntriesBatchSelect = new()
            {
                ("Name", TextAlignment.Right, false),
                ("Batch Code", TextAlignment.Left, true),
                ("Price", TextAlignment.Right, false),
                ("SIH", TextAlignment.Right, false),
                ("Exp Date", TextAlignment.Right, false),
            };
            CheckTimeButton.Click += (e, a) =>
            {
                while (true)
                {
                    var req = (
                        new AuthenticationForm<string, SingleValueString>("/PermTimeTest", "Time")
                    );
                    req.ShowModal();
                    if (req.Error == false)
                    {
                        MessageBox.Show(req.Response.response, "Time", MessageBoxType.Information);
                        break;
                    }
                }
            };
            var CurrentPosEntriesInGrid = new List<string[]>();
            var CurrentPosEntries = new List<PosSaleEntry>();
            var CurrentPosEntriesGrid = new GridView()
            {
                ShowHeader = true,
                GridLines = GridLines.Both,
            };
            Label TotalPayable = new Label()
            {
                TextColor = Eto.Drawing.Colors.DarkCyan,
                Font = new Eto.Drawing.Font(
                    Eto.Drawing.FontFamilies.MonospaceFamilyName,
                    16,
                    FontStyle.Bold
                ),
            };
            Label TotalPayableInd = new Label()
            {
                TextColor = Eto.Drawing.Colors.DarkCyan,
                Text = "Total Payable:",
                Font = new Eto.Drawing.Font(
                    Eto.Drawing.FontFamilies.MonospaceFamilyName,
                    16,
                    FontStyle.Bold
                ),
            };

            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SearchCatalogue = PR
                .Catalogue.Select<PosCatalogue, (string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>
                //(e => { return (e.ToStringArray(), Randomizers.GetRandomBgColor(), Randomizers.GetRandomFgColor()); })
                (e =>
                {
                    return (e.ToStringArray(), null, null);
                })
                .ToList();
            List<string[]> BatchSelectOutput = new List<string[]> { };
            Barcode.KeyDown += (e, a) =>
            {
                SearchDialog SD = new SearchDialog(SearchCatalogue, HeaderEntries);
                SD.ShowModal();
                
                if (SD.Selected == null) return;
                Barcode.Text = SD.Selected[0];
                MessageBox.Show(String.Concat(SD.Selected), "Selected", MessageBoxType.Information);
                long SelectedItemcode = long.Parse(SD.Selected[0]);
                var SelectedItem = PR.Catalogue.Where(x => x.itemcode == SelectedItemcode).First();
                long BatchCount = PR
                    .Batches.Where(x => x.itemcode == long.Parse(SD.Selected[0]))
                    .Count();
                MessageBox.Show(BatchCount.ToString(), "Batch Count", MessageBoxType.Information);
                long batchcode;
                if (BatchCount > 1)
                {
                    List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> BatchSelectList = PR
                        .Batches.Where(x => x.itemcode == long.Parse(SD.Selected[0]))
                        .Select<PosBatch, (string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>(x =>
                        {
                            return (
                                new string[]
                                {
                                    SD.Selected[1],
                                    x.batchcode.ToString(),
                                    x.marked.ToString(),
                                    x.SIH.ToString(),
                                    x.expireson.ToString("o"),
                                },
                                null,
                                null
                            );
                        })
                        .ToList();
                    var BatchSelect = new SearchDialog(BatchSelectList, HeaderEntriesBatchSelect);
                    BatchSelect.ShowModal();
                    BatchSelectOutput = BatchSelect.OutputList;
                    batchcode = long.Parse(BatchSelect.Selected[1]);
                    Quantity.Focus();
                    VatCategory.Text = SelectedItem.DefaultVatCategory.ToString();
                    //VatCategory.Focus();
                }
                else if (BatchCount == 1)
                {
                    batchcode = PR
                        .Batches.Where(x => x.itemcode == long.Parse(SD.Selected[0]))
                        .First()
                        .batchcode;
                    Quantity.Focus();
                    VatCategory.Text = SelectedItem.DefaultVatCategory.ToString();
                    //VatCategory.Focus();
                }
                else
                    return;
            };
            List<(long, double)> SelectedBatches = new List<(long, double)>();
            Quantity.KeyDown += (e, a) =>
            {
                if (a.Key == Keys.Enter)
                {
                    double SelectQuantity = double.Parse(Quantity.Text);
                    SelectedBatches = new List<(long, double)> { };
                    double CumulativeQuantity = 0;
                    int index = 0;
                    int MaxCount = BatchSelectOutput.Count;
                    MessageBox.Show(MaxCount.ToString(), "Max count", MessageBoxType.Information);
                    string TextDesc = "";
                    while ((CumulativeQuantity <= SelectQuantity) && (MaxCount > index))
                    {
                        long CurrentSelectedBatch = long.Parse(
                            ((BatchSelectOutput.ElementAt(index))[1])
                        );
                        double CurrentSelectedBatchMaxQty = double.Parse(
                            ((BatchSelectOutput.ElementAt(index))[3])
                        );

                        double CurrentSelectQuantity =
                            CurrentSelectedBatchMaxQty < (SelectQuantity - CumulativeQuantity)
                                ? CurrentSelectedBatchMaxQty
                                : (SelectQuantity - CumulativeQuantity);
                        CumulativeQuantity += CurrentSelectQuantity;
                        TextDesc +=
                            $"([Aggregator], {CumulativeQuantity}) + ({CurrentSelectedBatch}, {CurrentSelectQuantity}) <= {CurrentSelectedBatchMaxQty}\r\n";
                        SelectedBatches.Add((CurrentSelectedBatch, CurrentSelectQuantity));
                        if (CumulativeQuantity == SelectQuantity)
                            break;
                        index++;
                    }
                    MessageBox.Show(TextDesc, "Selected Batches", MessageBoxType.Information);
                    VatCategory.Focus();
                }
            };
            var RenderPosEntries = () =>
            {
                CurrentPosEntriesInGrid = new List<string[]>();
                foreach (PosSaleEntry entry in CurrentPosEntries)
                {
                    CurrentPosEntriesInGrid.Add(
                        [
                            entry.state,
                            entry.itemcode.ToString(),
                            entry.batchcode.ToString(),
                            PR.Catalogue.Where(e => e.itemcode == entry.itemcode).First().itemdesc,
                            entry.quantity.ToString(),
                            entry.umarked.ToString(),
                            entry.uselling.ToString(),
                            entry.VatPercent.ToString(),
                            entry.VatAmount.ToString(),
                            entry.Total.ToString(),
                        ]
                    );
                }
                CurrentPosEntriesGrid.DataStore = CurrentPosEntriesInGrid;
                TotalPayable.Text = CurrentPosEntries.Sum((e) => e.Total).ToString("C");
            };
            VatCategory.KeyDown += (e, a) =>
            {
                if (a.Key == Keys.Enter)
                {
                    var SelectedItemCode = int.Parse(Barcode.Text);
                    foreach ((long, double) SelectedBatch in SelectedBatches)
                    {
                        var CurrentBatchToAdd = PR
                            .Batches.Where(b =>
                                b.itemcode == SelectedItemCode && b.batchcode == SelectedBatch.Item1
                            )
                            .First();
                        long CurrentVatCat = PR
                            .VatCategories.Where(v =>
                                v.VatCategoryId == int.Parse(VatCategory.Text)
                            )
                            .FirstOrDefault(
                                (
                                    PR.VatCategories.Where(v =>
                                        v.VatCategoryId
                                        == PR.Catalogue.Where(i => i.itemcode == SelectedItemCode)
                                            .First()
                                            .DefaultVatCategory
                                    )
                                ).First()
                            )
                            .VatCategoryId;
                        CurrentPosEntries.Add(
                            new PosSaleEntry()
                            {
                                itemcode = int.Parse(Barcode.Text),
                                batchcode = int.Parse(Barcode.Text),
                                state = "A",
                                quantity = SelectedBatch.Item2,
                                umarked = CurrentBatchToAdd.marked,
                                uselling = CurrentBatchToAdd.selling,
                                VatCategory = CurrentVatCat,
                                VatPercent = PR
                                    .VatCategories.Where(v => v.VatCategoryId == CurrentVatCat)
                                    .First()
                                    .VatPercentage,
                                Total =
                                    CurrentBatchToAdd.selling
                                    * (SelectedBatch.Item2)
                                    * (
                                        1
                                        + PR.VatCategories.Where(v =>
                                                v.VatCategoryId == int.Parse(VatCategory.Text)
                                            )
                                            .First()
                                            .VatPercentage / 100
                                    ),
                            }
                        );
                    }
                    RenderPosEntries();
                }
                CurrentPosEntriesGrid.Invalidate(true);
            };
            var PosEntriesFields = new (string, TextAlignment?)[]
            {
                ("S", TextAlignment.Right),
                ("Itemcode", TextAlignment.Right),
                ("Batchcode", TextAlignment.Right),
                ("Name", TextAlignment.Left),
                ("Quantity", TextAlignment.Right),
                ("Listed U Price", TextAlignment.Right),
                ("U Price", TextAlignment.Right),
                ("Vat %", TextAlignment.Right),
                ("Vat Amount", TextAlignment.Right),
                ("Total", TextAlignment.Right),
            };

            int gridColCursor = 0;
            foreach ((string, TextAlignment?) field in PosEntriesFields)
            {
                CurrentPosEntriesGrid.Columns.Add(
                    new GridColumn()
                    {
                        HeaderText = field.Item1,
                        DataCell = new TextBoxCell(gridColCursor)
                        {
                            TextAlignment = field.Item2 ?? TextAlignment.Left,
                        },
                    }
                );
                gridColCursor++;
            }

            Content = new StackLayout(
                null,
                TL,
                new StackLayout(CurrentPosEntriesGrid, TotalPayableInd, TotalPayable)
                {
                    Spacing = 10,
                    HorizontalContentAlignment = HorizontalAlignment.Right,
                },
                null
            )
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Padding = 5,
            };
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
