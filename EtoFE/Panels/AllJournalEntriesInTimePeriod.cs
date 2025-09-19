using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonUi;
using Eto.Forms;
using RV.InvNew.Common;

namespace EtoFE.Panels
{
    public class AllJournalEntriesInTimePeriod : Panel
    {
        public AllJournalEntriesInTimePeriod()
        {
            var LocalColor = ColorSettings.GetPanelSettings(
                "Editor",
                (IReadOnlyDictionary<string, object>)Program.ConfigDict
            );
            LocalColor = ColorSettings.RotateAllToPanelSettings(0);
            BackgroundColor = LocalColor?.BackgroundColor ?? ColorSettings.BackgroundColor;
            List<AccountsJournalEntry> JEs;

            Button LoadResults = new Button()
            {
                Height = ColorSettings.ControlHeight ?? 30,
                Width = ColorSettings.ControlWidth ?? 150,
                Text = "Load Entries",
            };
            DateTimePicker DTFrom = new DateTimePicker()
            {
                Height = ColorSettings.ControlHeight ?? 30,
                Width = ColorSettings.ControlWidth ?? 150,
            };
            DateTimePicker DTTo = new DateTimePicker()
            {
                Height = ColorSettings.ControlHeight ?? 30,
                Width = ColorSettings.ControlWidth ?? 150,
            };
            Label LblDTo = new Label() { Text = "To" };
            Label LblDFrom = new Label() { Text = "From" };
            Panel ContentPanel = new() { Width = 300, Height = 800 };

            TableLayout TLInput = new TableLayout();
            TLInput.Rows.Add(new TableRow(new TableCell(LblDFrom), new TableCell(DTFrom)));
            TLInput.Rows.Add(new TableRow(new TableCell(LblDTo), new TableCell(DTTo)));
            var SL = new StackLayout(TLInput, LoadResults, ContentPanel)
            {
                Orientation = Orientation.Vertical,
                Width = 600,
                Height = 800,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
            Content = SL;
            LoadResults.Click += (_, _) =>
            {
                while (true)
                {
                    var req = (
                        SendAuthenticatedRequest<TimePeriod, List<AccountsJournalEntry>>.Send(
                            new TimePeriod(
                                from: DTFrom.Value ?? DateTime.Now.AddDays(-1),
                                to: DTTo.Value ?? DateTime.Now.AddDays(-1)
                            ),
                            "/GetAllJournalEntriesWithinTimePeriod",
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
                ContentPanel.Content = SearchPanelUtility.GenerateSearchPanel(
                    JEs,
                    false,
                    null,
                    [
                        "TimeAsEntered",
                        "DebitAccountName",
                        "CreditAccountName",
                        "Amount",
                        "JournalNo",
                        "PrincipalName",
                    ]
                );
                SL.Invalidate();
            };
        }
    }
}
