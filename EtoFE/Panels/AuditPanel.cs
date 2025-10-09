using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Eto.Forms;
using Eto.Drawing;
using CommonUi;
using RV.InvNew.Common;

namespace EtoFE.Panels
{
    public class RequestsSearchPanel : Panel
    {
        // Controls
        private TextBox txtToken;
        private TextBox txtType;
        private TextBox txtRequestedAction;
        private TextBox txtRequestedPrivilegeLevel;
        private TextBox txtEndpoint;
        private TextBox txtPrincipal;
        private CheckBox chkIncludeBadRequests;
        private DateTimePicker dpFrom;
        private DateTimePicker dpTo;
        private TextBox txtLimit;
        private Button btnSearch;
        private Button btnReset;
        private GridView gvResults;

        // Data
        private List<object> searchResults = new List<object>();

        public RequestsSearchPanel()
        {
            InitializeComponents();
            SetupLayout();
            SetupEventHandlers();

            // Set default values
            dpTo.Value = DateTime.Now;
            dpFrom.Value = DateTime.Now.AddDays(-180);
            txtLimit.Text = "100";
        }

        private void InitializeComponents()
        {
            // Initialize controls
            txtToken = new TextBox();
            txtType = new TextBox();
            txtRequestedAction = new TextBox();
            txtRequestedPrivilegeLevel = new TextBox();
            txtEndpoint = new TextBox();
            txtPrincipal = new TextBox();
            chkIncludeBadRequests = new CheckBox { Text = TranslationHelper.Translate("Include Bad Requests") };
            dpFrom = new DateTimePicker();
            dpTo = new DateTimePicker();
            txtLimit = new TextBox();
            btnSearch = new Button { Text = TranslationHelper.Translate("Search") };
            btnReset = new Button { Text = TranslationHelper.Translate("Reset") };
            gvResults = new GridView();

            // Set up grid view columns
            gvResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = new PropertyBinding<object>("TimeTai") },
                HeaderText = TranslationHelper.Translate("Time")
            });
            gvResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = new PropertyBinding<object>("Principal") },
                HeaderText = TranslationHelper.Translate("Principal")
            });
            gvResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = new PropertyBinding<object>("Token") },
                HeaderText = TranslationHelper.Translate("Token")
            });
            gvResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = new PropertyBinding<object>("Type") },
                HeaderText = TranslationHelper.Translate("Type")
            });
            gvResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = new PropertyBinding<object>("RequestedAction") },
                HeaderText = TranslationHelper.Translate("Requested Action")
            });
            gvResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = new PropertyBinding<object>("RequestedPrivilegeLevel") },
                HeaderText = TranslationHelper.Translate("Requested Privilege Level")
            });
            gvResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = new PropertyBinding<object>("Endpoint") },
                HeaderText = TranslationHelper.Translate("Endpoint")
            });
        }

        private void SetupLayout()
        {
            var columnCount = 2; // Default value

            // Create the search criteria panel
            var criteriaPanel = new TableLayout();

            // Add controls to the criteria panel
            var controls = new List<Control> {
                new Label { Text = TranslationHelper.Translate("Token") }, txtToken,
                new Label { Text = TranslationHelper.Translate("Type") }, txtType,
                new Label { Text = TranslationHelper.Translate("Requested Action") }, txtRequestedAction,
                new Label { Text = TranslationHelper.Translate("Requested Privilege Level") }, txtRequestedPrivilegeLevel,
                new Label { Text = TranslationHelper.Translate("Endpoint") }, txtEndpoint,
                new Label { Text = TranslationHelper.Translate("Principal") }, txtPrincipal,
                new Label { Text = TranslationHelper.Translate("From Date") }, dpFrom,
                new Label { Text = TranslationHelper.Translate("To Date") }, dpTo,
                new Label { Text = TranslationHelper.Translate("Limit") }, txtLimit,
                chkIncludeBadRequests, null // Checkbox with no label
            };

            // Arrange controls in rows with specified column count
            for (int i = 0; i < controls.Count; i += columnCount)
            {
                var rowCells = new List<TableCell>();
                for (int j = 0; j < columnCount && i + j < controls.Count; j++)
                {
                    var control = controls[i + j];
                    if (control != null)
                    {
                        if (control is Label)
                        {
                            control.Width = ColorSettings.InnerLabelWidth ?? 150;
                            // Use TextAlignment instead of VerticalAlignment
                            if (control is Label label)
                                label.TextAlignment = TextAlignment.Left;
                        }
                        else
                        {
                            control.Width = ColorSettings.InnerControlWidth ?? 200;
                        }
                        rowCells.Add(new TableCell(control));
                    }
                    else
                    {
                        rowCells.Add(new TableCell());
                    }
                }
                criteriaPanel.Rows.Add(new TableRow(rowCells.ToArray()));
            }

            // Button panel
            var buttonPanel = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Items = {
                    new StackLayoutItem(btnSearch, true),
                    new StackLayoutItem(btnReset, true)
                },
                Height = ColorSettings.InnerControlHeight ?? 30,
                Width = ColorSettings.ControlWidth ?? 400
            };

            // Results panel
            var resultsPanel = new StackLayout
            {
                Items = {
                    new Label { Text = TranslationHelper.Translate("Search Results") },
                    gvResults
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };

            // Main layout
            Content = new StackLayout
            {
                Items = {
                    criteriaPanel,
                    buttonPanel,
                    resultsPanel
                },
                Padding = 10,
                Spacing = 10
            };
        }

        private void SetupEventHandlers()
        {
            // Search button click
            btnSearch.Click += (sender, e) => SearchRequests();

            // Reset button click
            btnReset.Click += (sender, e) => ResetForm();

            // Key handlers for text boxes
            txtToken.KeyDown += HandleTextBoxKeyDown;
            txtType.KeyDown += HandleTextBoxKeyDown;
            txtRequestedAction.KeyDown += HandleTextBoxKeyDown;
            txtRequestedPrivilegeLevel.KeyDown += HandleTextBoxKeyDown;
            txtEndpoint.KeyDown += HandleTextBoxKeyDown;
            txtPrincipal.KeyDown += HandleTextBoxKeyDown;
            txtLimit.KeyDown += HandleTextBoxKeyDown;

            // Global key handlers
            KeyDown += (sender, e) => {
                switch (e.Key)
                {
                    case Keys.F1:
                    case Keys.F2:
                    case Keys.F3:
                    case Keys.F4:
                        SearchRequests();
                        break;
                    case Keys.F5:
                    case Keys.F6:
                        // Edit functionality would go here
                        break;
                    case Keys.F7:
                    case Keys.F8:
                        ResetForm();
                        break;
                    case Keys.F9:
                    case Keys.F10:
                    case Keys.F11:
                    case Keys.F12:
                        // Save functionality would go here
                        break;
                }
            };
        }

        private void HandleTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Enter)
            {
                // Find the next control in the tab order
                var currentControl = sender as Control;
                var allControls = new List<Control> {
                    txtToken, txtType, txtRequestedAction, txtRequestedPrivilegeLevel,
                    txtEndpoint, txtPrincipal, dpFrom, dpTo, txtLimit,
                    chkIncludeBadRequests, btnSearch, btnReset
                };

                var currentIndex = allControls.IndexOf(currentControl);
                if (currentIndex >= 0 && currentIndex < allControls.Count - 1)
                {
                    allControls[currentIndex + 1].Focus();
                }

                e.Handled = true;
            }
        }

        private (bool IsValid, string ConsolidatedErrorList) ValidateInputs()
        {
            var errors = new List<string>();

            // Validate limit
            if (!string.IsNullOrEmpty(txtLimit.Text) && (!int.TryParse(txtLimit.Text, out int limit) || limit <= 0))
            {
                errors.Add(TranslationHelper.Translate("Limit must be a positive integer"));
            }

            // Validate date range
            if (dpFrom.Value > dpTo.Value)
            {
                errors.Add(TranslationHelper.Translate("From date cannot be after To date"));
            }

            // Validate principal if provided
            if (!string.IsNullOrEmpty(txtPrincipal.Text) && (!long.TryParse(txtPrincipal.Text, out long principal) || principal <= 0))
            {
                errors.Add(TranslationHelper.Translate("Principal must be a positive integer"));
            }

            return (errors.Count == 0, string.Join("\n", errors));
        }

        private void SearchRequests()
        {
            var validation = ValidateInputs();
            if (!validation.IsValid)
            {
                MessageBox.Show(validation.ConsolidatedErrorList, TranslationHelper.Translate("Validation Error"), MessageBoxType.Error);
                return;
            }

            try
            {
                // Create search criteria
                var searchCriteria = new RequestSearchCriteria
                {
                    From = dpFrom.Value,
                    To = dpTo.Value,
                    Token = string.IsNullOrEmpty(txtToken.Text) ? null : txtToken.Text,
                    Type = string.IsNullOrEmpty(txtType.Text) ? null : txtType.Text,
                    RequestedAction = string.IsNullOrEmpty(txtRequestedAction.Text) ? null : txtRequestedAction.Text,
                    RequestedPrivilegeLevel = string.IsNullOrEmpty(txtRequestedPrivilegeLevel.Text) ? null : txtRequestedPrivilegeLevel.Text,
                    Endpoint = string.IsNullOrEmpty(txtEndpoint.Text) ? null : txtEndpoint.Text,
                    Principal = string.IsNullOrEmpty(txtPrincipal.Text) ? null : long.Parse(txtPrincipal.Text),
                    IncludeBadRequests = chkIncludeBadRequests.Checked ?? false,
                    Limit = string.IsNullOrEmpty(txtLimit.Text) ? null : int.Parse(txtLimit.Text)
                };

                // Send request to backend
                var (Out, Error) = SendAuthenticatedRequest<RequestSearchCriteria, List<object>>.Send(
                    searchCriteria,
                    "/RequestsSearchEndpoint",
                    true
                );

                if (Error == false)
                {
                    searchResults = Out;
                    gvResults.DataStore = searchResults;
                    Log($"Found {searchResults.Count} results");
                }
                else
                {
                    // Use a generic error message since we don't have access to the error details
                    MessageBox.Show(TranslationHelper.Translate("An error occurred while searching for requests"),
                                   TranslationHelper.Translate("Search Error"), MessageBoxType.Error);
                }
            }
            catch (Exception ex)
            {
                Log($"Error during search: {ex.Message}");
                MessageBox.Show(ex.Message, TranslationHelper.Translate("Search Error"), MessageBoxType.Error);
            }
        }

        private void ResetForm()
        {
            txtToken.Text = "";
            txtType.Text = "";
            txtRequestedAction.Text = "";
            txtRequestedPrivilegeLevel.Text = "";
            txtEndpoint.Text = "";
            txtPrincipal.Text = "";
            chkIncludeBadRequests.Checked = false;
            dpFrom.Value = DateTime.Now.AddDays(-180);
            dpTo.Value = DateTime.Now;
            txtLimit.Text = "100";
            gvResults.DataStore = null;
        }

        private void Log(string message)
        {
            Console.WriteLine($"[RequestsSearchPanel] {DateTime.Now}: {message}");
        }
    }

    // Request search criteria class
    public class RequestSearchCriteria
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public long? Principal { get; set; }
        public string? Token { get; set; }
        public string? Type { get; set; }
        public string? RequestedAction { get; set; }
        public string? RequestedPrivilegeLevel { get; set; }
        public string? Endpoint { get; set; }
        public bool IncludeBadRequests { get; set; } = false;
        public int? Limit { get; set; } = 100;
    }
}