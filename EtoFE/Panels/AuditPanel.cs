using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Reflection;
using Eto.Forms;
using Eto.Drawing;
using CommonUi;
using RV.InvNew.Common;
using JsonEditorExample;

namespace EtoFE.Panels
{
    // Solid type for request data
    public class RequestData
    {
        public DateTime TimeTai { get; set; }
        public long Principal { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string RequestedAction { get; set; } = string.Empty;
        public string RequestedPrivilegeLevel { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string ProvidedPrivilegeLevels { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty; // To differentiate between Request and RequestsBad
        public object OriginalData { get; set; } // Store the original data for double-click handling

        // Convert to a view array for display in the search results

        private static void Log(string message)
        {
            Console.WriteLine($"[RequestsSearchPanel] {DateTime.Now}: {message}");
        }
        public string[] ToViewArray()
        {
            return new string[]
            {
                TimeTai.ToString(),
                Principal.ToString(),
                Token,
                Type,
                RequestedAction,
                RequestedPrivilegeLevel,
                Endpoint,
                ProvidedPrivilegeLevels,
                Source
            };
        }

        // Create from Request object
        public static RequestData FromRequest(Request req)
        {
            Log($"Creating RequestData from Request: Token={req.Token}, Type={req.Type}");

            return new RequestData
            {
                TimeTai = req.TimeTai.UtcDateTime,
                Principal = req.Principal,
                Token = req.Token ?? string.Empty,
                Type = req.Type ?? string.Empty,
                RequestedAction = req.RequestedAction ?? string.Empty,
                RequestedPrivilegeLevel = req.RequestedPrivilegeLevel ?? string.Empty,
                Endpoint = req.Endpoint ?? string.Empty,
                ProvidedPrivilegeLevels = req.ProvidedPrivilegeLevels ?? string.Empty,
                Source = "Request",
                OriginalData = req
            };
        }

        // Create from RequestsBad object
        public static RequestData FromRequestsBad(RequestsBad req)
        {
            Log($"Creating RequestData from RequestsBad: Token={req.Token}, Type={req.Type}");

            return new RequestData
            {
                TimeTai = req.TimeTai,
                Principal = req.Principal ?? 0,
                Token = req.Token ?? string.Empty,
                Type = req.Type ?? string.Empty,
                RequestedAction = req.RequestedAction ?? string.Empty,
                RequestedPrivilegeLevel = req.RequestedPrivilegeLevel ?? string.Empty,
                Endpoint = req.Endpoint ?? string.Empty,
                ProvidedPrivilegeLevels = req.ProvidedPrivilegeLevels ?? string.Empty,
                Source = "RequestsBad",
                OriginalData = req
            };
        }

        // Create from JsonElement with case-insensitive property names
        public static RequestData FromJsonElement(JsonElement element)
        {
            Log("Creating RequestData from JsonElement");

            var data = new RequestData();

            // Helper function to try get property with different case variations
            bool TryGetPropertyCaseInsensitive(JsonElement elem, string propertyName, out JsonElement value)
            {
                // Try exact match first
                if (elem.TryGetProperty(propertyName, out value))
                    return true;

                // Try lowercase
                if (elem.TryGetProperty(char.ToLower(propertyName[0]) + propertyName.Substring(1), out value))
                    return true;

                // Try uppercase
                if (elem.TryGetProperty(char.ToUpper(propertyName[0]) + propertyName.Substring(1), out value))
                    return true;

                return false;
            }

            if (TryGetPropertyCaseInsensitive(element, "TimeTai", out var timeTaiProp))
            {
                if (timeTaiProp.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(timeTaiProp.GetString(), out var timeTai))
                    {
                        data.TimeTai = timeTai;
                        Log($"  TimeTai: {data.TimeTai}");
                    }
                }
                else
                {
                    data.TimeTai = timeTaiProp.GetDateTime();
                    Log($"  TimeTai: {data.TimeTai}");
                }
            }

            if (TryGetPropertyCaseInsensitive(element, "Principal", out var principalProp))
            {
                data.Principal = principalProp.GetInt64();
                Log($"  Principal: {data.Principal}");
            }

            if (TryGetPropertyCaseInsensitive(element, "Token", out var tokenProp))
            {
                if (tokenProp.ValueKind == JsonValueKind.String)
                {
                    data.Token = tokenProp.GetString() ?? string.Empty;

                    // Check if the token is itself a JSON string
                    if (data.Token.StartsWith("{") && data.Token.EndsWith("}"))
                    {
                        try
                        {
                            // Try to parse the token as JSON to extract TokenID
                            var tokenElement = JsonSerializer.Deserialize<JsonElement>(data.Token);
                            if (tokenElement.TryGetProperty("TokenID", out var tokenIdProp))
                            {
                                data.Token = tokenIdProp.GetString() ?? data.Token;
                                Log($"  Extracted TokenID: {data.Token}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"  Error parsing token as JSON: {ex.Message}");
                        }
                    }

                    Log($"  Token: {data.Token}");
                }
            }

            if (TryGetPropertyCaseInsensitive(element, "Type", out var typeProp))
            {
                data.Type = typeProp.GetString() ?? string.Empty;
                Log($"  Type: {data.Type}");
            }

            if (TryGetPropertyCaseInsensitive(element, "RequestedAction", out var requestedActionProp))
            {
                data.RequestedAction = requestedActionProp.GetString() ?? string.Empty;
                Log($"  RequestedAction: {data.RequestedAction}");
            }

            if (TryGetPropertyCaseInsensitive(element, "RequestedPrivilegeLevel", out var requestedPrivilegeLevelProp))
            {
                data.RequestedPrivilegeLevel = requestedPrivilegeLevelProp.GetString() ?? string.Empty;
                Log($"  RequestedPrivilegeLevel: {data.RequestedPrivilegeLevel}");
            }

            if (TryGetPropertyCaseInsensitive(element, "Endpoint", out var endpointProp))
            {
                data.Endpoint = endpointProp.GetString() ?? string.Empty;
                Log($"  Endpoint: {data.Endpoint}");
            }

            if (TryGetPropertyCaseInsensitive(element, "ProvidedPrivilegeLevels", out var providedPrivilegeLevelsProp))
            {
                data.ProvidedPrivilegeLevels = providedPrivilegeLevelsProp.GetString() ?? string.Empty;
                Log($"  ProvidedPrivilegeLevels: {data.ProvidedPrivilegeLevels}");
            }

            // Determine source based on the presence of certain properties
            data.Source = TryGetPropertyCaseInsensitive(element, "ErrorMessage", out _) ? "RequestsBad" : "Request";
            Log($"  Source: {data.Source}");

            data.OriginalData = element;

            return data;
        }

        // Create from unknown object using reflection
        public static RequestData FromUnknownObject(object obj)
        {
            Log($"Creating RequestData from unknown object of type: {obj?.GetType().FullName ?? "null"}");

            var data = new RequestData
            {
                Source = "Unknown",
                OriginalData = obj
            };

            if (obj == null)
                return data;

            // Special handling for JObject without directly referencing it
            if (obj.GetType().FullName?.Contains("JObject") == true)
            {
                try
                {
                    // Convert the JObject to a JSON string using its ToString method
                    var jsonString = obj.ToString();
                    Log($"  Converted JObject to JSON string: {jsonString.Substring(0, Math.Min(100, jsonString.Length))}...");

                    // Parse the JSON string using System.Text.Json
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);
                    return FromJsonElement(jsonElement);
                }
                catch (Exception ex)
                {
                    Log($"  Error handling JObject: {ex.Message}");
                }
            }

            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Helper function to safely get property value
            T GetPropertyValue<T>(string propertyName, T defaultValue = default)
            {
                var prop = properties.FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                if (prop != null && prop.PropertyType == typeof(T))
                {
                    var value = prop.GetValue(obj);
                    if (value != null)
                    {
                        Log($"  {propertyName}: {value}");
                        return (T)value;
                    }
                }
                return defaultValue;
            }

            // Helper function for string properties
            string GetStringProperty(string propertyName)
            {
                var prop = properties.FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                if (prop != null)
                {
                    var value = prop.GetValue(obj)?.ToString();
                    if (value != null)
                    {
                        Log($"  {propertyName}: {value}");
                        return value;
                    }
                }
                return string.Empty;
            }

            data.TimeTai = GetPropertyValue<DateTime>("TimeTai");
            data.Principal = GetPropertyValue<long>("Principal");
            data.Token = GetStringProperty("Token");
            data.Type = GetStringProperty("Type");
            data.RequestedAction = GetStringProperty("RequestedAction");
            data.RequestedPrivilegeLevel = GetStringProperty("RequestedPrivilegeLevel");
            data.Endpoint = GetStringProperty("Endpoint");
            data.ProvidedPrivilegeLevels = GetStringProperty("ProvidedPrivilegeLevels");

            return data;
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
        private List<RequestData> searchResults = new List<RequestData>();

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

                // Log the search criteria
                Log($"Search criteria: {JsonSerializer.Serialize(searchCriteria, new JsonSerializerOptions { WriteIndented = true })}");

                // Send request to backend
                var (Out, Error) = SendAuthenticatedRequest<RequestSearchCriteria, List<object>>.Send(
                    searchCriteria,
                    "/RequestsSearchEndpoint",
                    true
                );

                if (Error == false)
                {
                    // Convert the results to our solid type
                    searchResults = new List<RequestData>();

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
                        RequestData requestData = null;

                        // Try to handle JsonElement case
                        if (item is JsonElement element)
                        {
                            try
                            {
                                // Try to deserialize as Request
                                var request = element.Deserialize<Request>();
                                if (request != null)
                                {
                                    requestData = RequestData.FromRequest(request);
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
                                    requestData = RequestData.FromRequestsBad(badRequest);
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log($"Error deserializing as RequestsBad: {ex.Message}");
                            }

                            // If we can't deserialize to either type, use FromJsonElement
                            requestData = RequestData.FromJsonElement(element);
                        }
                        // Direct type handling
                        else if (item is Request req)
                        {
                            requestData = RequestData.FromRequest(req);
                        }
                        else if (item is RequestsBad badReq)
                        {
                            requestData = RequestData.FromRequestsBad(badReq);
                        }
                        else if (item != null)
                        {
                            Log($"Unknown item type: {item.GetType().FullName}");

                            // Use reflection instead of dynamic to extract properties
                            try
                            {
                                requestData = RequestData.FromUnknownObject(item);
                                Log($"Created RequestData using reflection");
                            }
                            catch (Exception ex)
                            {
                                Log($"Error extracting properties using reflection: {ex.Message}");
                            }
                        }

                        if (requestData != null)
                        {
                            searchResults.Add(requestData);
                            Log($"Added RequestData to results: Token={requestData.Token}, Type={requestData.Type}");
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
                // Convert RequestData objects to string arrays for display
                var viewData = searchResults.Select(r => r.ToViewArray()).ToList();

                // Define the callback for double-click
                Action<string[]> doubleClickCallback = (selected) =>
                {
                    // Find the selected item in our results
                    if (selected != null && selected.Length > 0)
                    {
                        // Try to find the item by matching the TimeTai (first column)
                        var timeTai = selected[0];
                        var selectedItem = searchResults.FirstOrDefault(r => r.TimeTai.ToString() == timeTai);

                        if (selectedItem != null && selectedItem.OriginalData != null)
                        {
                            ShowJsonEditor(selectedItem.OriginalData);
                        }
                    }
                };

                // Create a simple grid view instead of using SearchPanelUtility
                var gridView = new GridView();

                // Add columns
                gridView.Columns.Add(new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property((string[] item) => item[0]) }, HeaderText = "TimeTai" });
                gridView.Columns.Add(new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property((string[] item) => item[1]) }, HeaderText = "Principal" });
                gridView.Columns.Add(new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property((string[] item) => item[2]) }, HeaderText = "Token" });
                gridView.Columns.Add(new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property((string[] item) => item[3]) }, HeaderText = "Type" });
                gridView.Columns.Add(new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property((string[] item) => item[4]) }, HeaderText = "RequestedAction" });
                gridView.Columns.Add(new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property((string[] item) => item[5]) }, HeaderText = "RequestedPrivilegeLevel" });
                gridView.Columns.Add(new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property((string[] item) => item[6]) }, HeaderText = "Endpoint" });
                gridView.Columns.Add(new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property((string[] item) => item[7]) }, HeaderText = "ProvidedPrivilegeLevels" });
                gridView.Columns.Add(new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property((string[] item) => item[8]) }, HeaderText = "Source" });

                // Set the data source
                gridView.DataStore = viewData;

                // Handle double-click
                gridView.MouseDoubleClick += (sender, e) =>
                {
                    var selectedRow = gridView.SelectedItem as string[];
                    if (selectedRow != null)
                    {
                        doubleClickCallback(selectedRow);
                    }
                };

                // Remove the old results panel (last item in mainLayout)
                if (mainLayout.Items.Count > 0)
                {
                    mainLayout.Items.RemoveAt(mainLayout.Items.Count - 1);
                }

                // Add the new results panel
                mainLayout.Items.Add(new StackLayoutItem(gridView, true));

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

        private void ShowJsonEditor(object data)
        {
            try
            {
                string json;

                // Handle JObject without directly referencing it
                if (data?.GetType().FullName?.Contains("JObject") == true)
                {
                    // Convert the JObject to a JSON string using its ToString method
                    json = data.ToString();
                    Log($"Converted JObject to JSON string for editor");
                }
                else
                {
                    // Serialize the data to JSON with proper formatting
                    var jsonOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = null
                    };

                    json = JsonSerializer.Serialize(data, jsonOptions);
                }

                Log($"Opening JSON editor with data: {json.Substring(0, Math.Min(200, json.Length))}...");

                // Create a new form with the JsonEditorPanel using the specified signature
                var form = new Form
                {
                    Title = TranslationHelper.Translate("Request Details"),
                    ClientSize = new Size(800, 600),
                    Content = new JsonEditorPanel(
                        json, // Pass the JSON string directly
                        Orientation.Vertical
                    )
                };

                form.Show();
            }
            catch (Exception ex)
            {
                Log($"Error showing JSON editor: {ex.Message}");
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
            mainLayout.Items.Add(new StackLayoutItem(resetResultsPanel, true));

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