using CommonUi;
using Eto.Forms;
using RV.InvNew.Common;
Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

//using Terminal.Gui;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
var CT = new CatalogueTransfer { Active = true, ActiveWeb = true, CreatedOn = DateTime.Now, DefaultVatCategory = 0, Description = "Hello", DescriptionPos = "Goodnight", DescriptionsOtherLanguages = 0, DescriptionWeb = "Web", EnforceAboveCost = true, ExpiryTrackingEnabled = false, Itemcode = 0, PermissionsCategory = 1, PriceManual=false, VatCategoryAdjustable = false, VatDependsOnUser = false };
new Application().Run(new Form() { Content = new GenEtoUI(CT.ToDict(), (_) => { MessageBox.Show("Clicked save", "Event received", MessageBoxType.Information); return 100; }, (_) => { MessageBox.Show("Clicked save", "Event received", MessageBoxType.Information); return 100; }, new Dictionary<string, ShowAndGetValue> { { "one", () => { MessageBox.Show("one", "InputHandler", MessageBoxType.Information); return 100; } }, { "VatChooser", () => { MessageBox.Show("one", "InputHandler", MessageBoxType.Information); return 101; } }, { "LangChooser", () => { MessageBox.Show("one", "InputHandler", MessageBoxType.Information); return 102; } } }) });