using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Eto.Drawing;
using RV.InvNew.Common;
using SharpDX.Direct2D1;

namespace EtoFE
{
    public class SearchDialog: Dialog
    {
        public SearchDialog(List<PosCatalogue> PC)
        {
            TableLayout TL = new TableLayout();
            Label SL = new Label() { Text = "Search for: " };
            Label LabelResults = new Label() { Text = "Results: " };
            GridView Results = new GridView();
            Results.Columns.Add(new GridColumn { HeaderText = "Itemcode", DataCell = new TextBoxCell(0) });
            Results.Columns.Add(new GridColumn { HeaderText = "Description", DataCell = new TextBoxCell(1) });
            Results.Enabled = true;
            Results.BackgroundColor = Eto.Drawing.Colors.Wheat;
            Results.Size = new Size(500, 500);
            Results.CellFormatting += (e, a) => {
                if (a.Row % 2 == 0)
                {
                    a.BackgroundColor = Eto.Drawing.Colors.Turquoise;
                    a.ForegroundColor = Eto.Drawing.Colors.Black;
                }
            };
            TextBox SearchBox = new TextBox();
            MessageBox.Show($"PC: {PC.Count()}");
            bool searching = false;
            List<PosCatalogue> Filtered = new List<PosCatalogue>();
            List<PosCatalogue> FilteredTemp = new List<PosCatalogue>();
            MessageBox.Show($"Last: {PC.Last().itemcode} Desc: {PC.Last().itemdesc}");
            TL.Padding = 10;
            TL.Spacing = new Eto.Drawing.Size(10, 10);
            int FilteredCount = 0;
            Results.GridLines = GridLines.Both;
            var UpdateView = () => {
                Filtered = FilteredTemp;
                List<GridItem> GI = new List<GridItem>();
                Filtered = FilteredTemp;
                GI.Add(new GridItem("Hello", "World"));
                foreach (var item in Filtered)
                {
                    GI.Add(new GridItem(item.itemcode.ToString(), item.itemdesc));
                }
                //MessageBox.Show(GI.Count().ToString());
                Results.DataStore = GI;
                Results.Invalidate(true);
                this.Invalidate();
                this.Title = $"Found {FilteredCount} ";
            };
            SearchBox.KeyDown += (e, a) =>
            {
                if (SearchBox.Text.Length > 0 && searching != true)
                {
                    var searchString = SearchBox.Text.ToLowerInvariant();
                    searching = true;
                    (new Thread(() =>
                    {
                        var FilteredBeforeCounting = PC.AsParallel().Where((x) => x.itemdesc.ToLowerInvariant().Contains(searchString)).AsSequential();
                        FilteredTemp = FilteredBeforeCounting.Take(1000).ToList();
                        FilteredCount = FilteredBeforeCounting.Count();
                        searching = false;
                        Application.Instance.Invoke(UpdateView);
                        //MessageBox.Show(FilteredCount.ToString());
                    })).Start();
                    
                }
                
            };
            TL.Rows.Add(new TableRow(SearchBox));
            TL.Rows.Add(new TableRow(LabelResults));
            TL.Rows.Add(new TableRow(Results));
            Content = TL;
        }
    }
}
