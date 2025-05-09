using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommonUi;
using Eto.Forms;
using RV.InvNew;
using RV.InvNew.Common;

namespace EtoFE
{
    public class CatalogueEditPanel : Panel
    {
        public CatalogueEditPanel()
        {
            
            PosRefresh PR;
            while (true)
            {
                var req = (
                    SendAuthenticatedRequest<string, PosRefresh>.Send(
                        "Refresh",
                        "/PosRefreshBearerAuth",
                        true
                    )
                );
                //req.ShowModal();
                if (req.Error == false)
                {
                    PR = req.Out;
                    //MessageBox.Show(req.Response.Catalogue.Count.ToString(), "Time", MessageBoxType.Information);
                    break;
                }
            }
            List<(string, TextAlignment, bool)> HeaderEntries = new()
            {
                ("Itemcode", TextAlignment.Right, true),
                ("Name", TextAlignment.Left, false),
                //("Split 1", TextAlignment.Right, false),
                //("Split 2", TextAlignment.Center, false),
                //("Split 3", TextAlignment.Right, false),
                //("Split 4", TextAlignment.Left, true),
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
            var LowerPanelContent = SimpleJsonToUISerialization.ConvertToUISerialization(
                JsonSerializer.Serialize(
                    new Catalogue()
                    {
                        Active = true,
                        ActiveWeb = true,
                        CategoriesBitmask = 0b1,
                        CreatedOn = DateTime.Now,
                        DefaultVatCategory = 0,
                        Description = "",
                        DescriptionPos = "",
                        DescriptionsOtherLanguages = 0,
                        DescriptionWeb = "",
                        EnforceAboveCost = true,
                        ExpiryTrackingEnabled = true,
                        Itemcode = 0,
                        PermissionsCategory = 0,
                        PriceManual = true,
                        VatCategoryAdjustable = true,
                        VatDependsOnUser = true,
                    }
                )
            );
            var LowerPanel = new GenEtoUI(
                LowerPanelContent,
                (a) =>
                {
                    MessageBox.Show(
                        JsonSerializer.Serialize(a),
                        "Serialized",
                        MessageBoxType.Information
                    );
                    return 0;
                },
                (e) =>
                {
                    return 0;
                },
                new Dictionary<string, (CommonUi.ShowAndGetValue, CommonUi.LookupValue)>(),
                "id",
                true
            );
            //MessageBox.Show(JsonSerializer.Serialize(LowerPanelContent));
            var ScrollableLower = new Scrollable() { Content = LowerPanel, Height = 300 };
            SearchBox.OnSelectionMade = () =>
            {
                //MessageBox.Show(String.Join(',', SearchBox.Selected));

                var A = SendAuthenticatedRequest<long, Catalogue>.Send(
                    long.Parse(SearchBox.Selected[0]),
                    "CatalogueRead"
                );
                MessageBox.Show(JsonSerializer.Serialize(A.Out), "Response");
                ScrollableLower.Content = new GenEtoUI(
                    SimpleJsonToUISerialization.ConvertToUISerialization(
                        JsonSerializer.Serialize(A.Out)
                    ),
                    (a) =>
                    {
                        MessageBox.Show(
                            JsonSerializer.Serialize(a),
                            "Serialized",
                            MessageBoxType.Information
                        );
                        return 0;
                    },
                    (e) =>
                    {
                        return 0;
                    },
                    new Dictionary<string, (CommonUi.ShowAndGetValue, CommonUi.LookupValue)>(),
                    "id",
                    true
                );
                ScrollableLower.Invalidate();
            };

            Content = new StackLayout(SearchBox, ScrollableLower);
            //this.ApplyDarkTheme();
        }
    }
}
