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
            TextBox SearchBox = new TextBox();
            MessageBox.Show($"PC: {PC.Count()}");
            bool searching = false;
            List<PosCatalogue> Filtered = new List<PosCatalogue>();
            List<PosCatalogue> FilteredTemp = new List<PosCatalogue>();
            MessageBox.Show($"Last: {PC.Last().itemcode} Desc: {PC.Last().itemdesc}");
            int FilteredCount = 0;
            SearchBox.KeyDown += (e, a) =>
            {
                if (SearchBox.Text.Length > 0 && searching != true)
                {
                    var searchString = SearchBox.Text.ToLowerInvariant();
                    searching = true;
                    (new Thread(() =>
                    {
                        var FilteredBeforeCounting = PC.AsParallel().Where((x) => x.itemdesc.ToLowerInvariant().Contains(searchString)).AsSequential();
                        FilteredTemp = FilteredBeforeCounting.Take(100).ToList();
                        FilteredCount = Filtered.Count();
                        searching = false;
                        //MessageBox.Show(FilteredCount.ToString());
                    })).Start();
                    
                }
                List<GridItem> GI = new List<GridItem>();
                foreach (var item in FilteredTemp)
                {
                    GI.Add(new GridItem(item.itemcode, item.itemdesc) { });

                }
                Filtered = FilteredTemp;
                Results.DataStore = Filtered;
                this.Title = $"Found {FilteredCount} ";
            };

            TL.Rows.Add(new TableRow(SearchBox));
            TL.Rows.Add(new TableRow(Results));
            Content = TL;
        }
    }
}
