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
using RV.InvNew.Common;

namespace EtoFE
{
    public class NavigableListForm : Form
    {
        public NavigableListForm()
        {
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

            Panel CurrentPanel = new Panel() { MinimumSize = new Eto.Drawing.Size(1100, 700) };

            var loadOncePanels = (
                new List<(string, object)>()
                {
                    (
                        $" 📦 Inventory ",
                        (
                            new LoadOncePanel<Panel>(
                                new NestableNavigableListPanel(
                                    new List<(string Label, object Content)>
                                    {
                                        // Core inventory panels
                                        ("📝 Editor", new LoadOncePanel<CatalogueEditPanel>()),
                                        ("📋 Batch Editor", new LoadOncePanel<Panel>()),
                                        ("🔧 Adjustments", new LoadOncePanel<Panel>()),
                                        ("📦 Items", new LoadOncePanel<Panel>()),
                                        ("📊 Stock Overview", new LoadOncePanel<Panel>()),
                                        ("📍 Locations", new LoadOncePanel<Panel>()),
                                        ("🔄 Transfers", new LoadOncePanel<Panel>()),
                                        ("📈 Reports", new LoadOncePanel<Panel>()),
                                        ("⛑ Alerts", new LoadOncePanel<Panel>()),
                                        ("🔍 Search", new LoadOncePanel<Panel>()),
                                        // Additional standardized ERP modules
                                        ("🗃 Material Master", new LoadOncePanel<Panel>()),
                                        ("📥 Goods Receipt", new LoadOncePanel<Panel>()),
                                        ("📤 Goods Issue", new LoadOncePanel<Panel>()),
                                        ("🧮 Cycle Count", new LoadOncePanel<Panel>()),
                                        ("🏭 Warehouse Management", new LoadOncePanel<Panel>()),
                                        ("🔢 Serial & Lot Control", new LoadOncePanel<Panel>()),
                                        ("🔄 Replenishment", new LoadOncePanel<Panel>()),
                                        // Barcode printing section
                                        ("🖨️ Barcode Print", new LoadOncePanel<Panel>()),
                                    }
                                )
                            )
                        )
                    ),
                    (" 💰 Accounts  ", (new LoadOncePanel<Panel>())),
                    ($" 👥 HR / {Environment.NewLine} Employees  ", (new LoadOncePanel<Panel>())),
                    (
                        $" 🤝 CRM {Environment.NewLine} (Customer Management)  ",
                        (new LoadOncePanel<Panel>())
                    ),
                    (
                        $" ⚙️ Administration / {Environment.NewLine} Settings  ",
                        (new LoadOncePanel<Panel>())
                    ),
                    (
                        $" ⚙️ Current {Environment.NewLine} Connection ",
                        (new LoadOncePanel<Panel>())
                    ),
                    (" 🎫 About ", (new LoadOncePanel<Panel>())),
                }
            ).ToArray();
            int SelectedButtonIndex = -1;
            Dictionary<string, object> Panels = new Dictionary<string, object>();
            foreach (var panel in loadOncePanels)
            {
                Panels.Add(panel.Item1, panel.Item2);
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
            List<Label> Buttons = new();
            int i = 0;
            foreach ((string, object) LoadOncePanel in loadOncePanels)
            {
                Label B = new Label() { Text = LoadOncePanel.Item1 };

                B.VerticalAlignment = VerticalAlignment.Center;
                B.Height = 60;

                B.Font = new Eto.Drawing.Font(Program.UIFont, 11) { };
                B.TextColor = ColorSettings.ForegroundColor;
                B.Enabled = true;
                B.BackgroundColor = ColorSettings.BackgroundColor;
                //B.Click += (e, a) => { };
                B.MouseEnter += (e, a) =>
                {
                    Label CurrentLabel = ((Label)e);
                    if (SelectedButtonIndex != Buttons.IndexOf(CurrentLabel))
                    {
                        B.TextColor = ColorSettings.SelectedColumnColor;
                        B.BackgroundColor = ColorSettings.ForegroundColor;
                    }
                };
                B.MouseLeave += (e, a) =>
                {
                    Label CurrentLabel = ((Label)e);
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
                    Label ClickedLabel = ((Label)e);
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
                    foreach (Label L in Buttons)
                    {
                        L.TextColor = ColorSettings.ForegroundColor;
                        L.BackgroundColor = ColorSettings.BackgroundColor;
                        L.Invalidate(true);
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
                Text = " Enable Accessibility... ♿👓 ",
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
                MinimumSize = new Eto.Drawing.Size(30, 30),
                BackgroundColor = ColorSettings.BackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
            };
            Label CurrentClientTimeLabel = new Label()
            {
                Text = DateTime.UtcNow.ToString("O"),
                BackgroundColor = Eto.Drawing.Color.FromGrayscale(0.35f),
                TextColor = ColorSettings.ForegroundColor,
                VerticalAlignment = VerticalAlignment.Center,
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
            };
            Label CurrentServerTimeLabel = new Label()
            {
                Text = "TBD",
                BackgroundColor = ColorSettings.LesserBackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
                VerticalAlignment = VerticalAlignment.Center,
                Font = new Eto.Drawing.Font(Program.UIFont, 10),
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
                },
                new Panel() { Width = 3, BackgroundColor = ColorSettings.ForegroundColor },
                new StackLayoutItem(CurrentPanel)
            )
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                BackgroundColor = ColorSettings.BackgroundColor,
            };
            var TopPanel = new StackLayout(
                EnableAccessibilityButton,
                CurrentClientTimeLabel,
                CurrentServerTimeLabel,
                CurrentUserAndToken
            )
            {
                Spacing = 4,
                BackgroundColor = ColorSettings.BackgroundColor,
                Orientation = Orientation.Horizontal,
                Padding = 4,
            };
            Content = new StackLayout(TopPanel, Inner);
            Padding = 10;
            var LocalTimeRefresher = (
                new Thread(() =>
                {
                    while (true)
                    {
                        Application.Instance.Invoke(() =>
                        {
                            CurrentClientTimeLabel.Text =
                                $"Client time: {Environment.NewLine}{DateTime.Now.ToString("s")}";
                        });
                        Thread.Sleep(1000);
                    }
                })
            );
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
