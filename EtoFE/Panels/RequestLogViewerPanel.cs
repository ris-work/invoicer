using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Eto.Forms;
using Eto.Drawing;
using CommonUi;
using RV.InvNew.Common;

namespace EtoFE
{
    public class RequestLogViewerPanel : Panel, ILoadOncePanel<RequestLogViewerPanel>
    {
        private GridView gridView;
        private Button replayButton;
        private Button refreshButton;
        private Button clearButton;

        public RequestLogViewerPanel()
        {
            InitializeComponents();
            LoadData();
        }

        private void InitializeComponents()
        {
            // Create buttons
            replayButton = new Button()
            {
                Text = TranslationHelper.Translate("Replay Selected"),
                Width = ColorSettings.ControlWidth ?? 100,
            };

            refreshButton = new Button()
            {
                Text = TranslationHelper.Translate("Refresh"),
                Width = ColorSettings.ControlWidth ?? 100,
            };

            clearButton = new Button()
            {
                Text = TranslationHelper.Translate("Clear Logs"),
                Width = ColorSettings.ControlWidth ?? 100,
            };

            // Create grid view
            gridView = new GridView() { ShowHeader = true };

            // Add columns
            gridView.Columns.Add(new GridColumn()
            {
                HeaderText = TranslationHelper.Translate("Timestamp"),
                DataCell = new TextBoxCell { Binding = Binding.Property<RequestLogEntry, string>(r => r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")) },
                Width = 150
            });

            gridView.Columns.Add(new GridColumn()
            {
                HeaderText = TranslationHelper.Translate("Endpoint"),
                DataCell = new TextBoxCell { Binding = Binding.Property<RequestLogEntry, string>(r => r.Endpoint) },
                Width = 200
            });

            gridView.Columns.Add(new GridColumn()
            {
                HeaderText = TranslationHelper.Translate("Request"),
                DataCell = new TextBoxCell { Binding = Binding.Property<RequestLogEntry, string>(r => TruncateString(r.SerializedRequest, 50)) },
                Width = 250
            });

            gridView.Columns.Add(new GridColumn()
            {
                HeaderText = TranslationHelper.Translate("Response"),
                DataCell = new TextBoxCell { Binding = Binding.Property<RequestLogEntry, string>(r => TruncateString(r.SerializedResponse, 50)) },
                Width = 250
            });

            gridView.Columns.Add(new GridColumn()
            {
                HeaderText = TranslationHelper.Translate("Success"),
                DataCell = new TextBoxCell { Binding = Binding.Property<RequestLogEntry, string>(r => r.Success ? "Yes" : "No") },
                Width = 80
            });

            // Set up event handlers
            replayButton.Click += ReplayButton_Click;
            refreshButton.Click += RefreshButton_Click;
            clearButton.Click += ClearButton_Click;

            // Create button panel
            var buttonPanel = new StackLayout()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Items =
                {
                    new StackLayoutItem(replayButton),
                    new StackLayoutItem(refreshButton),
                    new StackLayoutItem(clearButton)
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
                    new StackLayoutItem(gridView, true)
                }
            };

            // Apply theme
            BackgroundColor = ColorSettings.BackgroundColor;
            gridView.BackgroundColor = ColorSettings.BackgroundColor;
            buttonPanel.BackgroundColor = ColorSettings.BackgroundColor;

            // Style buttons
            StyleButton(replayButton);
            StyleButton(refreshButton);
            StyleButton(clearButton);
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
                // Get request logs in reverse chronological order
                var logs = RequestLogger.GetRequestLogs().OrderByDescending(l => l.Timestamp).ToList();
                gridView.DataStore = logs;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    TranslationHelper.Translate("Error loading request logs: ") + ex.Message,
                    TranslationHelper.Translate("Error"),
                    MessageBoxType.Error
                );
            }
        }

        private void ReplayButton_Click(object sender, EventArgs e)
        {
            if (gridView.SelectedItem is RequestLogEntry selectedEntry)
            {
                try
                {
                    // Replay the selected request using the ReplayRequest method
                    var result = SendAuthenticatedRequest<object, object>.ReplayRequest(selectedEntry);

                    if (result.Error)
                    {
                        MessageBox.Show(
                            TranslationHelper.Translate("Failed to replay request."),
                            TranslationHelper.Translate("Error"),
                            MessageBoxType.Error
                        );
                    }
                    else
                    {
                        MessageBox.Show(
                            TranslationHelper.Translate("Request replayed successfully"),
                            TranslationHelper.Translate("Success"),
                            MessageBoxType.Information
                        );

                        // Refresh the logs to include the replayed request
                        LoadData();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        TranslationHelper.Translate("Error replaying request: ") + ex.Message,
                        TranslationHelper.Translate("Error"),
                        MessageBoxType.Error
                    );
                }
            }
            else
            {
                MessageBox.Show(
                    TranslationHelper.Translate("Please select a request to replay."),
                    TranslationHelper.Translate("Warning"),
                    MessageBoxType.Warning
                );
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                TranslationHelper.Translate("Are you sure you want to clear all request logs?"),
                TranslationHelper.Translate("Confirm"),
                MessageBoxButtons.YesNo,
                MessageBoxType.Question
                ) == DialogResult.Yes)
            {
                try
                {
                    RequestLogger.ClearLogs();
                    gridView.DataStore = new List<RequestLogEntry>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        TranslationHelper.Translate("Error clearing request logs: ") + ex.Message,
                        TranslationHelper.Translate("Error"),
                        MessageBoxType.Error
                    );
                }
            }
        }

        private string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength) + "...";
        }

        // ILoadOncePanel implementation
        public object GetInnerAsObject()
        {
            return this;
        }

        public RequestLogViewerPanel GetInner()
        {
            return this;
        }

        public void Destroy()
        {
            // Clean up resources if needed
            gridView.DataStore = null;
        }
    }
}