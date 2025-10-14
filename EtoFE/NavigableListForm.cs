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
using RV.Invnew.EtoFE;
using RV.InvNew.UI;

namespace EtoFE
{
    public class NavigableListForm : Form
    {
        // Track currently selected button index for visual feedback
        private int SelectedButtonIndex = -1;

        // Store buttons list at class level for access in event handlers
        private List<RoundedLabel> Buttons = new List<RoundedLabel>();

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

            Panel CurrentPanel = new Panel()
            {
                Size = new Eto.Drawing.Size(-1, -1),
            };

            // Simplified panel configuration
            var loadOncePanels = CreatePanelConfiguration();

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

            int i = 0;
            foreach ((string, object, string) loadOncePanel in loadOncePanels)
            {
                RoundedLabel B = CreateNavigationButton(
                    loadOncePanel.Item1,
                    loadOncePanel.Item3,
                    i,
                    () => LoadPanelContent(loadOncePanel.Item1, CurrentPanel, ROD),
                    (index) =>
                    {
                        SelectedButtonIndex = index;
                        UpdateButtonStyles(Buttons, index);
                        Title = $"\u300e{loadOncePanel.Item1}\u300f RV InvNew Inventory Manager";
                    }
                );
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
            BackgroundColor = ColorSettings.BackgroundColor;
            Padding = 10;

            // Create UI elements
            Button EnableAccessibilityButton = CreateUIButton(" Enable Accessibility... ♿👓 ");
            Label CurrentClientTimeLabel = CreateStatusLabel("Client time: ", showUtcNow: false);
            Label CurrentServerTimeLabel = CreateStatusLabel("TBD", isServerLabel: true);

            // Create color adjustment labels
            var colorAdjustmentLabels = CreateColorAdjustmentLabels();

            Label CurrentUserAndToken = new Label()
            {
                Text = $"{LoginTokens.Username}{Environment.NewLine}{LoginTokens.token.TokenID}",
                BackgroundColor = ColorSettings.LesserBackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
            };

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
                colorAdjustmentLabels.HueRotate,
                colorAdjustmentLabels.Contrast,
                colorAdjustmentLabels.Lightness
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

            StartTimeRefreshers(CurrentClientTimeLabel, CurrentServerTimeLabel);
        }

        // Helper methods to reduce repetition

        private (string Label, object Content, string Name)[] CreatePanelConfiguration()
        {
            return new (string, object, string)[]
            {
                (
                    "🏷️ Sales & POS",
                    new LoadOncePanel<Panel>(
                        new NestableNavigableListPanel(
                            new List<(string Label, object Content, string Name)>
                            {
                                ("👤 Customers", new LoadOncePanel<Panel>(), "Customers"),
                                ("📋 Sales Orders", new LoadOncePanel<Panel>(), "SalesOrders"),
                                ("💳 Point of Sale", new LoadOncePanel<SalesPanel>(), "PosTerminal"),
                                ("🧾 Invoices", new LoadOncePanel<Panel>(), "Invoices"),
                                ("📦 Shipments", new LoadOncePanel<Panel>(), "Shipments"),
                                ("🔄 Returns", new LoadOncePanel<Panel>(), "Returns"),
                                ("💰 Payments", new LoadOncePanel<Panel>(), "Payments"),
                                ("📊 Sales Reports", new LoadOncePanel<Panel>(), "SalesReports"),
                                ("💲 Price Lists", new LoadOncePanel<Panel>(), "PriceLists"),
                                ("🎁 Promotions", new LoadOncePanel<Panel>(), "Promotions"),
                            }
                        )
                    ),
                    "Sales"
                ),
                (
                    $" 📦 Inventory ",
                    new LoadOncePanel<Panel>(
                        new NestableNavigableListPanel(
                            new List<(string Label, object Content, string Name)>
                            {
                                ("✏️ Item Editor", new LoadOncePanel<CatalogueEditPanel>(), "Editor"),
                                ("📝 Batch Editor", new LoadOncePanel<Panel>(), "BatchEditor"),
                                ("⚙️ Adjustments", new LoadOncePanel<InventoryAdjustmentPanel>(), "Adjustments"),
                                ("📦 All Items", new LoadOncePanel<Panel>(), "Items"),
                                ("📈 Stock Status", new LoadOncePanel<Panel>(), "StockOverview"),
                                ("📍 Locations", new LoadOncePanel<Panel>(), "Locations"),
                                ("🔄 Stock Transfers", new LoadOncePanel<Panel>(), "Transfers"),
                                ("📊 Inventory Reports", new LoadOncePanel<Panel>(), "Reports"),
                                ("📜 Movement History", new LoadOncePanel<InventoryMovementsPanel>(), "InventoryMovements"),
                                ("⚠️ Stock Alerts", new LoadOncePanel<Panel>(), "Alerts"),
                                ("🔍 Item Search", new LoadOncePanel<Panel>(), "Search"),
                                ("🗂️ Material Master", new LoadOncePanel<Panel>(), "MaterialMaster"),
                                ("📥 Goods Receipt", new LoadOncePanel<ReceivedInvoicePanel>(), "GoodsReceipt"),
                                ("📤 Goods Issue", new LoadOncePanel<Panel>(), "GoodsIssued"),
                                ("🔢 Cycle Count", new LoadOncePanel<CycleCountPanel>(), "CycleCount"),
                                ("🏭 Warehouse", new LoadOncePanel<Panel>(), "Warehouse"),
                                ("🏷️ Serial/Lot", new LoadOncePanel<Panel>(), "SerialControl"),
                                ("♻️ Replenishment", new LoadOncePanel<Panel>(), "Replenishment"),
                                ("🖨️ Barcode Print", new LoadOncePanel<Panel>(), "BarcodePrint"),
                            }
                        )
                    ),
                    "Inventory"
                ),
                (
                    " 💰 Accounting ",
                    new LoadOncePanel<Panel>(
                        new NestableNavigableListPanel(
                            new List<(string Label, object Content, string Name)>
                            {
                                ("🏦 Chart of Accounts", new LoadOncePanel<AllAccountsTypes>(), "AccountTypes"),
                                ("📖 Recent Journal", new LoadOncePanel<AllJournalEntries>(), "AllJournalEntries"),
                                ("📅 Journal by Date", new LoadOncePanel<AllJournalEntriesInTimePeriod>(), "AllJournalEntries"),
                                ("✏️ New Journal Entry", new LoadOncePanel<JournalEntriesPanel>(), "JournalEntry"),
                                ("⚖️ Account Balances", new LoadOncePanel<AllAccountsBalances>(), "AllAccountBalances"),
                                ("💸 Outgoing Payments", new LoadOncePanel<ScheduledPaymentPanel>(), "ScheduledPayments"),
                                ("💵 Incoming Receipts", new LoadOncePanel<ScheduledReceiptPanel>(), "ScheduledPayments"),
                                ("🔍 Audit Trail", new LoadOncePanel<RequestsSearchPanel>(), "Audits"),
                            }
                        )
                    ),
                    "Accounts"
                ),
                (
                    $" 👥 Human Resources ",
                    (new LoadOncePanel<Panel>()),
                    "HR"
                ),
                (
                    $" 🤝 Customer Relations ",
                    (new LoadOncePanel<Panel>()),
                    "CRM"
                ),
                (
                    $" ⚙️ System Settings ",
                    (new LoadOncePanel<Panel>()),
                    "Settings"
                ),
                (
                    $" 🔗 Connection Status ",
                    (new LoadOncePanel<Panel>()),
                    "Connection"
                ),
                (" ℹ️ About ", (new LoadOncePanel<Panel>()), "About"),
            };
        }

        private RoundedLabel CreateNavigationButton(
            string label,
            string translationKey,
            int tabIndex,
            Action loadAction,
            Action<int> selectAction)
        {
            RoundedLabel button = new RoundedLabel()
            {
                Text = TranslationHelper.Translate(translationKey, label, Program.lang),
                CornerRadius = 2,
                HoverBorderColor = ColorSettings.LesserForegroundColor,
                BorderColor = ColorSettings.LesserBackgroundColor,
                BorderWidth = 1,
                Enabled = true,
                CanFocus = true,
                TabIndex = tabIndex,
                Height = 60,
                Width = 150,
                Font = new Eto.Drawing.Font(Program.UIFont, 11),
                TextColor = ColorSettings.LesserForegroundColor,
                BackgroundColor = ColorSettings.BackgroundColor,
            };
            button.ConfigureForPlatform();

            button.MouseEnter += (e, a) =>
            {
                RoundedLabel currentLabel = ((RoundedLabel)e);
                if (SelectedButtonIndex != Buttons.IndexOf(currentLabel))
                {
                    currentLabel.TextColor = ColorSettings.SelectedColumnColor;
                    currentLabel.BackgroundColor = ColorSettings.ForegroundColor;
                }
            };

            button.MouseLeave += (e, a) =>
            {
                RoundedLabel currentLabel = ((RoundedLabel)e);
                if (SelectedButtonIndex != Buttons.IndexOf(currentLabel))
                {
                    currentLabel.TextColor = ColorSettings.ForegroundColor;
                    currentLabel.BackgroundColor = ColorSettings.BackgroundColor;
                }
                this.Invalidate();
            };

            button.MouseDown += (e, a) =>
            {
                RoundedLabel clickedLabel = ((RoundedLabel)e);
                loadAction();
                selectAction(Buttons.IndexOf(clickedLabel));
                this.Size = new Eto.Drawing.Size(-1, -1);
                this.Invalidate(true);
                this.TriggerStyleChanged();
            };

            button.KeyUp += (e, a) =>
            {
                RoundedLabel clickedLabel = ((RoundedLabel)e);
                loadAction();
                selectAction(Buttons.IndexOf(clickedLabel));
                this.Size = new Eto.Drawing.Size(-1, -1);
                this.Invalidate(true);
                this.TriggerStyleChanged();
            };

            return button;
        }

        private void LoadPanelContent(string panelKey, Panel currentPanel, IReadOnlyDictionary<string, object> panels)
        {
            currentPanel.Content = (Control)
                (
                    (ILoadOncePanel<object>)
                        panels.GetValueOrDefault<string, object?>(
                            panelKey,
                            null
                        )
                ).GetInnerAsObject();
        }

        private void UpdateButtonStyles(List<RoundedLabel> buttons, int selectedIndex)
        {
            foreach (RoundedLabel L in buttons)
            {
                // Reset all buttons to normal state
                L.TextColor = ColorSettings.ForegroundColor;
                L.BackgroundColor = ColorSettings.BackgroundColor;
                L.BorderWidth = 1;
                L.BorderColor = ColorSettings.LesserBackgroundColor;
                // Remove the selection indicator from non-selected buttons
                string text = L.Text;
                if (text.StartsWith("▶ "))
                {
                    L.Text = text.Substring(2);
                }
            }

            // Highlight selected button
            if (selectedIndex >= 0 && selectedIndex < buttons.Count)
            {
                var selected = buttons[selectedIndex];
                selected.TextColor = ColorSettings.BackgroundColor;
                selected.BackgroundColor = ColorSettings.ForegroundColor;
                selected.BorderWidth = 2;
                selected.BorderColor = ColorSettings.SelectedColumnColor;
                // Add selection indicator with arrow
                if (!selected.Text.StartsWith("▶ "))
                {
                    selected.Text = "▶ " + selected.Text;
                }
            }
        }

        private (Label HueRotate, Label Contrast, Label Lightness) CreateColorAdjustmentLabels()
        {
            Label CreateColorLabel(string text)
            {
                return new Label()
                {
                    Text = text,
                    BackgroundColor = ColorSettings.LesserBackgroundColor,
                    TextColor = ColorSettings.ForegroundColor,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    TextAlignment = TextAlignment.Center,
                    Font = new Eto.Drawing.Font(Program.UIFont, 5),
                    Width = Program.ControlWidth / 3 ?? 50,
                    Height = Program.ControlHeight ?? 30,
                };
            }

            var hueRotateLabel = CreateColorLabel(
                $"🌈 Rotate{Environment.NewLine}Colors{Environment.NewLine}+15°"
            );
            var contrastLabel = CreateColorLabel(
                $"◐ Contrast{Environment.NewLine}+10%"
            );
            var lightnessLabel = CreateColorLabel(
                $"☀️ Brightness{Environment.NewLine}+10%"
            );

            hueRotateLabel.MouseDown += (_, _) => AdjustColors(ColorAdjustType.HueRotate);
            contrastLabel.MouseDown += (_, _) => AdjustColors(ColorAdjustType.Contrast);
            lightnessLabel.MouseDown += (_, _) => AdjustColors(ColorAdjustType.Lightness);

            return (hueRotateLabel, contrastLabel, lightnessLabel);
        }

        private enum ColorAdjustType { HueRotate, Contrast, Lightness }

        private void AdjustColors(ColorAdjustType adjustType)
        {
            // Apply adjustments using a different approach since methods might be in-place
            switch (adjustType)
            {
                case ColorAdjustType.HueRotate:
                    ColorSettings.AlternatingColor1.HueRotate(15);
                    ColorSettings.AlternatingColor2.HueRotate(15);
                    ColorSettings.ForegroundColor.HueRotate(15);
                    ColorSettings.BackgroundColor.HueRotate(15);
                    ColorSettings.LesserForegroundColor.HueRotate(15);
                    ColorSettings.LesserBackgroundColor.HueRotate(15);
                    ColorSettings.SelectedColumnColor.HueRotate(15);
                    break;

                case ColorAdjustType.Contrast:
                    ColorSettings.AlternatingColor1.AdjustContrast(10);
                    ColorSettings.AlternatingColor2.AdjustContrast(10);
                    ColorSettings.ForegroundColor.AdjustContrast(10);
                    ColorSettings.BackgroundColor.AdjustContrast(10);
                    ColorSettings.LesserForegroundColor.AdjustContrast(10);
                    ColorSettings.LesserBackgroundColor.AdjustContrast(10);
                    ColorSettings.SelectedColumnColor.AdjustContrast(10);
                    break;

                case ColorAdjustType.Lightness:
                    ColorSettings.AlternatingColor1.AdjustLightness(10);
                    ColorSettings.AlternatingColor2.AdjustLightness(10);
                    ColorSettings.ForegroundColor.AdjustLightness(10);
                    ColorSettings.BackgroundColor.AdjustLightness(10);
                    ColorSettings.LesserForegroundColor.AdjustLightness(10);
                    ColorSettings.LesserBackgroundColor.AdjustLightness(10);
                    ColorSettings.SelectedColumnColor.AdjustLightness(10);
                    break;
            }

            this.UpdateLayout();
            (new NavigableListForm()).Show();
            this.Close();
            this.Invalidate(true);
        }

        private Button CreateUIButton(string text)
        {
            return new Button()
            {
                Text = TranslationHelper.Translate(text, Program.lang),
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
                MinimumSize = new Eto.Drawing.Size(30, 30),
                BackgroundColor = ColorSettings.BackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
                Width = Program.ControlWidth ?? 100,
                Height = Program.ControlHeight ?? 30,
            };
        }

        private Label CreateStatusLabel(string prefix, bool showUtcNow = false, bool isServerLabel = false)
        {
            var label = new Label()
            {
                Text = showUtcNow ? $"{prefix}{Environment.NewLine}{DateTime.UtcNow.ToString("O")}" : prefix,
                BackgroundColor = isServerLabel ? ColorSettings.LesserBackgroundColor : ColorSettings.BackgroundColor,
                TextColor = ColorSettings.LesserForegroundColor,
                VerticalAlignment = VerticalAlignment.Center,
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
                Width = Program.ControlWidth ?? 150,
                Height = Program.ControlHeight ?? 30,
            };
            label.ConfigureForPlatform();
            return label;
        }

        private void StartTimeRefreshers(Label clientTimeLabel, Label serverTimeLabel)
        {
            var LocalTimeRefresher = new Thread(() =>
            {
                while (true)
                {
                    if (Application.Instance != null)
                        Application.Instance.Invoke(() =>
                        {
                            clientTimeLabel.Text = $"⏰ Client: {Environment.NewLine}{DateTime.Now:HH:mm:ss}";
                        });
                    Thread.Sleep(1000);
                }
            });
            LocalTimeRefresher.Start();

            var ServerTimeRefresher = new Thread(() =>
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
                        if (req.Error == false)
                        {
                            TR = req.Out;
                            if (Application.Instance != null)
                                Application.Instance.Invoke(() =>
                                {
                                    serverTimeLabel.Text =
                                        $"🌐 Server: {Environment.NewLine}{DateTime.Parse(TR.response, null, DateTimeStyles.RoundtripKind).ToLocalTime():HH:mm:ss}";
                                });
                        }
                    }
                    catch (Exception E) { }
                    Thread.Sleep(1000);
                }
            });
            ServerTimeRefresher.Start();
        }
    }
}