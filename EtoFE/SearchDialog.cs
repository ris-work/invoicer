using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Eto.Drawing;
using RV.InvNew.Common;

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
            SearchBox.KeyUp += (e, a) =>
            {
                if (SearchBox.Text.Length > 3)
                {
                    bool searching = true;
                    (new Thread(() =>
                    {
                        List<PosCatalogue> Filtered = PC.Where((x) => x.itemdesc.Contains(SearchBox.Text)).Take(100).ToList();
                        List<GridItem> GI = new List<GridItem>();
                        foreach (var item in Filtered)
                        {
                            GI.Add(new GridItem(item.itemcode, item.itemdesc) { });

                        }
                        searching = false;
                        Results.DataStore = Filtered;
                    })).Start();
                    
                }
            };


            Content = TL;
        }
    }
}
