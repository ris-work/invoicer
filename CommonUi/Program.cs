using CommonUi;
using Eto.Forms;
using RV.InvNew.Common;
using Terminal.Gui;
Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

//using Terminal.Gui;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
var CT = new CatalogueTransfer { Active = true, ActiveWeb = true, CreatedOn = DateTime.Now, DefaultVatCategory = 0, Description = "Hello", DescriptionPos = "Goodnight", DescriptionsOtherLanguages = 0, DescriptionWeb = "Web", EnforceAboveCost = true, ExpiryTrackingEnabled = false, Itemcode = 0, PermissionsCategory = 1, PriceManual=false, VatCategoryAdjustable = false, VatDependsOnUser = false };
new Eto.Forms.Application().Run(new Form() { Content = new GenEtoUI(CT.ToDict(), (_) => { Eto.Forms.MessageBox.Show("Clicked save", "Event received", MessageBoxType.Information); return 100; }, (_) => { Eto.Forms.MessageBox.Show("Clicked save", "Event received", MessageBoxType.Information); return 100; }, new Dictionary<string, ShowAndGetValue> { { "one", () => { Eto.Forms.MessageBox.Show("one", "InputHandler", MessageBoxType.Information); return 100; } }, { "VatChooser", () => { Eto.Forms.MessageBox.Show("one", "InputHandler", MessageBoxType.Information); return 101; } }, { "LangChooser", () => { Eto.Forms.MessageBox.Show("one", "InputHandler", MessageBoxType.Information); return 102; } } }) });
Terminal.Gui.Application.Init();
Terminal.Gui.Application.Run(new GenTopLevel(CT.ToDict(), (_) => { Eto.Forms.MessageBox.Show("Clicked save", "Event received", MessageBoxType.Information); return 100; }, (_) => { Eto.Forms.MessageBox.Show("Clicked save", "Event received", MessageBoxType.Information); return 100; }, new Dictionary<string, ShowAndGetValue> { { "one", () => { Eto.Forms.MessageBox.Show("one", "InputHandler", MessageBoxType.Information); return 100; } }, { "VatChooser", () => { Eto.Forms.MessageBox.Show("one", "InputHandler", MessageBoxType.Information); return 101; } }, { "LangChooser", () => { Eto.Forms.MessageBox.Show("one", "InputHandler", MessageBoxType.Information); return 102; } } }));

