using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using Eto.Drawing;
using CommonUi;
using RV.InvNew.CommonUi;

namespace EtoFE
{
    public class PanelManagerPanel : Panel, ILoadOncePanel<PanelManagerPanel>
    {
        private Button refreshButton;
        private Button openButton;
        private SearchPanelEto searchPanel;
        private List<LaunchedPanelInfo> _panels = new List<LaunchedPanelInfo>();

        public PanelManagerPanel()
        {
            InitializeComponents();
            LoadData();
        }

        private void InitializeComponents()
        {
            // Create buttons
            refreshButton = new Button()
            {
                Text = TranslationHelper.Translate("Refresh"),
                Width = ColorSettings.ControlWidth ?? 100,
            };

            openButton = new Button()
            {
                Text = TranslationHelper.Translate("Open Selected"),
                Width = ColorSettings.ControlWidth ?? 100,
            };

            // Set up event handlers
            refreshButton.Click += RefreshButton_Click;
            openButton.Click += OpenButton_Click;

            // Create button panel
            var buttonPanel = new StackLayout()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Items =
                {
                    new StackLayoutItem(refreshButton),
                    new StackLayoutItem(openButton)
                }
            };

            // Create main layout
            Content = new StackLayout()
            {
                Padding = 10,
                Spacing = 5,
                Items =
                {
                    new StackLayoutItem(buttonPanel),
                    new StackLayoutItem(null, true) // Placeholder for searchPanel
                }
            };

            // Apply theme
            BackgroundColor = ColorSettings.BackgroundColor;
            buttonPanel.BackgroundColor = ColorSettings.BackgroundColor;

            // Style buttons
            StyleButton(refreshButton);
            StyleButton(openButton);
        }

        private void StyleButton(Button button)
        {
            button.BackgroundColor = ColorSettings.BackgroundColor;
            button.TextColor = ColorSettings.ForegroundColor;
            if (ColorSettings.UIFont != null)
                button.Font = new Font(ColorSettings.UIFont, 10);
        }

        private void LoadData()
        {
            try
            {
                _panels = PanelManager.GetAllPanels();
                UpdateSearchPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    TranslationHelper.Translate("Error loading panels: ") + ex.Message,
                    TranslationHelper.Translate("Error"),
                    MessageBoxType.Error
                );
            }
        }

        private void UpdateSearchPanel()
        {
            if (_panels == null || _panels.Count == 0)
            {
                // Create empty search panel
                searchPanel = new SearchPanelEto(
                    new List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>(),
                    new List<(string, TextAlignment, bool)>()
                );
            }
            else
            {
                // Convert panels to search format
                var searchCatalogue = _panels.Select(p => new[] {
                    p.Id,
                    p.Name,
                    p.TypeName,
                    p.LaunchedAt.ToString("yyyy-MM-dd HH:mm:ss")
                }).ToList();

                var coloredCatalogue = searchCatalogue.Select(row =>
                    (row, (Eto.Drawing.Color?)null, (Eto.Drawing.Color?)null)).ToList();

                var headers = new List<(string, TextAlignment, bool)>
                {
                    (TranslationHelper.Translate("ID"), TextAlignment.Left, false),
                    (TranslationHelper.Translate("Name"), TextAlignment.Left, false),
                    (TranslationHelper.Translate("Type"), TextAlignment.Left, false),
                    (TranslationHelper.Translate("Launched At"), TextAlignment.Left, false)
                };

                // Create search panel
                searchPanel = new SearchPanelEto(
                    coloredCatalogue,
                    headers,
                    Debug: false,
                    LocalColors: null,
                    GWW: 800,
                    GWH: 600
                );
            }

            // Update the layout to include the search panel
            var stackLayout = Content as StackLayout;
            if (stackLayout != null && stackLayout.Items.Count > 1)
            {
                stackLayout.Items[1] = new StackLayoutItem(searchPanel, true);
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            if (searchPanel != null && !searchPanel.Cancelled && searchPanel.Selected != null && searchPanel.Selected.Length > 0)
            {
                var panelId = searchPanel.Selected[0];
                var panel = PanelManager.GetPanel(panelId);
                if (panel?.PanelReference != null)
                {
                    var window = new Form()
                    {
                        Title = panel.Name,
                        Size = new Size(900, 700),
                        Content = panel.PanelReference
                    };
                    window.Show();
                }
                else
                {
                    MessageBox.Show(
                        TranslationHelper.Translate("Panel not found or has been closed."),
                        TranslationHelper.Translate("Warning"),
                        MessageBoxType.Warning
                    );
                }
            }
            else
            {
                MessageBox.Show(
                    TranslationHelper.Translate("Please select a panel to open."),
                    TranslationHelper.Translate("Warning"),
                    MessageBoxType.Warning
                );
            }
        }

        // ILoadOncePanel implementation
        public object GetInnerAsObject()
        {
            return this;
        }

        public PanelManagerPanel GetInner()
        {
            return this;
        }

        public void Destroy()
        {
            // Clean up resources if needed
            _panels.Clear();
        }
    }
}