// See https://aka.ms/new-console-template for more information
using CommonUi;
using Eto.Forms;
using EtoFE;
using RV.LabelRetriever;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Eto.Forms;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text.Json;
using RV.InvNew.Common;
using System.Linq;

Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
var ActionsMap = new Dictionary<string, (ShowAndGetValue, LookupValue)>();
LabelRetriever.Initialize();
var SampleJson = JsonSerializer.Serialize(new ItemLabel());
Console.WriteLine(SampleJson);


new Application(Eto.Platforms.Wpf).Run(new MainForm());
class MainForm: Eto.Forms.Form
{
    public MainForm()
    {
        var ActionsMap = new Dictionary<string, (ShowAndGetValue, LookupValue)>();
        List<(string, TextAlignment, bool)> HeaderEntries = new()
            {
                ("Itemcode", TextAlignment.Right, true),
                ("en:name", TextAlignment.Left, false),
                ("தமிழ்", TextAlignment.Left, false),
                ("සිංහල", TextAlignment.Left, false),
            };
        List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SearchCatalogue = LabelRetriever.I18nLabelsWithDefault.
                        Select<KeyValuePair<int, (string Default, string Tamil, string Sinhala)>, (string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>
                        //(e => { return (e.ToStringArray(), Randomizers.GetRandomBgColor(), Randomizers.GetRandomFgColor()); })
                        (e =>
                        {
                            return ([e.Key.ToString(), e.Value.Default, e.Value.Tamil, e.Value.Sinhala], null, null);
                        })
                        .ToList();
        //var SearchDialog = 
        //new Eto.Forms.Application(Eto.Platforms.Wpf).Run(new SearchDialogEto(SearchCatalogue, HeaderEntries));
        var SearchDialog = new SearchDialogEto(SearchCatalogue, HeaderEntries);
        SearchDialog.ShowModal();
        (new Form()
        {
            Content = new GenEtoUI(
            SimpleJsonToUISerialization.ConvertToUISerialization(JsonSerializer.Serialize(new { Itemcode = int.Parse(SearchDialog.Selected[0]), English = SearchDialog.Selected[1], Tamil = SearchDialog.Selected[2], Sinhala = SearchDialog.Selected[3] })),
            (_) =>
            {
                return 100;
            },
            (_) =>
            {
                return 100;
            },
            ActionsMap,
            null
        ),
        }).Show();
    }

}


[JsonSerializable(typeof(ItemLabel))]
public class ItemLabel
{
    public int itemcode { get; set; }
    public string label_i18n_ta { get; set; }
    public string label_i18n_si { get; set; }
}
