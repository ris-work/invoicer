using CommonUi;
using Eto.Forms;
using RV.InvNew.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EtoFE.Panels
{
    public class AllAccountsTypes: Panel
    {
        public AllAccountsTypes()
        {
            var LocalColor = ColorSettings.GetPanelSettings(
                "Editor",
                (IReadOnlyDictionary<string, object>)Program.ConfigDict
            );
            LocalColor = ColorSettings.RotateAllToPanelSettings(0);
            BackgroundColor = LocalColor?.BackgroundColor ?? ColorSettings.BackgroundColor;
            List<AccountsType> ACs;
            while (true)
            {
                var req = (
                    SendAuthenticatedRequest<string, List<AccountsType>>.Send(
                        "Refresh",
                        "/GetAccountsTypes",
                        true
                    )
                );
                //req.ShowModal();
                if (req.Error == false)
                {
                    ACs = req.Out;
                    MessageBox.Show(JsonSerializer.Serialize(req.Out), "Got this", MessageBoxType.Information);
                    break;
                }
            }
            Content = SearchPanelUtility.GenerateSearchPanel(ACs, false);
        }
    }
}
