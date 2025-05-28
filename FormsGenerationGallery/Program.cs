using System.Text.Json;
using common;
using CommonUi;
using Eto.Forms;
using FormsGenerationGallery;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using MyAOTFriendlyExtensions;
using MyImageProcessing;
using MySkiaApp;
using Rv.InvNew.Common;
using RV.InvNew.Common;
using Terminal.Gui;

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
ColorSettings.BackgroundColor = Eto.Drawing.Color.FromGrayscale(1);
ColorSettings.ForegroundColor = Eto.Drawing.Color.FromGrayscale(0);
ColorSettings.LesserBackgroundColor = Eto.Drawing.Color.FromGrayscale(0.3f);
ColorSettings.LesserForegroundColor = Eto.Drawing.Color.FromGrayscale(0.8f);
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
    @"{""name"": ""name"",""localName"": ""பெயர், नमस्ते"",""float"": 1.2,""location"": ""ஊர் பெயர்"",""ஊர் பெயர்"": ""திருகோணமலை"", ""long"": 65536, ""bool"": true, ""price"": 600.0, ""pdiscount"": 10.0, ""adiscount"": 11.0, ""today"": ""2024"" }";
var SampleJsonNested =
    @"{
    ""identity"": {
        ""firstName"": ""John"",
        ""lastName"": ""Doe"",
        ""localName"": ""जॉन डो, ஜான் டோ, ジョン・ドウ""
    },
    ""location"": {
        ""country"": ""Sri Lanka"",
        ""city"": ""Colombo"",
        ""province"": {
            ""english"": ""Western Province"",
            ""native"": ""மேற்கு மாகாணம்""
        }
    },
    ""misc"": {
        ""floatValue"": 3.14159,
        ""bool"": true,
        ""longArray"": [65536, 131072, 262144]
    },
    ""funFacts"": {
        ""hobbies"": [
            ""Cricket"",
            ""Chess"",
            ""映画を見る"",
            ""సంగీతం"",
            ""Приключение""
        ],
        ""favoriteQuote"": {
            ""en"": ""The universe is full of surprises, each a testament to nature’s elegant design."",
            ""jp"": ""宇宙は驚きに満ちており、そのすべてが自然の優雅な設計の証です."",
            ""hi"": ""ब्रह्मांड आश्चर्यों से भरा है, प्रत्येक एक प्रकृति के सुंदर डिजाइन का प्रमाण है।"",
            ""ru"": ""Вселенная полна сюрпризов, и каждый из них — свидетельство элегантного замысла природы.""
        }
    },
    ""culturalNotes"": {
        ""festivals"": [
            ""Diwali"",
            ""New Year"",
            ""பொங்கல்"",
            ""අලුත් අවුරුද්ද""
        ],
        ""languagesSpoken"": [
            ""English"",
            ""සිංහල"",
            ""தமிழ்"",
            ""हिंदी"",
            ""日本語"",
            ""русский""
        ],
        ""traditionalMusic"": {
            ""instrument"": ""Veena"",
            ""style"": ""Carnatic""
        }
    }
}";

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
JsonSerializerOptions JSOptions = new();
JSOptions.Converters.Add(new ForceDoubleConverter());

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

var AC = new Eto.Forms.Application();

//Image: https://www.flickr.com/photos/ellaolsson/48839117163/
//Copyright (c) Ella Olson 2019, CC-BY 2.0
Panel imagePanel = ImagePanelFactory.CreateImagePanel("large_image.jpg", 100);
Panel imagePanelSkia = ImagePanelFactorySkia.CreateImagePanel("large_image.jpg", 100);
TextBox DiscountMarkupText = new TextBox() { Text = "10.52" };
var SuggestedPrices = SampleDataGenerators.GenerateSuggestedPrices();
var VatCategories = SampleDataGenerators.GenerateVatCategories();
var AccountsInformation = SampleDataGenerators.GenerateAccountsInformation();
var PIISamples = SampleDataGenerators.GeneratePiiList();
Form RandomSearchForm = new Form()
{
    Content = new Eto.Forms.StackLayout(
        new Eto.Forms.StackLayout(
            SearchPanelUtility.GenerateSearchPanel(SuggestedPrices, debug: false),
            SearchPanelUtility.GenerateSearchPanel(VatCategories, debug: false)
        )
        {
            Orientation = Eto.Forms.Orientation.Horizontal,
        },
        new Eto.Forms.StackLayout(
            SearchPanelUtility.GenerateSearchPanel(AccountsInformation, debug: false),
            SearchPanelUtility.GenerateSearchPanel(PIISamples, debug: false)
        )
        {
            Orientation = Eto.Forms.Orientation.Horizontal,
        }
    ),
};
var SearchUIButton = new Eto.Forms.Button() { Text = "Launch sample SearchUI" };
SearchUIButton.Click += (_, _) =>
{
    RandomSearchForm.Show();
};
var PurchasingUIButton = (
    new Eto.Forms.Button() { Text = "Launch ReceivedInvoices UI with Purchases" }
);
var SamplePurchasePanel = new PurchasePanel();
List<Purchase> LP = SampleDataGenerator.GetSampleValidPurchases();
SamplePurchasePanel.Render(SampleDataGenerator.GetSampleValidPurchases());
var PurchaseDataEntryForm = new GenEtoUI(
    SimpleJsonToUISerialization.ConvertToUISerialization(
        JsonSerializer.Serialize(LP[0], JSOptions)
    ),
    (e) =>
    {
        Eto.Forms.MessageBox.Show(
            JsonSerializer.Serialize(e),
            "Serialized",
            MessageBoxType.Information
        );
        LP.Add(JsonSerializer.Deserialize<Purchase>(JsonSerializer.Serialize(e)));
        SamplePurchasePanel.Render(LP);
        return 100;
    },
    (e) =>
    {
        Eto.Forms.MessageBox.Show(
            $"{JsonSerializer.Serialize(e)}, {JsonSerializer.Deserialize<Purchase>(JsonSerializer.Serialize(e)).Validate().ErrorDescription}",
            "Serialized",
            MessageBoxType.Information
        );
        LP.Add(JsonSerializer.Deserialize<Purchase>(JsonSerializer.Serialize(e)));
        SamplePurchasePanel.Render(LP);
        return 100;
    },
    ActionsMap,
    null,
    false,
    [],
    null,
    PanelGenerators.Defaults(),
    new Dictionary<string[], (string, string)>
    {
        { ["adiscount", "pdiscount"], ("DiscountPanel", "price") },
        { ["today"], ("DatePickerPanel", null) },
        { ["ExpiryDate"], ("DatePickerPanel", null) },
        { ["ManufacturingDate"], ("DatePickerPanel", null) },
        { ["DiscountAbsolute", "DiscountPercentage"], ("DiscountPanel", "GrossTotal") },
        { ["VatAbsolute", "VatPercentage"], ("VatPanel", "NetTotal") },
        { ["GrossMarkupAbsolute", "GrossMarkupPercentage"], ("MarkupPanel", "NetCostPerUnit") }
    }
);
PurchaseDataEntryForm.AnythingChanged = () =>
{
    //double PackSize = double.Parse(((TextBox)PurchaseDataEntryForm._Einputs["PackSize"]).Text);
    //Eto.Forms.MessageBox.Show($"{PackSize.ToString()}, {PurchaseDataEntryForm.Lookup("PackSize")}", "Title");
    //ExternalLabelCalculated.Text = GeneratedEtoUISample.SerializeIfValid();
    var P = PurchaseDataEntryForm;
    try
    {
        // Retrieve the underlying values using Lookup() so that
        // we do not overwrite any user-edited uncomputed values.
        long packSize = (long)P.Lookup("PackSize");
        long packQuantity = (long)P.Lookup("PackQuantity");
        long freePacks = (long)P.Lookup("FreePacks");
        Console.WriteLine("Did not except yet");

        double receivedAsUnitQuantity = (double)P.Lookup("ReceivedAsUnitQuantity");
        double freeUnits = (double)P.Lookup("FreeUnits");
        Console.WriteLine("Did not except yet");

        double costPerPack = (double)P.Lookup("CostPerPack");
        double costPerUnit = (double)P.Lookup("CostPerUnit");
        double discountAbsolute = (double)P.Lookup("DiscountAbsolute");
        double vatAbsolute = (double)P.Lookup("VatAbsolute");
        Console.WriteLine("Did not except yet");

        // Calculate total units including both paid and free units.
        double totalUnits =
            ((packQuantity + freePacks) * packSize) + receivedAsUnitQuantity + freeUnits;

        // Compute the GrossTotal using the updated formula.
        // Note that free packs/units are not charged.
        double grossTotal = (packQuantity * costPerPack) + (receivedAsUnitQuantity * costPerUnit);

        // Calculate the net total price and the total amount due.
        double netTotalPrice = grossTotal - discountAbsolute;
        double totalAmountDue = netTotalPrice + vatAbsolute;

        // Update computed fields only so that user-edited values remain unchanged.
        P.SetValue("TotalUnits", totalUnits);
        P.SetValue("GrossTotal", grossTotal);
        P.SetValue("NetTotalPrice", netTotalPrice);
        P.SetValue("TotalAmountDue", totalAmountDue);
    }
    catch (Exception ex)
    {
        // Log or handle the exception as necessary.
        Console.WriteLine("Error recalculating computed fields: " + ex.Message + ex.StackTrace);
    }
    //((TextBox)GeneratedEtoUISample._Einputs["float"]).Text = (long.Parse(((TextBox)GeneratedEtoUISample._Einputs["long"]).Text)/3).ToString(); --> Works
};

PurchasingUIButton.Click += (_, _) =>
{
    SamplePurchasePanel.DeleteReceivedInvoiceItem = (i) =>
    {
        System.Console.WriteLine($"Requested to remove: {i}");
        LP.RemoveAt(i);
        SamplePurchasePanel.Render(LP);
    };
    var F = new Eto.Forms.Form()
    {
        Content = new Eto.Forms.StackLayout(PurchaseDataEntryForm, SamplePurchasePanel),
    };
    F.Show();
};
var GeneratedEtoUISample = new GenEtoUI(
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
    false,
    ["localName"],
    null,
    PanelGenerators.Defaults(),
    new Dictionary<string[], (string, string)>
    {
        { ["adiscount", "pdiscount"], ("DiscountPanel", "price") },
        { ["today"], ("DatePickerPanel", null) },
    },
    ["price"]
);
var ExternalCalculateButton = new Eto.Forms.Button() { Text = "Run external calculation" };
ExternalCalculateButton.Click += (_, _) =>
{
    Eto.Forms.MessageBox.Show(
        GeneratedEtoUISample.SerializeIfValid(),
        Eto.Forms.MessageBoxType.Information
    );
};
var ExternalLabelCalculated = new Eto.Forms.Button() { Text = "Externally calculated" };
GeneratedEtoUISample.AnythingChanged = () => { };
var ExternalWatcher = new Eto.Forms.StackLayout(ExternalCalculateButton, ExternalLabelCalculated)
{ };

AC.Run(
    new Form()
    {
        Content = new Eto.Forms.Scrollable()
        {
            Content = new Eto.Forms.StackLayout(
                GeneratedEtoUISample,
                new Eto.Forms.StackLayout(
                    new StackLayoutItem(null, true),
                    new StackLayoutItem(imagePanel, false),
                    new StackLayoutItem(null, true),
                    new StackLayoutItem(imagePanelSkia, false),
                    new StackLayoutItem(null, true)
                )
                {
                    Orientation = Eto.Forms.Orientation.Horizontal,
                    HorizontalContentAlignment = Eto.Forms.HorizontalAlignment.Stretch,
                },
                ExternalWatcher,
                PurchasingUIButton,
                SearchUIButton,
                DiscountMarkupText,
                new DiscountMarkupPanel(
                    DiscountMarkupText,
                    "Discount",
                    Eto.Forms.Orientation.Vertical,
                    Mappings: ["a", "b"]
                ),
                new JsonEditorExample.JsonEditorPanel(SampleJson, Eto.Forms.Orientation.Horizontal),
                new JsonEditorExample.JsonEditorPanel(
                    SampleJsonNested,
                    Eto.Forms.Orientation.Vertical
                )
            )
            {
                HorizontalContentAlignment = Eto.Forms.HorizontalAlignment.Stretch,
            },
        },
    }
);
var config = ReceiptPrinter.LoadConfig("theme.toml");

var invoiceItems = new List<string[]>
{
    new string[] { "Item1 பெயர், नमस्ते 1", "2", "$20.00" },
    new string[] { "Item2 பெயர், नमस्ते 2", "1", "$10.00" },
    new string[] { "Item3 பெயர், नमस्ते 3", "5", "$50.00" },
};
var fieldRemovalTestObjectInput = new
{
    a = "Hello",
    b = "World",
    c = "Everyone!",
    ShouldBeRemoved1 = "You should not see this",
    ShouldBeRemoved2 = "You should not see this!",
};
Eto.Forms.MessageBox.Show(
    $"Field removal: {Environment.NewLine}Input: {JsonSerializer.Serialize(fieldRemovalTestObjectInput)}{Environment.NewLine}Output: {JsonSerializer.Serialize(fieldRemovalTestObjectInput.RemoveFieldIfPresent("ShouldBeRemoved1").RemoveFieldIfPresent("ShouldBeremoved2"))}",
    "Field Removal Test"
);
var fieldUpdateTestObjectInput = new
{
    a = "Hello",
    b = "World",
    c = "Everyone!",
    ShouldBeUpdated1 = "You should not see this",
    ShouldBeUpdated2 = "You should not see this!",
    ShouldNotBeUpdated1 = "You should see this",
};
Eto.Forms.MessageBox.Show(
    $"Field update: {Environment.NewLine}Input: {JsonSerializer.Serialize(fieldUpdateTestObjectInput)}{Environment.NewLine}Output: {JsonSerializer.Serialize(fieldUpdateTestObjectInput.ApplyChangesExceptFilteredFromJson(["a", "ShouldNotbeupdated1"], JsonSerializer.Serialize(new { a = "You should not see this", ShouldBeUpdated1 = "This is correct", ShouldBeUpdated2 = "This is correct", ShouldNotBeUpdated1 = "You should not see this" })))}",
    "Field Update Test"
);
var AclRemovalInput = new
{
    allowed = "allowed",
    denied = "You should not see this",
    DeniedCaseInsensitive = "You should not see this",
};
string PatchJson = AclRemovalInput.RemoveRelevantDenyFields(["denied", "deniedcaseinsensitive"]);
Eto.Forms.MessageBox.Show($"Input: {AclRemovalInput.ToJson()} Output: {PatchJson}");
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
