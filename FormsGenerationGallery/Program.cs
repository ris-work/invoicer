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
string[] fieldOrder = new string[]
{
    // Identification & Product Details
    "ReceivedInvoiceId",
    "Itemcode",
    "ProductName",
    
    // Date & Batch Information
    "AddedDate",
    "ManufacturingDate",
    "ExpiryDate",
    "ManufacturerBatchId",
    
    // Quantity Input Fields
    "PackSize",
    "PackQuantity",
    "FreePacks",
    "ReceivedAsUnitQuantity",
    "FreeUnits",
    
    // Computed: Total quantity (paid and free)
    "TotalUnits",
    
    // Cost Input Fields
    "CostPerPack",
    "CostPerUnit",
    
    // Computed: Gross Total (cost for paid items only)
    "GrossTotal",
    
    // Discounts
    "DiscountPercentage",
    "DiscountAbsolute",
    
    // Computed: Net Price before Tax (GrossTotal - DiscountAbsolute)
    "NetTotalPrice",
    
    // Tax Input Fields & Flag (for VAT reclaimability)
    "VatPercentage",
    "VatCategory",
    "VatAbsolute",
    "VatCategoryName",
    "IsVatADisallowedInputTax",
    
    // Computed: Final Amount Due including VAT
    "TotalAmountDue",
    
    // Computed: Average Costs per Unit
    "GrossCostPerUnit",
    "NetCostPerUnit",
    
    // Computed: Final incurred cost (renamed from NetTotal to NetTotalCost)
    "NetTotalCost",
    
    // Sales and Profit Information
    "SellingPrice",
    "GrossMarkupPercentage",
    "GrossMarkupAbsolute",
};


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
        Eto.Forms.MessageBox.Show(JsonSerializer.Deserialize<Purchase>(JsonSerializer.Serialize(e)).Validate().ErrorDescription, "Validation status");
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
        Eto.Forms.MessageBox.Show(JsonSerializer.Deserialize<Purchase>(JsonSerializer.Serialize(e)).Validate().ErrorDescription, "Validation status");
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
        { ["VatAbsolute", "VatPercentage"], ("VatPanel", "NetTotalPrice") },
        { ["GrossMarkupAbsolute", "GrossMarkupPercentage"], ("MarkupPanel", "NetCostPerUnit") }
    }, fieldOrder
);
PurchaseDataEntryForm.AnythingChanged = () =>
{
    //double PackSize = double.Parse(((TextBox)PurchaseDataEntryForm._Einputs["PackSize"]).Text);
    //Eto.Forms.MessageBox.Show($"{PackSize.ToString()}, {PurchaseDataEntryForm.Lookup("PackSize")}", "Title");
    //ExternalLabelCalculated.Text = GeneratedEtoUISample.SerializeIfValid();
    var P = PurchaseDataEntryForm;
    try
    {
        // Retrieve base values using Lookup so that any user-edited values remain intact.
        long packSize = (long)P.Lookup("PackSize");
        long packQuantity = (long)P.Lookup("PackQuantity");
        long freePacks = (long)P.Lookup("FreePacks");

        double receivedAsUnitQuantity = (double)P.Lookup("ReceivedAsUnitQuantity");
        double freeUnits = (double)P.Lookup("FreeUnits");

        // Obtain provided cost details.
        // Note: The initial CostPerUnit is provided by the user and is not recalculated.
        double costPerPack = (double)P.Lookup("CostPerPack");
        double costPerUnit = (double)P.Lookup("CostPerUnit");
        double discountAbsolute = (double)P.Lookup("DiscountAbsolute");
        double vatAbsolute = (double)P.Lookup("VatAbsolute");
        bool isVatADisallowed = (bool)P.Lookup("IsVatADisallowedInputTax");

        // Calculate the total number of units, including free items.
        double totalUnits = ((packQuantity + freePacks) * packSize)
                            + receivedAsUnitQuantity
                            + freeUnits;

        // Compute the gross total: only the cost that is actually charged.
        double grossTotal = (packQuantity * costPerPack)
                            + (receivedAsUnitQuantity * costPerUnit);

        // Compute net total price (price after discount, before tax).
        double netTotalPrice = grossTotal - discountAbsolute;

        // Calculate the final amount due including VAT.
        double totalAmountDue = netTotalPrice + vatAbsolute;

        // Calculate cost per unit values (costs averaged over all physical units).
        // GrossCostPerUnit considers the cost divided over the total units received.
        double grossCostPerUnit = totalUnits > 0 ? grossTotal / totalUnits : 0.0;

        // NetCostPerUnit uses either the net price or the final amount due based on whether VAT is reclaimable.
        double netCostPerUnit = totalUnits > 0
            ? (isVatADisallowed ? totalAmountDue / totalUnits : netTotalPrice / totalUnits)
            : 0.0;

        // Now compute the incurred cost, renamed as NetTotalCost.
        // When VAT cannot be reclaimed, include VAT as an incurred extra cost.
        double netTotalCost = isVatADisallowed ? totalAmountDue : netTotalPrice;

        // Update computed fields and disable editing for calculated values.
        P.SetValue("TotalUnits", totalUnits); P.Disable("TotalUnits");
        P.SetValue("GrossTotal", grossTotal); P.Disable("GrossTotal");
        P.SetValue("NetTotalPrice", netTotalPrice); P.Disable("NetTotalPrice");
        P.SetValue("TotalAmountDue", totalAmountDue); P.Disable("TotalAmountDue");
        P.SetValue("GrossCostPerUnit", grossCostPerUnit); P.Disable("GrossCostPerUnit");
        P.SetValue("NetCostPerUnit", netCostPerUnit); P.Disable("NetCostPerUnit");
        P.SetValue("NetTotalCost", netTotalCost); P.Disable("NetTotalCost");

    }
    catch (Exception ex)
    {
        // Handle or log exceptions as necessary.
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
