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
//using Rv.InvNew.Common;
using RV.InvNew.Common;
using Terminal.Gui;
using System.IO;

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
    @"{""name"": ""name"",""localName"": ""பெயர், नमस्ते"",""float"": 1.2,""location"": ""ஊர் பெயர்"",""ஊர் பெயர்"": ""திருகோணமலை"", ""long"": 65536, ""bool"": true, ""price"": 600.0, ""pdiscount"": 10.0, ""adiscount"": 11.0, ""today"": ""2024-12-12"" }";
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
        ""longArray"": [65536, 131072, 262144],
        ""longArrayNested"": [65536, 131072, 262144, [1,2,3, [4,5,6]]]
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

// Instantiate a RoundedC hosting a Button.
var RC = new RoundedC(new Eto.Forms.Button { Text = "RoundedC Button" })
{
    Width = 200,
    Height = 60,
    Padding = new Eto.Drawing.Padding(10), // uses Panel.Padding
    BorderRadius = 15,
    BackgroundColor = Eto.Drawing.Colors.LightGrey,
    FocusedBackgroundColor = Eto.Drawing.Colors.DarkGray,
    BorderColor = Eto.Drawing.Colors.Black,
    FocusedBorderColor = Eto.Drawing.Colors.Red,
};

// Instantiate a RoundedD hosting a TextBox.
var RD = new RoundedD(new TextBox { PlaceholderText = "Enter text..." })
{
    Width = 200,
    Height = 60,
    Padding = new Eto.Drawing.Padding(10),
    BorderRadius = 15,
    BackgroundColor = Eto.Drawing.Colors.LightYellow,
    FocusedBackgroundColor = Eto.Drawing.Colors.Gold,
    BorderColor = Eto.Drawing.Colors.Black,
    FocusedBorderColor = Eto.Drawing.Colors.Orange,
};

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
List<Purchase> LP = SampleDataGenerator.SampleDataGenerator.GetSampleValidPurchases();
var CloneEFCoreObject = (Purchase O) =>
{
    return JsonSerializer.Deserialize<Purchase>(JsonSerializer.Serialize(O));
};
LP = LP.SelectMany(x =>
    {
        return new[] { (Purchase)CloneEFCoreObject(x), (Purchase)CloneEFCoreObject(x) };
    })
    .ToList();
LP = LP.SelectMany(x =>
    {
        return new[] { (Purchase)CloneEFCoreObject(x), (Purchase)CloneEFCoreObject(x) };
    })
    .ToList();
SamplePurchasePanel.Render(LP);
var InvoiceHeaderForm = new GenEtoUI(
    SimpleJsonToUISerialization.ConvertToUISerialization(
        JsonSerializer.Serialize(
            SampleDataGenerator.SampleDataGenerator.GetSampleValidInvoice(0),
            JSOptions
        )
    ),
    (_) =>
    {
        return 0;
    },
    (_) =>
    {
        return 0;
    },
    ActionsMap,
    ""
);
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
        Eto.Forms.MessageBox.Show(
            JsonSerializer.Deserialize<Purchase>(JsonSerializer.Serialize(e)).Validate().Error,
            "Validation status"
        );
        LP.Add(JsonSerializer.Deserialize<Purchase>(JsonSerializer.Serialize(e)));
        SamplePurchasePanel.Render(LP);
        return 100;
    },
    (e) =>
    {
        Eto.Forms.MessageBox.Show(
            $"{JsonSerializer.Serialize(e)}, {JsonSerializer.Deserialize<Purchase>(JsonSerializer.Serialize(e)).Validate().Error}",
            "Serialized",
            MessageBoxType.Information
        );
        Eto.Forms.MessageBox.Show(
            JsonSerializer.Deserialize<Purchase>(JsonSerializer.Serialize(e)).Validate().Error,
            "Validation status"
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
        { ["VatAbsolute", "VatPercentage"], ("VatPanel", "NetTotalPrice") },
        { ["GrossMarkupAbsolute", "GrossMarkupPercentage"], ("MarkupPanel", "NetCostPerUnit") },
    },
    fieldOrder
);
PurchaseDataEntryForm.AnythingChanged = (string[] currentControlGroup) =>
{
    // Accumulate detailed log messages.
    var log = new System.Text.StringBuilder();
    log.AppendLine("=== Starting Global Update Recalculation ===");

    var P = PurchaseDataEntryForm;
    try
    {
        // Retrieve basic inputs.
        long packSize = (long)P.Lookup("PackSize");
        long packQuantity = (long)P.Lookup("PackQuantity");
        long freePacks = (long)P.Lookup("FreePacks");

        double receivedAsUnitQuantity = (double)P.Lookup("ReceivedAsUnitQuantity");
        double freeUnits = (double)P.Lookup("FreeUnits");

        double costPerPack = (double)P.Lookup("CostPerPack");
        double costPerUnit = (double)P.Lookup("CostPerUnit");
        double discountAbsolute = (double)P.Lookup("DiscountAbsolute");
        double vatAbsolute = (double)P.Lookup("VatAbsolute");
        bool isVatADisallowed = (bool)P.Lookup("IsVatADisallowedInputTax");

        log.AppendLine("Basic Inputs:");
        log.AppendLine(
            $"  PackSize={packSize}, PackQuantity={packQuantity}, FreePacks={freePacks}"
        );
        log.AppendLine($"  ReceivedAsUnitQuantity={receivedAsUnitQuantity}, FreeUnits={freeUnits}");
        log.AppendLine(
            $"  CostPerPack={costPerPack}, CostPerUnit={costPerUnit}, DiscountAbsolute={discountAbsolute}, VatAbsolute={vatAbsolute}"
        );
        log.AppendLine($"  IsVatADisallowed={isVatADisallowed}");

        // Compute quantities and totals.
        double totalUnits =
            ((packQuantity + freePacks) * packSize) + receivedAsUnitQuantity + freeUnits;
        double grossTotal = (packQuantity * costPerPack) + (receivedAsUnitQuantity * costPerUnit);
        double netTotalPrice = grossTotal - discountAbsolute;
        double totalAmountDue = netTotalPrice + vatAbsolute;
        double grossCostPerUnit = totalUnits > 0 ? grossTotal / totalUnits : 0.0;
        double netCostPerUnit =
            totalUnits > 0
                ? (isVatADisallowed ? totalAmountDue / totalUnits : netTotalPrice / totalUnits)
                : 0.0;
        log.AppendLine("Computed Totals:");
        log.AppendLine(
            $"  TotalUnits={totalUnits}, GrossTotal={grossTotal}, NetTotalPrice={netTotalPrice}, TotalAmountDue={totalAmountDue}"
        );
        log.AppendLine($"  GrossCostPerUnit={grossCostPerUnit}, NetCostPerUnit={netCostPerUnit}");

        // Determine incurred cost.
        double netTotalCost = isVatADisallowed ? totalAmountDue : netTotalPrice;
        log.AppendLine($"NetTotalCost (accounts for VAT if unreclaimable)={netTotalCost}");

        // Helper to update a field only if it is not in the current control group.
        Action<string, object> SetValueIfNotInCurrentControl = (key, value) =>
        {
            if (!currentControlGroup.Contains(key))
            {
                P.SetValue(key, value);
                log.AppendLine($"Updated {key} to {value}");
            }
            else
            {
                log.AppendLine($"Skipped updating {key} (active control).");
            }
        };

        // Update computed fields.
        SetValueIfNotInCurrentControl("TotalUnits", totalUnits);
        SetValueIfNotInCurrentControl("GrossTotal", grossTotal);
        SetValueIfNotInCurrentControl("NetTotalPrice", netTotalPrice);
        SetValueIfNotInCurrentControl("TotalAmountDue", totalAmountDue);
        SetValueIfNotInCurrentControl("GrossCostPerUnit", grossCostPerUnit);
        SetValueIfNotInCurrentControl("NetCostPerUnit", netCostPerUnit);
        SetValueIfNotInCurrentControl("NetTotalCost", netTotalCost);

        // Retrieve the SellingPrice TextBox and its focus state.
        Eto.Forms.TextBox sellingPriceTextBox = (Eto.Forms.TextBox)P._Einputs["SellingPrice"];
        bool isSellingPriceFocused = sellingPriceTextBox.HasFocus;
        log.AppendLine($"SellingPrice TextBox Focused: {isSellingPriceFocused}");

        // Compute a selling price from markup percentages.
        double currentMarkupPercentage = P.Lookup("GrossMarkupPercentage") is double d ? d : 0.0;
        double computedSellingPrice = netCostPerUnit * (1 + (currentMarkupPercentage / 100.0));
        log.AppendLine(
            $"Computed SellingPrice from markup: {computedSellingPrice} (Markup {currentMarkupPercentage}%)"
        );

        // Set a threshold for updating (e.g. if difference > 0.01).
        const double priceDiffThreshold = 0.01;

        // Branch based on focus state.
        if (isSellingPriceFocused)
        {
            // If focused, the user is updating the selling price. Use its current value to update the markup fields.
            log.AppendLine(
                "SellingPrice is focused. Updating markup based on user-entered selling price."
            );
            if (double.TryParse(sellingPriceTextBox.Text, out double updatedSellingPrice))
            {
                double markupAbsolute = updatedSellingPrice - netCostPerUnit;
                double markupPercentage =
                    netCostPerUnit != 0.0 ? (markupAbsolute / netCostPerUnit) * 100.0 : 0.0;
                if (Math.Abs(updatedSellingPrice - netCostPerUnit) < 0.001)
                {
                    log.AppendLine(
                        "User-entered SellingPrice is nearly equal to NetCostPerUnit; forcing zero margin."
                    );
                    markupAbsolute = 0.0;
                    markupPercentage = 0.0;
                }
                SetValueIfNotInCurrentControl("GrossMarkupAbsolute", markupAbsolute);
                SetValueIfNotInCurrentControl("GrossMarkupPercentage", markupPercentage);
                log.AppendLine(
                    $"Updated markup from focused SellingPrice: Absolute={markupAbsolute}, Percentage={markupPercentage}"
                );
            }
            else
            {
                log.AppendLine("Failed to parse SellingPrice while focused.");
            }
        }
        else
        {
            // If not focused, compare the computed selling price with what is currently displayed.
            if (double.TryParse(sellingPriceTextBox.Text, out double currentSellingPrice))
            {
                double diff = Math.Abs(computedSellingPrice - currentSellingPrice);
                if (diff > priceDiffThreshold)
                {
                    log.AppendLine(
                        $"Difference {diff} exceeds threshold {priceDiffThreshold}. Updating SellingPrice to computed value."
                    );
                    SetValueIfNotInCurrentControl("SellingPrice", computedSellingPrice);
                    sellingPriceTextBox.Text = computedSellingPrice.ToString("F2");
                }
                else
                {
                    log.AppendLine(
                        $"Difference {diff} is within threshold. Keeping current SellingPrice."
                    );
                }
                // Regardless, update the markup fields based on the computed selling price.
                double newGrossMarkupAbsolute = computedSellingPrice - netCostPerUnit;
                double newGrossMarkupPercentage =
                    (netCostPerUnit != 0.0)
                        ? (newGrossMarkupAbsolute / netCostPerUnit) * 100.0
                        : 0.0;
                if (Math.Abs(computedSellingPrice - netCostPerUnit) < 0.001)
                {
                    newGrossMarkupAbsolute = 0.0;
                    newGrossMarkupPercentage = 0.0;
                    log.AppendLine(
                        "Computed SellingPrice is nearly equal to NetCostPerUnit; forcing zero margin for markup."
                    );
                }
                SetValueIfNotInCurrentControl("GrossMarkupAbsolute", newGrossMarkupAbsolute);
                SetValueIfNotInCurrentControl("GrossMarkupPercentage", newGrossMarkupPercentage);
                log.AppendLine(
                    $"Updated markup based on computed SellingPrice: Absolute={newGrossMarkupAbsolute}, Percentage={newGrossMarkupPercentage}"
                );
            }
            else
            {
                log.AppendLine("Failed to parse current SellingPrice while not focused.");
            }
        }

        // Attach the SellingPrice TextChanged event handler (only once) for live updates.
        if (!(sellingPriceTextBox.Tag is bool handlerAttached && handlerAttached))
        {
            sellingPriceTextBox.TextChanged += (sender, e) =>
            {
                var innerLog = new System.Text.StringBuilder();
                innerLog.AppendLine("=== SellingPrice TextChanged event triggered ===");
                if (!currentControlGroup.Contains("SellingPrice"))
                {
                    if (double.TryParse(sellingPriceTextBox.Text, out double updatedSellingPrice))
                    {
                        innerLog.AppendLine($"User updated SellingPrice to {updatedSellingPrice}");
                        double markupAbsolute = updatedSellingPrice - netCostPerUnit;
                        double markupPercentage =
                            (netCostPerUnit != 0.0)
                                ? (markupAbsolute / netCostPerUnit) * 100.0
                                : 0.0;
                        if (Math.Abs(updatedSellingPrice - netCostPerUnit) < 0.001)
                        {
                            markupAbsolute = 0.0;
                            markupPercentage = 0.0;
                            innerLog.AppendLine(
                                "Updated SellingPrice nearly equals NetCostPerUnit; forcing zero margin."
                            );
                        }
                        SetValueIfNotInCurrentControl("GrossMarkupAbsolute", markupAbsolute);
                        SetValueIfNotInCurrentControl("GrossMarkupPercentage", markupPercentage);
                        innerLog.AppendLine(
                            $"Updated markup from TextChanged: Absolute={markupAbsolute}, Percentage={markupPercentage}"
                        );
                    }
                    else
                    {
                        innerLog.AppendLine(
                            "Failed to parse SellingPrice during TextChanged event."
                        );
                    }
                }
                Console.WriteLine(innerLog.ToString());
            };
            sellingPriceTextBox.Tag = true;
            log.AppendLine("Attached SellingPrice TextChanged event handler.");
        }

        log.AppendLine("=== Global update recalculation completed successfully. ===");
    }
    catch (Exception ex)
    {
        log.AppendLine("Error during recalculations: " + ex.Message + " " + ex.StackTrace);
    }

    // Output the entire accumulated log in one big Console.WriteLine.
    Console.WriteLine(log.ToString());
};
InvoiceHeaderForm.AnythingChanged = (string[] currentControlGroup) =>
{
    // Begin logging.
    var log = new System.Text.StringBuilder();
    log.AppendLine("=== Starting Global Invoice Header Recalculation ===");

    // Terse reference to the header form.
    var P = InvoiceHeaderForm;

    // --- Compute Aggregated Values from the Global Purchase List (LP) ---
    double computedGrossTotal = LP.Sum(p => p.GrossTotal);
    double computedTotalDiscountAbsolute = LP.Sum(p => p.DiscountAbsolute);
    double weightedDiscountPctFromItems =
        computedGrossTotal > 0 ? (computedTotalDiscountAbsolute / computedGrossTotal) * 100 : 0;
    double computedVatTotal = LP.Sum(p => p.VatAbsolute);
    double computedTotalAmountDue = LP.Sum(p => p.TotalAmountDue);

    // Retrieve whole-invoice discount values entered by the user.
    double userWholeInvoiceDiscountAbsolute = (double)P.Lookup("WholeInvoiceDiscountAbsolute");
    double userWholeInvoiceDiscountPercentage = (double)P.Lookup("WholeInvoiceDiscountPercentage");

    // Calculate effective discount totals.
    double effectiveDiscountAbsoluteTotal =
        userWholeInvoiceDiscountAbsolute + computedTotalDiscountAbsolute;
    double effectiveDiscountPercentageTotal =
        userWholeInvoiceDiscountPercentage + weightedDiscountPctFromItems;

    // Calculate VAT effective percentage.
    double taxableBase = computedGrossTotal - computedTotalDiscountAbsolute;
    double computedEffectiveVatPercentage =
        taxableBase > 0 ? (computedVatTotal / taxableBase) * 100 : 0;

    // Log each computed value.
    log.AppendLine($"Computed GrossTotal: {computedGrossTotal}");
    log.AppendLine($"Computed TotalDiscountAbsolute (items): {computedTotalDiscountAbsolute}");
    log.AppendLine($"Weighted Discount Percentage (items): {weightedDiscountPctFromItems}");
    log.AppendLine($"User WholeInvoiceDiscountAbsolute: {userWholeInvoiceDiscountAbsolute}");
    log.AppendLine($"User WholeInvoiceDiscountPercentage: {userWholeInvoiceDiscountPercentage}");
    log.AppendLine($"EffectiveDiscountAbsoluteTotal: {effectiveDiscountAbsoluteTotal}");
    log.AppendLine($"EffectiveDiscountPercentageTotal: {effectiveDiscountPercentageTotal}");
    log.AppendLine($"Computed VatTotal: {computedVatTotal}");
    log.AppendLine($"TaxableBase: {taxableBase}");
    log.AppendLine($"Computed EffectiveVatPercentage: {computedEffectiveVatPercentage}");
    log.AppendLine($"Computed TotalAmountDue: {computedTotalAmountDue}");

    // --- Helper for Computed Fields (fields the user should never alter manually) ---
    Action<string, object> UpdateComputedField = (key, value) =>
    {
        P.SetValue(key, value);
        log.AppendLine($"Updated computed field '{key}' to {value}");
        // Disable field to prevent manual editing.
        P.Disable(key);
        log.AppendLine($"Disabled field '{key}' from user input.");
    };

    // --- Update Computed Invoice Header Fields ---
    UpdateComputedField("GrossTotal", computedGrossTotal);
    UpdateComputedField("EffectiveDiscountAbsoluteFromEnteredItems", computedTotalDiscountAbsolute);
    UpdateComputedField(
        "EffectiveDiscountPercentageFromEnteredItems",
        weightedDiscountPctFromItems
    );
    UpdateComputedField("EffectiveDiscountAbsoluteTotal", effectiveDiscountAbsoluteTotal);
    UpdateComputedField("EffectiveDiscountPercentageTotal", effectiveDiscountPercentageTotal);
    UpdateComputedField("VatTotal", computedVatTotal);
    UpdateComputedField("EffectiveVatPercentage", computedEffectiveVatPercentage);
    UpdateComputedField("TotalAmountDue", computedTotalAmountDue);

    // --- Update LastSavedAt ---
    DateTime now = DateTime.Now;
    P.SetValue("LastSavedAt", now);
    P.Disable("LastSavedAt");
    log.AppendLine($"Updated and disabled 'LastSavedAt' to {now}");

    log.AppendLine("=== Completed Global Invoice Header Recalculation ===");

    // One final output.
    Console.WriteLine(log.ToString());
};
SamplePurchasePanel.AnythingHappened = InvoiceHeaderForm.AnythingChanged;

PurchasingUIButton.Click += (_, _) =>
{
    SamplePurchasePanel.DeleteReceivedInvoiceItem = (i) =>
    {
        System.Console.WriteLine($"Requested to remove: {i}");
        LP.RemoveAt(i);
        SamplePurchasePanel.Render(LP);
    };
    var F = new Eto.Forms.Form() { Content = new ReceivedInvoicePanel() };
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
    ["name", "price"]
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
GeneratedEtoUISample.AnythingChanged = (_) => { };
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
                RD,
                RC,
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
                ),
                new JsonEditorExample.FullJsonEditorPanel(
                    SampleJsonNested,
                    true, true
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
    new GenTopLevel(new Dictionary<string, (string, object, string)>(),//SimpleJsonToUISerialization.ConvertToUISerialization(SampleJsonNested),
        (_) =>
        {
            return 100;
        },
        (_) =>
        {
            return 100;
        },
        ActionsMap,
        
        SampleJsonNested,
        null
    )
);
