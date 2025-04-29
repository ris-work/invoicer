using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using RV.InvNew;
using CommonUi;
using RV.InvNew.Common;

namespace EtoFE
{
    public class CatalogueEditPanel: Panel
    {
        public CatalogueEditPanel()
        {
            PosRefresh PR;
            while (true)
            {
                var req = (
                    new AuthenticationForm<string, PosRefresh>(
                        "/PosRefreshBearerAuth",
                        "Refresh",
                        true
                    )
                );
                req.ShowModal();
                if (req.Error == false)
                {
                    PR = req.Response;
                    //MessageBox.Show(req.Response.Catalogue.Count.ToString(), "Time", MessageBoxType.Information);
                    break;
                }
            }
            List<(string, TextAlignment, bool)> HeaderEntries = new()
            {
                ("Itemcode", TextAlignment.Right, true),
                ("Name", TextAlignment.Left, false),
                ("Split 1", TextAlignment.Right, false),
                ("Split 2", TextAlignment.Center, false),
                ("Split 3", TextAlignment.Right, false),
                ("Split 4", TextAlignment.Left, true),
            };
            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SearchCatalogue = PR
                .Catalogue.Select<PosCatalogue, (string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>
                //(e => { return (e.ToStringArray(), Randomizers.GetRandomBgColor(), Randomizers.GetRandomFgColor()); })
                (e =>
                {
                    return (e.ToStringArray(), null, null);
                })
                .ToList();
            var SearchBox = new SearchPanelEto(SearchCatalogue, HeaderEntries, false);
            SearchBox.OnSelectionMade = () => { MessageBox.Show(String.Join(',', SearchBox.Selected)); };
            Content = new StackLayout(SearchBox);
        }
    }
}
