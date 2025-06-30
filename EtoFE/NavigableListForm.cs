using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonUi;
using Eto;
using Eto.Containers;
using Eto.Forms;
using EtoFE;
using EtoFE.Panels;
using MyExtensions;
using RV.InvNew.Common;

namespace EtoFE
{
    public class NavigableListForm : Form
    {
        public NavigableListForm()
        {
            Location = new Eto.Drawing.Point(200, 150);
            this.ApplyDarkThemeForScrollBarsAndGridView();
            this.ApplyDarkTheme();
            //DisableDefaults.ApplyGlobalScrollBarArrowStyle();
            //DisableDefaults.ApplyGlobalScrollBarThumbStyle();
            //DisableDefaults.ApplyGlobalScrollBarPageButtonStyle();
            this.MinimumSize = new Eto.Drawing.Size(1280, 720);
            Title = $"RV InvNew Inventory Manager";
            GridView LB = new GridView() { ShowHeader = false, GridLines = GridLines.None };
            LB.Size = new Eto.Drawing.Size(200, 600);
            LB.Columns.Add(
                new GridColumn()
                {
                    HeaderText = "Navigate to...",
                    DataCell = new TextBoxCell(0) { },
                }
            );
            LB.BackgroundColor = ColorSettings.BackgroundColor;
            //LB.TextColor = ColorSettings.ForegroundColor;
            //LB.Font = new Eto.Drawing.Font("Courier", 18);

            Panel CurrentPanel = new Panel()
            {
                //MinimumSize = new Eto.Drawing.Size(1100, 700),
                Size = new Eto.Drawing.Size(-1, -1),
            };

            var loadOncePanels = (
                new List<(string Label, object Content, string Name)>()
                {
                    (
                        $" 📦 Inventory ",
                        (
                            new LoadOncePanel<Panel>(
                                new NestableNavigableListPanel(
                                    new List<(string Label, object Content, string Name)>
                                    {
                                        // Core inventory panels
                                        (
                                            "📝 Editor",
                                            new LoadOncePanel<CatalogueEditPanel>(),
                                            "Editor"
                                        ),
                                        (
                                            "📋 Batch Editor",
                                            new LoadOncePanel<Panel>(),
                                            "BatchEditor"
                                        ),
                                        (
                                            "🔧 Adjustments",
                                            new LoadOncePanel<Panel>(),
                                            "Adjustments"
                                        ),
                                        ("📦 Items", new LoadOncePanel<Panel>(), "Items"),
                                        (
                                            "📊 Stock Overview",
                                            new LoadOncePanel<Panel>(),
                                            "StockOverview"
                                        ),
                                        ("📍 Locations", new LoadOncePanel<Panel>(), "Locations"),
                                        ("🔄 Transfers", new LoadOncePanel<Panel>(), "Transfers"),
                                        ("📈 Reports", new LoadOncePanel<Panel>(), "Reports"),
                                        ("⛑ Alerts", new LoadOncePanel<Panel>(), "Alerts"),
                                        ("🔍 Search", new LoadOncePanel<Panel>(), "Search"),
                                        // Additional standardized ERP modules
                                        (
                                            "🗃 Material Master",
                                            new LoadOncePanel<Panel>(),
                                            "MaterialMaster"
                                        ),
                                        (
                                            "📥 Goods Receipt",
                                            new LoadOncePanel<ReceivedInvoicePanel>(),
                                            "GoodsReceipt"
                                        ),
                                        (
                                            "📤 Goods Issue",
                                            new LoadOncePanel<Panel>(),
                                            "GoodsIssued"
                                        ),
                                        (
                                            "🧮 Cycle Count",
                                            new LoadOncePanel<Panel>(),
                                            "CycleCount"
                                        ),
                                        (
                                            "🏭 Warehouse Management",
                                            new LoadOncePanel<Panel>(),
                                            "Warehouse"
                                        ),
                                        (
                                            "🔢 Serial & Lot Control",
                                            new LoadOncePanel<Panel>(),
                                            "SerialControl"
                                        ),
                                        (
                                            "🔄 Replenishment",
                                            new LoadOncePanel<Panel>(),
                                            "Replenishment"
                                        ),
                                        // Barcode printing section
                                        (
                                            "🖨️ Barcode Print",
                                            new LoadOncePanel<Panel>(),
                                            "BarcodePrint"
                                        ),
                                    }
                                )
                            )
                        ),
                        "Inventory"
                    ),
                    (" 💰 Accounts  ", (new LoadOncePanel<Panel>(new NestableNavigableListPanel(
                                    new List<(string Label, object Content, string Name)>
                                    {
                                        (
                                            "📝 Account Types",
                                            new LoadOncePanel<AllAccountsTypes>(),
                                            "AccountTypes"
                                        ),
                                        (
                                            "📋 All Journal Entries",
                                            new LoadOncePanel<AllJournalEntries>(),
                                            "AllJournalEntries"
                                        ),
                                        (
                                            "📋 Make Journal Entries",
                                            new LoadOncePanel<JournalEntriesPanel>(),
                                            "JournalEntry"
                                        ),
                                    }))), "Accounts"),
                    (
                        $" 👥 HR / {Environment.NewLine} Employees  ",
                        (new LoadOncePanel<Panel>()),
                        "HR"
                    ),
                    (
                        $" 🤝 CRM {Environment.NewLine} (Customer Management)  ",
                        (new LoadOncePanel<Panel>()),
                        "CRM"
                    ),
                    (
                        $" ⚙️ Administration / {Environment.NewLine} Settings  ",
                        (new LoadOncePanel<Panel>()),
                        "Settings"
                    ),
                    (
                        $" ⚙️ Current {Environment.NewLine} Connection ",
                        (new LoadOncePanel<Panel>()),
                        "Connection"
                    ),
                    (" 🎫 About ", (new LoadOncePanel<Panel>()), "About"),
                }
            ).ToArray();
            int SelectedButtonIndex = -1;
            Dictionary<string, object> Panels = new Dictionary<string, object>();
            foreach (var panel in loadOncePanels)
            {
                Panels.Add(
                    TranslationHelper.Translate(panel.Item3, panel.Item1, Program.lang),
                    panel.Item2
                );
            }
            LB.DataStore = loadOncePanels.Select(x => new List<string>() { x.Item1 }).ToList();

            LB.GridLines = GridLines.None;

            IReadOnlyDictionary<string, object> ROD = Panels;
            LB.CellFormatting += (e, a) =>
            {
                if (a.Row == LB.SelectedRow)
                {
                    a.ForegroundColor = ColorSettings.BackgroundColor;
                    a.BackgroundColor = ColorSettings.ForegroundColor;
                }
                else
                {
                    a.ForegroundColor = ColorSettings.LesserForegroundColor;
                    a.BackgroundColor = ColorSettings.BackgroundColor;
                }
                a.Font = new Eto.Drawing.Font(
                    "Segoe UI",
                    10,
                    Eto.Drawing.FontStyle.Bold,
                    Eto.Drawing.FontDecoration.None
                );
            };
            List<RoundedLabel> Buttons = new();
            int i = 0;
            foreach ((string, object, string) LoadOncePanel in loadOncePanels)
            {
                RoundedLabel B = new RoundedLabel()
                {
                    Text = TranslationHelper.Translate(
                        LoadOncePanel.Item3,
                        LoadOncePanel.Item1,
                        Program.lang
                    ),
                    CornerRadius = 2,
                    HoverBorderColor = ColorSettings.LesserForegroundColor,
                    BorderColor = ColorSettings.LesserBackgroundColor,
                    Enabled = true,
                    CanFocus = true,
                    TabIndex = 2,
                };
                B.ConfigureForPlatform();

                //B.VerticalAlignment = VerticalAlignment.Center;
                B.Height = 60;
                B.Width = 150;

                B.Font = new Eto.Drawing.Font(Program.UIFont, 11) { };
                B.TextColor = ColorSettings.LesserForegroundColor;
                B.Enabled = true;
                B.BackgroundColor = ColorSettings.BackgroundColor;
                //B.Click += (e, a) => { };
                B.MouseEnter += (e, a) =>
                {
                    RoundedLabel CurrentLabel = ((RoundedLabel)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel))
                    {
                        B.TextColor = ColorSettings.SelectedColumnColor;
                        B.BackgroundColor = ColorSettings.ForegroundColor;
                    }
                };
                B.MouseLeave += (e, a) =>
                {
                    RoundedLabel CurrentLabel = ((RoundedLabel)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel))
                    {
                        B.TextColor = ColorSettings.ForegroundColor;
                        B.BackgroundColor = ColorSettings.BackgroundColor;
                    }
                    this.Invalidate();
                };
                B.TabIndex = i;
                B.MouseDown += (e, a) =>
                {
                    RoundedLabel ClickedLabel = ((RoundedLabel)e);
                    //MessageBox.Show($"Clicked {ClickedLabel.Text}", MessageBoxType.Information);

                    CurrentPanel.Content = (Control)
                        (
                            (ILoadOncePanel<object>)
                                ROD.GetValueOrDefault<string, object?>(
                                    (string)((string)ClickedLabel.Text),
                                    null
                                )
                        ).GetInnerAsObject();
                    SelectedButtonIndex = Buttons.IndexOf(ClickedLabel);
                    foreach (RoundedLabel L in Buttons)
                    {
                        L.TextColor = ColorSettings.ForegroundColor;
                        L.BackgroundColor = ColorSettings.BackgroundColor;
                        System.Console.WriteLine($"Adding button: {L.Text}");
                        //L.Invalidate(true);
                    }

                    ClickedLabel.TextColor = ColorSettings.BackgroundColor;
                    ClickedLabel.BackgroundColor = ColorSettings.ForegroundColor;
                    this.Size = new Eto.Drawing.Size(-1, -1);
                    this.Invalidate(true);
                    this.TriggerStyleChanged();
                    Title = $"\u300e{ClickedLabel.Text}\u300f RV InvNew Inventory Manager";
                };
                B.KeyUp += (e, a) =>
                {
                    RoundedLabel ClickedLabel = ((RoundedLabel)e);
                    //MessageBox.Show($"Clicked {ClickedLabel.Text}", MessageBoxType.Information);

                    CurrentPanel.Content = (Control)
                        (
                            (ILoadOncePanel<object>)
                                ROD.GetValueOrDefault<string, object?>(
                                    (string)((string)ClickedLabel.Text),
                                    null
                                )
                        ).GetInnerAsObject();
                    SelectedButtonIndex = Buttons.IndexOf(ClickedLabel);
                    foreach (RoundedLabel L in Buttons)
                    {
                        L.TextColor = ColorSettings.ForegroundColor;
                        L.BackgroundColor = ColorSettings.BackgroundColor;
                        System.Console.WriteLine($"Adding button: {L.Text}");
                        //L.Invalidate(true);
                    }

                    ClickedLabel.TextColor = ColorSettings.BackgroundColor;
                    ClickedLabel.BackgroundColor = ColorSettings.ForegroundColor;
                    this.Size = new Eto.Drawing.Size(-1, -1);
                    this.Invalidate(true);
                    this.TriggerStyleChanged();
                    Title = $"\u300e{ClickedLabel.Text}\u300f RV InvNew Inventory Manager";
                };

                Buttons.Add(B);
                i++;
            }
            LB.SelectedItemsChanged += (sender, e) =>
            {
                CurrentPanel.Content = (Control)
                    (
                        (ILoadOncePanel<object>)
                            ROD.GetValueOrDefault<string, object?>(
                                (string)((List<string>)LB.SelectedItem).First(),
                                null
                            )
                    ).GetInnerAsObject();
                Title = $"{(string)((List<string>)LB.SelectedItem).First()}";
                this.Size = new Eto.Drawing.Size(-1, -1);
                this.Invalidate();
                this.UpdateLayout();
            };
            LB.DisableLines();
            //LB.DisableGridViewEnterKey();
            BackgroundColor = ColorSettings.BackgroundColor;
            Padding = 10;
            Button EnableAccessibilityButton = new Button()
            {
                Text = TranslationHelper.Translate(" Enable Accessibility... ♿👓 "),
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
                MinimumSize = new Eto.Drawing.Size(30, 30),
                BackgroundColor = ColorSettings.BackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
                Width = Program.ControlWidth ?? 100,
                Height = Program.ControlHeight ?? 30,
            };
            Label CurrentClientTimeLabel = new Label()
            {
                Text = DateTime.UtcNow.ToString("O"),
                BackgroundColor = ColorSettings.BackgroundColor,
                TextColor = ColorSettings.LesserForegroundColor,
                VerticalAlignment = VerticalAlignment.Center,
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
                Width = Program.ControlWidth ?? 150,
                Height = Program.ControlHeight ?? 30,
            };
            Label CurrentServerTimeLabel = new Label()
            {
                Text = "TBD",
                BackgroundColor = ColorSettings.LesserBackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
                VerticalAlignment = VerticalAlignment.Center,
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
                Width = Program.ControlWidth ?? 150,
                Height = Program.ControlHeight ?? 30,
            };
            Label HueRotateLabel = new Label()
            {
                Text =
                    $"Rotate{Environment.NewLine}Colours{Environment.NewLine}by 15{Environment.NewLine}degrees",
                BackgroundColor = ColorSettings.LesserBackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
                VerticalAlignment = VerticalAlignment.Stretch,
                TextAlignment = TextAlignment.Center,

                Font = new Eto.Drawing.Font(Program.UIFont, 5),
                Width = Program.ControlWidth / 3 ?? 50,
                Height = Program.ControlHeight ?? 30,
            };
            Label IncreaseLightnessLabel = new Label()
            {
                Text =
                    $"Increase{Environment.NewLine}Lightness{Environment.NewLine}by 10{Environment.NewLine}percentage",
                BackgroundColor = ColorSettings.LesserBackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
                VerticalAlignment = VerticalAlignment.Stretch,
                TextAlignment = TextAlignment.Center,

                Font = new Eto.Drawing.Font(Program.UIFont, 5),
                Width = Program.ControlWidth / 3 ?? 50,
                Height = Program.ControlHeight ?? 30,
            };
            Label IncreaseContrastLabel = new Label()
            {
                Text =
                    $"Increase{Environment.NewLine}Contrast{Environment.NewLine}by 10{Environment.NewLine}percentage",
                BackgroundColor = ColorSettings.LesserBackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
                VerticalAlignment = VerticalAlignment.Stretch,
                TextAlignment = TextAlignment.Center,

                Font = new Eto.Drawing.Font(Program.UIFont, 5),
                Width = Program.ControlWidth / 3 ?? 50,
                Height = Program.ControlHeight ?? 30,
            };

            HueRotateLabel.MouseDown += (_, _) =>
            {
                ColorSettings.AlternatingColor1 = ColorSettings.AlternatingColor1.HueRotate(15);
                ColorSettings.AlternatingColor2 = ColorSettings.AlternatingColor2.HueRotate(15);
                ColorSettings.ForegroundColor = ColorSettings.ForegroundColor.HueRotate(15);
                ColorSettings.BackgroundColor = ColorSettings.BackgroundColor.HueRotate(15);
                ColorSettings.LesserForegroundColor = ColorSettings.LesserForegroundColor.HueRotate(
                    15
                );
                ColorSettings.LesserBackgroundColor = ColorSettings.LesserBackgroundColor.HueRotate(
                    15
                );
                ColorSettings.SelectedColumnColor = ColorSettings.SelectedColumnColor.HueRotate(15);
                this.UpdateLayout();
                (new NavigableListForm()).Show();
                this.Close();
                this.Invalidate(true);
            };

            IncreaseContrastLabel.MouseDown += (_, _) =>
            {
                ColorSettings.AlternatingColor1 = ColorSettings.AlternatingColor1.AdjustContrast(
                    10
                );
                ColorSettings.AlternatingColor2 = ColorSettings.AlternatingColor2.AdjustContrast(
                    10
                );
                ColorSettings.ForegroundColor = ColorSettings.ForegroundColor.AdjustContrast(10);
                ColorSettings.BackgroundColor = ColorSettings.BackgroundColor.AdjustContrast(10);
                ColorSettings.LesserForegroundColor =
                    ColorSettings.LesserForegroundColor.AdjustContrast(10);
                ColorSettings.LesserBackgroundColor =
                    ColorSettings.LesserBackgroundColor.AdjustContrast(10);
                ColorSettings.SelectedColumnColor =
                    ColorSettings.SelectedColumnColor.AdjustContrast(10);
                this.UpdateLayout();
                (new NavigableListForm()).Show();
                this.Close();
                this.Invalidate(true);
            };
            IncreaseLightnessLabel.MouseDown += (_, _) =>
            {
                ColorSettings.AlternatingColor1 = ColorSettings.AlternatingColor1.AdjustLightness(
                    10
                );
                ColorSettings.AlternatingColor2 = ColorSettings.AlternatingColor2.AdjustLightness(
                    10
                );
                ColorSettings.ForegroundColor = ColorSettings.ForegroundColor.AdjustLightness(10);
                ColorSettings.BackgroundColor = ColorSettings.BackgroundColor.AdjustLightness(10);
                ColorSettings.LesserForegroundColor =
                    ColorSettings.LesserForegroundColor.AdjustLightness(10);
                ColorSettings.LesserBackgroundColor =
                    ColorSettings.LesserBackgroundColor.AdjustLightness(10);
                ColorSettings.SelectedColumnColor =
                    ColorSettings.SelectedColumnColor.AdjustLightness(10);
                this.UpdateLayout();
                (new NavigableListForm()).Show();
                this.Close();
                this.Invalidate(true);
            };
            Label CurrentUserAndToken = new Label()
            {
                Text = $"{LoginTokens.Username}{Environment.NewLine}{LoginTokens.token.TokenID}",
                BackgroundColor = ColorSettings.LesserBackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
            };
            EnableAccessibilityButton.ConfigureForPlatform();
            EnableAccessibilityButton.Click += (sender, e) =>
            {
                (new ListPanelOptionsAsButtons(loadOncePanels)).Show();
            };
            var Inner = new StackLayout(
                new StackLayout(Buttons.Select(b => new StackLayoutItem(b)).ToArray())
                {
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Spacing = 4,
                    VerticalContentAlignment = VerticalAlignment.Stretch,
                    TabIndex = 2,
                },
                new Panel() { Width = 3, BackgroundColor = ColorSettings.ForegroundColor },
                new StackLayoutItem(CurrentPanel, true)
            )
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                BackgroundColor = ColorSettings.BackgroundColor,
                Size = new Eto.Drawing.Size(-1, -1),
            };
            var TopPanel = new StackLayout(
                EnableAccessibilityButton,
                CurrentClientTimeLabel,
                CurrentServerTimeLabel,
                CurrentUserAndToken,
                HueRotateLabel,
                IncreaseContrastLabel,
                IncreaseLightnessLabel
            )
            {
                Spacing = 4,
                BackgroundColor = ColorSettings.BackgroundColor,
                Orientation = Orientation.Horizontal,
                Padding = 4,
            };
            var A = new StackLayout(
                new StackLayoutItem(TopPanel),
                new StackLayoutItem(
                    new Panel() { BackgroundColor = ColorSettings.ForegroundColor, Height = 1 }
                ),
                new StackLayoutItem(Inner, true)
            )
            {
                VerticalContentAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };

            Content = A;
            Padding = 10;
            var LocalTimeRefresher = (
                new Thread(() =>
                {
                    while (true)
                    {
                        if (Application.Instance != null)
                            Application.Instance.Invoke(() =>
                            {
                                CurrentClientTimeLabel.Text =
                                    $"Client time: {Environment.NewLine}{DateTime.Now.ToString("s")}";
                            });
                        Thread.Sleep(1000);
                    }
                })
            );
            //SizeChanged += (e, a) => { this.UpdateLayout(); this.Invalidate(true); };
            var ServerTimeRefresher = (
                new Thread(() =>
                {
                    while (true)
                    {
                        try
                        {
                            SingleValueString TR;
                            var req = (
                                SendAuthenticatedRequest<string, SingleValueString>.Send(
                                    "Time",
                                    "/AutogeneratedClockEndpointBearerAuth",
                                    true
                                )
                            );
                            //req.ShowModal();
                            if (req.Error == false)
                            {
                                TR = req.Out;
                                if (Application.Instance != null)
                                    Application.Instance.Invoke(() =>
                                    {
                                        CurrentServerTimeLabel.Text =
                                            $"Server Time: {Environment.NewLine}{DateTime.Parse(TR.response, null, DateTimeStyles.RoundtripKind).ToLocalTime().ToString("s")}";
                                    });
                            }
                        }
                        catch (Exception E) { }
                        Thread.Sleep(1000);
                    }
                })
            );
            LocalTimeRefresher.Start();
            ServerTimeRefresher.Start();
            //Position
        }
    }
}
