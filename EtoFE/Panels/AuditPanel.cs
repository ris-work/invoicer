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
    // Unified view model for both Request and RequestsBad
    public class RequestViewModel
    {
        public string TimeTai { get; set; } = string.Empty;
        public string Principal { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string RequestedAction { get; set; } = string.Empty;
        public string RequestedPrivilegeLevel { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string ProvidedPrivilegeLevels { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty; // To differentiate between Request and RequestsBad

        public static RequestViewModel FromRequest(Request req)
        {
            return new RequestViewModel
            {
                TimeTai = req.TimeTai.ToString(),
                Principal = req.Principal.ToString(),
                Token = req.Token,
                Type = req.Type ?? string.Empty,
                RequestedAction = req.RequestedAction ?? string.Empty,
                RequestedPrivilegeLevel = req.RequestedPrivilegeLevel ?? string.Empty,
                Endpoint = req.Endpoint ?? string.Empty,
                ProvidedPrivilegeLevels = req.ProvidedPrivilegeLevels ?? string.Empty,
                Source = "Request"
            };
        }

        public static RequestViewModel FromRequestsBad(RequestsBad req)
        {
            return new RequestViewModel
            {
                TimeTai = req.TimeTai.ToString(),
                Principal = req.Principal?.ToString() ?? string.Empty,
                Token = req.Token,
                Type = req.Type ?? string.Empty,
                RequestedAction = req.RequestedAction ?? string.Empty,
                RequestedPrivilegeLevel = req.RequestedPrivilegeLevel ?? string.Empty,
                Endpoint = req.Endpoint ?? string.Empty,
                ProvidedPrivilegeLevels = req.ProvidedPrivilegeLevels ?? string.Empty,
                Source = "RequestsBad"
            };
        }
    }

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
        private StackLayout mainLayout;

        // Data
        private List<RequestViewModel> searchResults = new List<RequestViewModel>();

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
                Items =
                {
                    new StackLayoutItem(btnSearch, true),
                    new StackLayoutItem(btnReset, true)
                },
                Height = ColorSettings.InnerControlHeight ?? 30,
                Width = ColorSettings.ControlWidth ?? 400
            };

            // Initial results panel with placeholder
            var initialResultsPanel = new Panel
            {
                Content = new Label { Text = TranslationHelper.Translate("No search results yet") }
            };

            // Create main layout
            mainLayout = new StackLayout
            {
                Items =
                {
                    criteriaPanel,
                    buttonPanel
                },
                Padding = 10,
                Spacing = 10
            };

            // Add results panel separately to make it easier to replace
            mainLayout.Items.Add(initialResultsPanel);

            // Set the content
            Content = mainLayout;
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
            KeyDown += (sender, e) =>
            {
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
                var allControls = new List<Control>
                {
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
            if (!string.IsNullOrEmpty(txtLimit.Text) &&
                (!int.TryParse(txtLimit.Text, out int limit) || limit <= 0))
            {
                errors.Add(TranslationHelper.Translate("Limit must be a positive integer"));
            }

            // Validate date range
            if (dpFrom.Value > dpTo.Value)
            {
                errors.Add(TranslationHelper.Translate("From date cannot be after To date"));
            }

            // Validate principal if provided
            if (!string.IsNullOrEmpty(txtPrincipal.Text) &&
                (!long.TryParse(txtPrincipal.Text, out long principal) || principal <= 0))
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
                MessageBox.Show(
                    validation.ConsolidatedErrorList,
                    TranslationHelper.Translate("Validation Error"),
                    MessageBoxType.Error
                );
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
                    // Convert the results to our view model
                    searchResults = new List<RequestViewModel>();

                    // Debug: Log the raw results
                    Log($"Raw results count: {Out?.Count ?? 0}");

                    // Debug: Log the types of the first few items
                    if (Out != null && Out.Count > 0)
                    {
                        for (int i = 0; i < Math.Min(5, Out.Count); i++)
                        {
                            var item = Out[i];
                            Log($"Item {i} type: {item?.GetType().FullName ?? "null"}");

                            // Try to serialize to JSON to see the structure
                            try
                            {
                                var json = JsonSerializer.Serialize(item);
                                Log($"Item {i} JSON: {json}");
                            }
                            catch (Exception ex)
                            {
                                Log($"Error serializing item {i}: {ex.Message}");
                            }
                        }
                    }

                    foreach (var item in Out)
                    {
                        // Try to handle JsonElement case
                        if (item is JsonElement element)
                        {
                            try
                            {
                                // Try to deserialize as Request
                                var request = element.Deserialize<Request>();
                                if (request != null)
                                {
                                    searchResults.Add(RequestViewModel.FromRequest(request));
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log($"Error deserializing as Request: {ex.Message}");
                            }

                            try
                            {
                                // Try to deserialize as RequestsBad
                                var badRequest = element.Deserialize<RequestsBad>();
                                if (badRequest != null)
                                {
                                    searchResults.Add(RequestViewModel.FromRequestsBad(badRequest));
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log($"Error deserializing as RequestsBad: {ex.Message}");
                            }

                            // If we can't deserialize to either type, try to extract properties manually
                            try
                            {
                                var manualViewModel = new RequestViewModel();

                                if (element.TryGetProperty("TimeTai", out var timeTaiProp))
                                    manualViewModel.TimeTai = timeTaiProp.GetDateTime().ToString();

                                if (element.TryGetProperty("Principal", out var principalProp))
                                    manualViewModel.Principal = principalProp.GetInt64().ToString();

                                if (element.TryGetProperty("Token", out var tokenProp))
                                    manualViewModel.Token = tokenProp.GetString() ?? string.Empty;

                                if (element.TryGetProperty("Type", out var typeProp))
                                    manualViewModel.Type = typeProp.GetString() ?? string.Empty;

                                if (element.TryGetProperty("RequestedAction", out var requestedActionProp))
                                    manualViewModel.RequestedAction = requestedActionProp.GetString() ?? string.Empty;

                                if (element.TryGetProperty("RequestedPrivilegeLevel", out var requestedPrivilegeLevelProp))
                                    manualViewModel.RequestedPrivilegeLevel = requestedPrivilegeLevelProp.GetString() ?? string.Empty;

                                if (element.TryGetProperty("Endpoint", out var endpointProp))
                                    manualViewModel.Endpoint = endpointProp.GetString() ?? string.Empty;

                                if (element.TryGetProperty("ProvidedPrivilegeLevels", out var providedPrivilegeLevelsProp))
                                    manualViewModel.ProvidedPrivilegeLevels = providedPrivilegeLevelsProp.GetString() ?? string.Empty;

                                // Determine source based on the presence of certain properties
                                manualViewModel.Source = element.TryGetProperty("ErrorMessage", out _) ? "RequestsBad" : "Request";

                                searchResults.Add(manualViewModel);
                                Log($"Manually created RequestViewModel from JsonElement");
                            }
                            catch (Exception ex)
                            {
                                Log($"Error manually extracting properties: {ex.Message}");
                            }
                        }
                        // Direct type handling
                        else if (item is Request req)
                        {
                            searchResults.Add(RequestViewModel.FromRequest(req));
                        }
                        else if (item is RequestsBad badReq)
                        {
                            searchResults.Add(RequestViewModel.FromRequestsBad(badReq));
                        }
                        else if (item != null)
                        {
                            Log($"Unknown item type: {item.GetType().FullName}");

                            // Try to cast to dynamic and extract properties
                            try
                            {
                                dynamic dynamicItem = item;
                                var manualViewModel = new RequestViewModel
                                {
                                    TimeTai = dynamicItem.TimeTai?.ToString() ?? string.Empty,
                                    Principal = dynamicItem.Principal?.ToString() ?? string.Empty,
                                    Token = dynamicItem.Token?.ToString() ?? string.Empty,
                                    Type = dynamicItem.Type?.ToString() ?? string.Empty,
                                    RequestedAction = dynamicItem.RequestedAction?.ToString() ?? string.Empty,
                                    RequestedPrivilegeLevel = dynamicItem.RequestedPrivilegeLevel?.ToString() ?? string.Empty,
                                    Endpoint = dynamicItem.Endpoint?.ToString() ?? string.Empty,
                                    ProvidedPrivilegeLevels = dynamicItem.ProvidedPrivilegeLevels?.ToString() ?? string.Empty,
                                    Source = "Unknown"
                                };

                                searchResults.Add(manualViewModel);
                                Log($"Manually created RequestViewModel from dynamic object");
                            }
                            catch (Exception ex)
                            {
                                Log($"Error extracting from dynamic object: {ex.Message}");
                            }
                        }
                    }

                    // Debug: Log the converted results
                    Log($"Converted results count: {searchResults.Count}");

                    // Update the results panel
                    UpdateResultsPanel();
                }
                else
                {
                    // Use a generic error message since we don't have access to the error details
                    MessageBox.Show(
                        TranslationHelper.Translate("An error occurred while searching for requests"),
                        TranslationHelper.Translate("Search Error"),
                        MessageBoxType.Error
                    );
                }
            }
            catch (Exception ex)
            {
                Log($"Error during search: {ex.Message}");
                MessageBox.Show(
                    ex.Message,
                    TranslationHelper.Translate("Search Error"),
                    MessageBoxType.Error
                );
            }
        }

        private void UpdateResultsPanel()
        {
            try
            {
                // Generate the search results panel - explicitly cast to Panel
                Panel newResultsPanel = (Panel)SearchPanelUtility.GenerateSearchPanel(
                    searchResults,
                    false,
                    null,
                    [
                        "TimeTai",
                        "Principal",
                        "Token",
                        "Type",
                        "RequestedAction",
                        "RequestedPrivilegeLevel",
                        "Endpoint",
                        "ProvidedPrivilegeLevels",
                        "Source"
                    ]
                );

                // Debug: Check if the panel is null
                if (newResultsPanel == null)
                {
                    Log("Error: SearchPanelUtility.GenerateSearchPanel returned null");
                    newResultsPanel = new Panel { Content = new Label { Text = TranslationHelper.Translate("Error generating results panel") } };
                }

                // Remove the old results panel (last item in mainLayout)
                if (mainLayout.Items.Count > 0)
                {
                    mainLayout.Items.RemoveAt(mainLayout.Items.Count - 1);
                }

                // Add the new results panel
                mainLayout.Items.Add(new StackLayoutItem(newResultsPanel,true));

                // Force a layout update
                mainLayout.Invalidate();


                Log("Results panel updated successfully");
            }
            catch (Exception ex)
            {
                Log($"Error updating results panel: {ex.Message}");
                MessageBox.Show(
                    ex.Message,
                    TranslationHelper.Translate("Display Error"),
                    MessageBoxType.Error
                );
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

            // Reset results panel
            var resetResultsPanel = new Panel
            {
                Content = new Label { Text = TranslationHelper.Translate("No search results yet") }
            };

            // Remove the old results panel (last item in mainLayout)
            if (mainLayout.Items.Count > 0)
            {
                mainLayout.Items.RemoveAt(mainLayout.Items.Count - 1);
            }

            // Add the reset results panel
            mainLayout.Items.Add(new StackLayoutItem(resetResultsPanel));

            // Force a layout update
            mainLayout.Invalidate();

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