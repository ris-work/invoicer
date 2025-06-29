using CommonUi;
using Eto.Forms;
using RV.InvNew.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtoFE.Panels
{
    public class AllJournalEntries: Panel
    {
        public AllJournalEntries()
        {
            var LocalColor = ColorSettings.GetPanelSettings(
                "Editor",
                (IReadOnlyDictionary<string, object>)Program.ConfigDict
            );
            LocalColor = ColorSettings.RotateAllToPanelSettings(0);
            BackgroundColor = LocalColor?.BackgroundColor ?? ColorSettings.BackgroundColor;
            List<AccountsJournalEntry> JEs;
            while (true)
            {
                var req = (
                    SendAuthenticatedRequest<string, List<AccountsJournalEntry>>.Send(
                        "Refresh",
                        "/GetAllJournalEntries",
                        true
                    )
                );
                //req.ShowModal();
                if (req.Error == false)
                {
                    JEs = req.Out;
                    //MessageBox.Show(req.Response.Catalogue.Count.ToString(), "Time", MessageBoxType.Information);
                    break;
                }
            }
            Content = SearchPanelUtility.GenerateSearchPanel(JEs, false);
        }
    }
}
