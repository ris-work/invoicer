// See https://aka.ms/new-console-template for more information
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using CommonUi;
using Eto.Forms;
using Eto.Forms;
using EtoFE;
using RV.InvNew.Common;
using RV.LabelRetriever;

Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
while (true)
{
    var ActionsMap = new Dictionary<string, (ShowAndGetValue, LookupValue)>();
    LabelRetriever.Initialize();
    var SampleJson = JsonSerializer.Serialize(new ItemLabel());
    Console.WriteLine(SampleJson);

    new Application(Eto.Platforms.Wpf).Run(new MainForm());
}

class MainForm : Eto.Forms.Form
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
        List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SearchCatalogue = LabelRetriever
            .I18nLabelsWithDefault.Select<
                KeyValuePair<long, (string Default, string Tamil, string Sinhala)>,
                (string[], Eto.Drawing.Color?, Eto.Drawing.Color?)
            >
            //(e => { return (e.ToStringArray(), Randomizers.GetRandomBgColor(), Randomizers.GetRandomFgColor()); })
            (e =>
            {
                return (
                    [e.Key.ToString(), e.Value.Default, e.Value.Tamil, e.Value.Sinhala],
                    null,
                    null
                );
            })
            .ToList();
        //var SearchDialog =
        //new Eto.Forms.Application(Eto.Platforms.Wpf).Run(new SearchDialogEto(SearchCatalogue, HeaderEntries));
        var SearchDialog = new SearchDialogEto(SearchCatalogue, HeaderEntries);
        SearchDialog.ShowModal();
        if(SearchDialog.Selected != null)(
            new Form()
            {
                Content = new GenEtoUI(
                        LabelRetriever.I18nLabelsOriginal[(int.Parse(SearchDialog.Selected[0]))].ToDict()
                    ,
                    (v) =>
                    {
                        LabelRetriever.HC.PostAsJsonAsync<ItemLabel>("https://in.test.vz.al/i18n/upsert.php", ItemLabel.FromDictionary(v.ToDictionary())).GetAwaiter().GetResult();
                        return 100;
                    },
                    (v) =>
                    {
                        LabelRetriever.HC.PostAsJsonAsync<ItemLabel>("https://in.test.vz.al/i18n/upsert.php", ItemLabel.FromDictionary(v.ToDictionary())).GetAwaiter().GetResult();
                        return 100;
                    },
                    ActionsMap,
                    "Itemcode"
                )
                ,
            }
        ).Show();
        //this.Close();
        Button Restart = new Button() { Text = "Restart" };
        Restart.Click += (_, _) => { this.Close(); };
    }
}

