// <copyright file="PanelManagerPanel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using CommonUi;
using Eto.Drawing;
using Eto.Forms;
// </copyright>

namespace EtoFE
{


    public class PanelManagerPanel : Panel, ILoadOncePanel<PanelManagerPanel>
    {
        private GridView gridView;
        private Button refreshButton;
        private Button openButton;
        private Button searchButton;

        public PanelManagerPanel()
        {
            this.InitializeComponents();
            this.LoadData();
        }

        private void InitializeComponents()
        {
            // Create buttons
            this.refreshButton = new Button()
            {
                Text = TranslationHelper.Translate("Refresh"),
                Width = ColorSettings.ControlWidth ?? 100,
            };

            this.openButton = new Button()
            {
                Text = TranslationHelper.Translate("Open Selected"),
                Width = ColorSettings.ControlWidth ?? 100,
            };

            this.searchButton = new Button()
            {
                Text = TranslationHelper.Translate("Search"),
                Width = ColorSettings.ControlWidth ?? 100,
            };

            // Create grid view
            this.gridView = new GridView() { ShowHeader = true };

            // Add columns
            this.gridView.Columns.Add(new GridColumn()
            {
                HeaderText = TranslationHelper.Translate("ID"),
                DataCell = new TextBoxCell { Binding = Binding.Property<LaunchedPanelInfo, string>(r => r.Id) },
                Width = 200,
            });

            this.gridView.Columns.Add(new GridColumn()
            {
                HeaderText = TranslationHelper.Translate("Name"),
                DataCell = new TextBoxCell { Binding = Binding.Property<LaunchedPanelInfo, string>(r => r.Name) },
                Width = 200,
            });

            this.gridView.Columns.Add(new GridColumn()
            {
                HeaderText = TranslationHelper.Translate("Type"),
                DataCell = new TextBoxCell { Binding = Binding.Property<LaunchedPanelInfo, string>(r => r.TypeName) },
                Width = 200,
            });

            this.gridView.Columns.Add(new GridColumn()
            {
                HeaderText = TranslationHelper.Translate("Launched At"),
                DataCell = new TextBoxCell { Binding = Binding.Property<LaunchedPanelInfo, string>(r => r.LaunchedAt.ToString("yyyy-MM-dd HH:mm:ss")) },
                Width = 150,
            });

            // Set up event handlers
            this.refreshButton.Click += this.RefreshButton_Click;
            this.openButton.Click += this.OpenButton_Click;
            this.searchButton.Click += this.SearchButton_Click;

            // Create button panel
            var buttonPanel = new StackLayout()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Items =
                {
                    new StackLayoutItem(this.refreshButton),
                    new StackLayoutItem(this.openButton),
                    new StackLayoutItem(this.searchButton)
                },
            };

            // Create main layout
            this.Content = new StackLayout()
            {
                Padding = 10,
                Spacing = 5,
                Items =
                {
                    new StackLayoutItem(buttonPanel),
                    new StackLayoutItem(this.gridView, true)
                },
            };

            // Apply theme
            this.BackgroundColor = ColorSettings.BackgroundColor;
            this.gridView.BackgroundColor = ColorSettings.BackgroundColor;
            buttonPanel.BackgroundColor = ColorSettings.BackgroundColor;

            // Style buttons
            this.StyleButton(this.refreshButton);
            this.StyleButton(this.openButton);
            this.StyleButton(this.searchButton);
        }

        private void StyleButton(Button button)
        {
            button.BackgroundColor = ColorSettings.BackgroundColor;
            button.TextColor = ColorSettings.ForegroundColor;
            if (ColorSettings.UIFont != null)
            {
                button.Font = new Font(ColorSettings.UIFont, 10);
            }
        }

        private void LoadData()
        {
            try
            {
                var panels = PanelManager.GetAllPanels();
                this.gridView.DataStore = panels;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    TranslationHelper.Translate("Error loading panels: ") + ex.Message,
                    TranslationHelper.Translate("Error"),
                    MessageBoxType.Error);
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            this.LoadData();
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            if (this.gridView.SelectedItem is LaunchedPanelInfo selectedPanel)
            {
                try
                {
                    var panel = PanelManager.GetPanel(selectedPanel.Id);
                    if (panel?.PanelReference != null)
                    {
                        var window = new Form()
                        {
                            Title = selectedPanel.Name,
                            Size = new Size(900, 700),
                            Content = panel.PanelReference,
                        };
                        window.Show();
                    }
                    else
                    {
                        MessageBox.Show(
                            TranslationHelper.Translate("Panel not found or has been closed."),
                            TranslationHelper.Translate("Warning"),
                            MessageBoxType.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        TranslationHelper.Translate("Error opening panel: ") + ex.Message,
                        TranslationHelper.Translate("Error"),
                        MessageBoxType.Error);
                }
            }
            else
            {
                MessageBox.Show(
                    TranslationHelper.Translate("Please select a panel to open."),
                    TranslationHelper.Translate("Warning"),
                    MessageBoxType.Warning);
            }
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            try
            {
                var panels = PanelManager.GetAllPanels();
                var searchItems = panels.Select(p => new { id = p.Id, name = p.Name, type = p.TypeName, launched = p.LaunchedAt.ToString("yyyy-MM-dd HH:mm:ss") }).ToList();

                var result = SearchPanelUtility.GenerateSearchDialog(
                    searchItems,
                    this,
                    order: new[] { "id" });

                if (result != null && result.Length > 0)
                {
                    var panelId = result[0];
                    var panel = PanelManager.GetPanel(panelId);
                    if (panel?.PanelReference != null)
                    {
                        var window = new Form()
                        {
                            Title = panel.Name,
                            Size = new Size(900, 700),
                            Content = panel.PanelReference,
                        };
                        window.Show();
                    }
                    else
                    {
                        MessageBox.Show(
                            TranslationHelper.Translate("Panel not found or has been closed."),
                            TranslationHelper.Translate("Warning"),
                            MessageBoxType.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    TranslationHelper.Translate("Error searching panels: ") + ex.Message,
                    TranslationHelper.Translate("Error"),
                    MessageBoxType.Error);
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
            this.gridView.DataStore = null;
        }
    }
}