using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommonUi;
using Eto.Forms;
using RV.InvNew.Common;

namespace EtoFE.Panels
{
    public class AllAccountsBalances : Panel
    {
        public AllAccountsBalances()
        {
            var LocalColor = ColorSettings.GetPanelSettings(
                "Editor",
                (IReadOnlyDictionary<string, object>)Program.ConfigDict
            );
            LocalColor = ColorSettings.RotateAllToPanelSettings(0);
            BackgroundColor = LocalColor?.BackgroundColor ?? ColorSettings.BackgroundColor;
            List<AccountsType> ACs = new();
            List<AccountsBalance> ABs = new();
            List<AccountsInformation> AIs = new();
            while (true)
            {
                var req = (
                    SendAuthenticatedRequest<string, List<AccountsType>>.Send(
                        "Refresh",
                        "/GetAccountsTypes",
                        true
                    )
                );
                var bal = (
                    SendAuthenticatedRequest<string, List<AccountsBalance>>.Send(
                        "Refresh",
                        "/GetAccountsBalances",
                        true
                    )
                );
                var info = (
                    SendAuthenticatedRequest<string, List<AccountsInformation>>.Send(
                        "Refresh",
                        "/GetAccountsInformation",
                        true
                    )
                );
                //req.ShowModal();
                if (bal.Error == false)
                {
                    ABs = bal.Out;
                }
                if (info.Error == false)
                {
                    AIs = info.Out;
                }
                if (req.Error == false)
                {
                    ACs = req.Out;
                    MessageBox.Show(
                        JsonSerializer.Serialize(req.Out),
                        "Got this",
                        MessageBoxType.Information
                    );
                    break;
                }
            }
            var J = AIs.Join(
                ABs,
                ai => new { ai.AccountType, ai.AccountNo },
                ab => new { ab.AccountType, ab.AccountNo },
                (ai, ab) =>
                    new
                    {
                        AccountType = ai.AccountType,
                        AccountNo = ai.AccountNo,
                        AccountName = ai.AccountName,
                        AccountBalance = ab.Amount,
                    }
            );
            Content = SearchPanelUtility.GenerateSearchPanel(J.ToList(), false);
        }
    }
}
