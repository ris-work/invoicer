using CommonUi;
using Eto.Forms;
using FormsGenerationGallery;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using RV.InvNew.Common;
using System.Text.Json;
using Terminal.Gui;
using MyAOTFriendlyExtensions;

Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

//using Terminal.Gui;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
var CT = new CatalogueTransfer
{
    Active = true,
    ActiveWeb = true,
    CreatedOn = DateTime.Now,
    DefaultVatCategory = 0,
    Description = "Hello",
    DescriptionPos = "Goodnight",
    DescriptionsOtherLanguages = 0,
    DescriptionWeb = "Web",
    EnforceAboveCost = true,
    ExpiryTrackingEnabled = false,
    Itemcode = 0,
    PermissionsCategory = 1,
    PriceManual = false,
    VatCategoryAdjustable = false,
    VatDependsOnUser = false,
};
var ActionsMap = new Dictionary<string, (ShowAndGetValue, LookupValue)>
{
    {
        "one",
        (
            () =>
            {
                Eto.Forms.MessageBox.Show("one", "InputHandler", MessageBoxType.Information);
                return 100;
            },
            (x) =>
            {
                Eto.Forms.MessageBox.Show("one", "InputHandler", MessageBoxType.Information);
                return "Hello";
            }
        )
    },
    {
        "VatChooser",
        (
            () =>
            {
                Eto.Forms.MessageBox.Show("one", "InputHandler", MessageBoxType.Information);
                return 101;
            },
            (x) =>
            {
                Eto.Forms.MessageBox.Show("one", "InputHandler", MessageBoxType.Information);
                return "Default VAT";
            }
        )
    },
    {
        "LangChooser",
        (
            () =>
            {
                Eto.Forms.MessageBox.Show("one", "InputHandler", MessageBoxType.Information);
                return 102;
            },
            (x) =>
            {
                Eto.Forms.MessageBox.Show("one", "InputHandler", MessageBoxType.Information);
                return "Default Language (editor to launch)";
            }
        )
    },
};

var SampleJson =
    @"{""name"": ""name"",""localName"": ""பெயர், नमस्ते"",""float"": 1.2,""location"": ""ஊர் பெயர்"",""ஊர் பெயர்"": ""திருகோணமலை"", ""long"": 65536, ""bool"": true}";
if (args.Length >= 1 && File.Exists(args[0]))
{
    try
    {
        SampleJson = File.ReadAllText(args[0]);
    }
    catch (Exception E)
    {
        Console.WriteLine($"Error reading {args[0]}: {E.ToString()}, {E.StackTrace}");
    }
}

/*
new Eto.Forms.Application().Run(
    new Form()
    {
        Content = new GenEtoUI(
            CT.ToDict(),
            (_) =>
            {
                Eto.Forms.MessageBox.Show(
                    "Clicked save",
                    "Event received",
                    MessageBoxType.Information
                );
                return 100;
            },
            (_) =>
            {
                Eto.Forms.MessageBox.Show(
                    "Clicked save",
                    "Event received",
                    MessageBoxType.Information
                );
                return 100;
            },
            ActionsMap,
            null
        ),
    }
);
*/

new Eto.Forms.Application().Run(
    new Form()
    {
        Content = new GenEtoUI(
            SimpleJsonToUISerialization.ConvertToUISerialization(SampleJson),
            (_) =>
            {
                return 100;
            },
            (_) =>
            {
                return 100;
            },
            ActionsMap,
            null,
            true
        ),
    }
);
var config = ReceiptPrinter.LoadConfig("theme.toml");

var invoiceItems = new List<string[]>
{
    new string[] { "Item1 பெயர், नमस्ते 1", "2", "$20.00" },
    new string[] { "Item2 பெயர், नमस्ते 2", "1", "$10.00" },
    new string[] { "Item3 பெயர், नमस्ते 3", "5", "$50.00" },
};
var fieldRemovalTestObjectInput = new { a = "Hello", b = "World", c = "Everyone!", ShouldBeRemoved1 = "You should not see this", ShouldBeRemoved2 = "You should not see this!" };
Eto.Forms.MessageBox.Show($"Field removal: {Environment.NewLine}Input: {JsonSerializer.Serialize(fieldRemovalTestObjectInput)}{Environment.NewLine}Output: {JsonSerializer.Serialize(fieldRemovalTestObjectInput.RemoveFieldIfPresent("ShouldBeRemoved1").RemoveFieldIfPresent("ShouldBeremoved2"))}", "Field Removal Test");
var fieldUpdateTestObjectInput = new { a = "Hello", b = "World", c = "Everyone!", ShouldBeUpdated1 = "You should not see this", ShouldBeUpdated2 = "You should not see this!", ShouldNotBeUpdated1 = "You should see this" };
Eto.Forms.MessageBox.Show($"Field update: {Environment.NewLine}Input: {JsonSerializer.Serialize(fieldUpdateTestObjectInput)}{Environment.NewLine}Output: {JsonSerializer.Serialize(fieldUpdateTestObjectInput.ApplyChangesExceptFilteredFromJson(["a", "ShouldNotbeupdated1"],JsonSerializer.Serialize( new {a = "You should not see this", ShouldBeUpdated1 = "This is correct", ShouldBeUpdated2 = "This is correct", ShouldNotBeUpdated1 = "You should not see this" })))}", "Field Update Test");
var receiptPrinter = new ReceiptPrinter(invoiceItems, config);
receiptPrinter.PrintReceipt();


Terminal.Gui.Application.Init();

/*
Terminal.Gui.Application.Run(
    new GenTopLevel(
        CT.ToDict(),
        (_) =>
        {
            Eto.Forms.MessageBox.Show("Clicked save", "Event received", MessageBoxType.Information);
            return 100;
        },
        (_) =>
        {
            Eto.Forms.MessageBox.Show("Clicked save", "Event received", MessageBoxType.Information);
            return 100;
        },
        ActionsMap,
        null
    )
);
*/

var newMauiWindow = (
    new GenMaUI(
        SimpleJsonToUISerialization.ConvertToUISerialization(SampleJson),
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
    )
);
var mauiBuilder = MauiApp.CreateBuilder().UseMauiApp<MauiAppl>((_) => new MauiAppl(newMauiWindow));
var mauiApp = mauiBuilder.ConfigureFonts().Build();

//mauiApp.
//

//Microsoft.Maui.Controls.Application.Current.OpenWindow(newMauiWindow);

Terminal.Gui.Application.Run(
    new GenTopLevel(
        SimpleJsonToUISerialization.ConvertToUISerialization(SampleJson),
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
    )
);

