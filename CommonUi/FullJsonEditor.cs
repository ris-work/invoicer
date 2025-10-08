using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Eto.Drawing;
using Eto.Forms;

namespace JsonEditorExample
{
    // Define a logging delegate for better control
    public delegate void LogDelegate(string message);

    public class FullJsonEditorPanel : Panel
    {
        private int fontSize = 12;
        private int cWidth = 200;
        private bool _appMode = false;
        private bool _showHeader = true;
        private string _currentJson = "{}"; // Initialize with empty object
        private StackLayout _rootLayout;
        private StackLayout _contentContainer;
        private Dictionary<string, Control> _controlCache = new Dictionary<string, Control>();
        private bool _showUploadButton = true;

        // Add a logging delegate
        public static LogDelegate Log = Console.WriteLine;

        // Add a reference to root panel for nested panels
        private FullJsonEditorPanel _rootPanel;

        // Track the path of this panel
        private string _panelPath;

        // JSON serialization options for reuse
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Gets or sets whether panel is in app mode, which limits interactions to buttons and date fields.
        /// </summary>
        public bool AppMode
        {
            get => _appMode;
            set
            {
                _appMode = value;
                UpdateControlStates();
            }
        }

        /// <summary>
        /// Gets or sets whether header with Show/Load buttons is visible.
        /// </summary>
        public bool ShowHeader
        {
            get => _showHeader;
            set
            {
                _showHeader = value;
                UpdateHeaderVisibility();
            }
        }

        /// <summary>
        /// For each node path (e.g. "Parent.Child" or "array[0]"), original JSON type is stored.
        /// </summary>
        public Dictionary<string, Type> OriginalTypes { get; private set; } = new Dictionary<string, Type>();

        /// <summary>
        /// Constructs a FullJsonEditorPanel from a JSON object expressed as an IReadOnlyDictionary.
        /// </summary>
        public FullJsonEditorPanel(IReadOnlyDictionary<string, JsonElement> data, Orientation orientation, bool showHeader = true, bool showUploadButton = true)
            : this(data, orientation, "", null, showHeader, showUploadButton) { }

        /// <summary>
        /// Constructs a FullJsonEditorPanel from a JSON string.
        /// </summary>
        /// <param name="jsonString">The JSON string to parse and display</param>
        /// <param name="baseOrientation">The base orientation for the layout (Horizontal or Vertical)</param>
        /// <param name="showHeader">Whether to show the header with Show/Load buttons</param>
        /// <param name="showUploadButton">Whether to show upload buttons for potential images</param>
        public FullJsonEditorPanel(
            string jsonString,
            Orientation baseOrientation,
            bool showHeader = true,
            bool showUploadButton = true
        )
        {
            Log($"[Init] String constructor called with orientation: {baseOrientation}, showHeader: {showHeader}, showUploadButton: {showUploadButton}");

            _showHeader = showHeader;
            _showUploadButton = showUploadButton;
            _rootPanel = this; // This is root panel
            _panelPath = ""; // Root panel has empty path

            try
            {
                // Validate and normalize JSON
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    Log("[Init] JSON string is empty, using empty object");
                    jsonString = "{}";
                }

                Log($"[Init] Original JSON: {jsonString}");

                // Parse and validate JSON
                var document = JsonDocument.Parse(jsonString);
                _currentJson = JsonSerializer.Serialize(document, _jsonOptions);

                Log($"[Init] Normalized JSON: {_currentJson}");
            }
            catch (JsonException ex)
            {
                // If JSON is invalid, start with an empty object
                Log($"[Init] Invalid JSON: {ex.Message}");
                _currentJson = "{}";
            }

            var data = ConvertJsonToDictionary(_currentJson);
            Initialize(data, baseOrientation, "", null, showHeader, showUploadButton);

            Log("[Init] String constructor completed successfully");
        }

        /// <summary>
        /// Constructs a FullJsonEditorPanel from a JSON string.
        /// </summary>
        /// <summary>
        /// Private constructor that supports recursive calls.
        /// </summary>
        private FullJsonEditorPanel(
            IReadOnlyDictionary<string, JsonElement> data,
            Orientation orientation,
            string path,
            Dictionary<string, Type> originalTypes,
            bool showHeader = true,
            bool showUploadButton = true
        )
        {
            _showHeader = showHeader;
            _showUploadButton = showUploadButton;
            _panelPath = path;


            // For nested panels, we need to find root panel to access current JSON
            // This is a bit of a hack, but it's necessary for nested panels to work correctly
            if (string.IsNullOrEmpty(path))
            {
                _rootPanel = this; // This is root panel
            }
            else
            {
                // For nested panels, we need to traverse up to find the root panel
                // This is a simplified approach - in a real implementation, you might want to pass a reference
                _rootPanel = this;
            }

            if (originalTypes != null)
                OriginalTypes = originalTypes;
            else
                OriginalTypes = new Dictionary<string, Type>();

            Log($"[Init] Creating FullJsonEditorPanel at path \"{path}\" with orientation {orientation}");

            // Create the root layout with header
            _rootLayout = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };

            // Create header if needed
            if (_showHeader && string.IsNullOrEmpty(path)) // Only show header for root panel
            {
                var header = CreateHeader();
                _rootLayout.Items.Add(new StackLayoutItem(header, HorizontalAlignment.Stretch));
            }

            // Create a container for the content
            _contentContainer = new StackLayout { Orientation = orientation, Spacing = 5 };
            _rootLayout.Items.Add(new StackLayoutItem(_contentContainer, HorizontalAlignment.Stretch, true));

            BuildFromDictionary(data, _contentContainer, orientation, path);

            this.Content = _rootLayout;

            // Set control states based on app mode
            UpdateControlStates();
        }

        /// <summary>
        /// Private constructor that supports recursive calls.
        /// </summary>
        // Replace the nested panel constructor with this corrected version:
        private FullJsonEditorPanel(
            IReadOnlyDictionary<string, JsonElement> data,
            Orientation orientation,
            string path,
            Dictionary<string, Type> originalTypes,
            bool showHeader = true
        )
        {
            _showHeader = showHeader;
            _panelPath = path;

            // For nested panels, we need to find root panel to access current JSON
            // This is a bit of a hack, but it's necessary for nested panels to work correctly
            if (string.IsNullOrEmpty(path))
            {
                _rootPanel = this; // This is root panel
            }
            else
            {
                // For nested panels, we need to find the root panel
                // In a real implementation, you might want to pass a reference to the root panel
                _rootPanel = this; // This is a simplified approach - in a real implementation, you might want to pass a reference
            }

            if (originalTypes != null)
                OriginalTypes = originalTypes;
            else
                OriginalTypes = new Dictionary<string, Type>();

            Log($"[Init] Creating FullJsonEditorPanel at path \"{path}\" with orientation {orientation}");

            // Create the root layout with header
            _rootLayout = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };

            // Create header if needed
            if (_showHeader && string.IsNullOrEmpty(path)) // Only show header for root panel
            {
                var header = CreateHeader();
                _rootLayout.Items.Add(new StackLayoutItem(header, HorizontalAlignment.Stretch));
            }

            // Create a container for the content
            _contentContainer = new StackLayout { Orientation = orientation, Spacing = 5 };
            _rootLayout.Items.Add(new StackLayoutItem(_contentContainer, HorizontalAlignment.Stretch, true));

            BuildFromDictionary(data, _contentContainer, orientation, path);

            this.Content = _rootLayout;

            // Set control states based on app mode
            UpdateControlStates();
        }

        /// <summary>
        /// Private initialization method that supports recursive calls.
        /// </summary>
        /// <param name="data">The JSON data dictionary to build from</param>
        /// <param name="orientation">The orientation for the layout</param>
        /// <param name="path">The path of this panel in the JSON structure</param>
        /// <param name="originalTypes">Dictionary of original types for validation</param>
        /// <param name="showHeader">Whether to show the header with Show/Load buttons</param>
        /// <param name="showUploadButton">Whether to show upload buttons for potential images</param>
        private void Initialize(
            IReadOnlyDictionary<string, JsonElement> data,
            Orientation orientation,
            string path,
            Dictionary<string, Type> originalTypes,
            bool showHeader,
            bool showUploadButton
        )
        {
            Log($"[Initialize] Starting with path: '{path}', orientation: {orientation}, showHeader: {showHeader}, showUploadButton: {showUploadButton}");

            _showHeader = showHeader;
            _showUploadButton = showUploadButton;
            _panelPath = path;

            // For nested panels, we need to find root panel to access current JSON
            if (string.IsNullOrEmpty(path))
            {
                _rootPanel = this; // This is root panel
            }
            else
            {
                // For nested panels, we need to traverse up to find the root panel
                _rootPanel = this;
            }

            if (originalTypes != null)
                OriginalTypes = originalTypes;
            else
                OriginalTypes = new Dictionary<string, Type>();

            Log($"[Initialize] Creating FullJsonEditorPanel at path \"{path}\" with orientation {orientation}");

            // Create the root layout with header
            _rootLayout = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };

            // Create header if needed
            if (_showHeader && string.IsNullOrEmpty(path)) // Only show header for root panel
            {
                var header = CreateHeader();
                _rootLayout.Items.Add(new StackLayoutItem(header, HorizontalAlignment.Stretch));
            }

            // Create a container for the content
            _contentContainer = new StackLayout { Orientation = orientation, Spacing = 5 };
            _rootLayout.Items.Add(new StackLayoutItem(_contentContainer, HorizontalAlignment.Stretch, true));

            BuildFromDictionary(data, _contentContainer, orientation, path);

            this.Content = _rootLayout;

            // Set control states based on app mode
            UpdateControlStates();

            Log($"[Initialize] Completed successfully for path: '{path}'");
        }
        /// <summary>
        /// Gets root panel, which contains current JSON.
        /// </summary>
        private FullJsonEditorPanel GetRootPanel()
        {
            // If this is root panel, return itself
            if (_rootPanel == this)
                return this;

            // Otherwise, return the stored root panel reference
            return _rootPanel;
        }

        /// <summary>
        /// Gets current JSON from root panel.
        /// </summary>
        private string GetCurrentJson()
        {
            var rootPanel = GetRootPanel();
            return rootPanel._currentJson;
        }

        /// <summary>
        /// Sets current JSON in root panel.
        /// </summary>
        private void SetCurrentJson(string json)
        {
            var rootPanel = GetRootPanel();
            rootPanel._currentJson = json;
            Log($"[SetCurrentJson] Updated stored JSON: {json}");
        }

        /// <summary>
        /// Refreshes the UI for a specific path without rebuilding the entire tree.
        /// </summary>
        private void RefreshPath(string path)
        {
            Log($"[Refresh Path] Starting - Refreshing path: {path}");

            // If path is empty, refresh the entire root
            if (string.IsNullOrEmpty(path))
            {
                Log("[Refresh Path] Path is empty, refreshing entire root");
                var json = GetCurrentJson();
                UpdateJson(json);
                return;
            }

            // Get the updated JSON for this path
            var jsonDocument = JsonDocument.Parse(GetCurrentJson());

            // Check if this is an array path
            if (path.Contains("[") && path.Contains("]"))
            {
                // Extract the array path (parent of the array)
                var match = Regex.Match(path, @"(.+)\[(\d+)\]");
                if (match.Success)
                {
                    var arrayPath = match.Groups[1].Value;
                    Log($"[Refresh Path] Detected array path, refreshing parent: '{arrayPath}'");
                    RefreshPath(arrayPath);
                    return;
                }
            }

            JsonElement element;

            try
            {
                element = NavigateToPath(jsonDocument.RootElement, path);
                Log($"[Refresh Path] Successfully navigated to path '{path}', ValueKind: {element.ValueKind}");
            }
            catch (Exception ex)
            {
                Log($"[Refresh Path] Error navigating to path '{path}': {ex.Message}");
                // If the path no longer exists, refresh the parent
                var lastDot = path.LastIndexOf('.');
                var parentPath = lastDot >= 0 ? path.Substring(0, lastDot) : "";
                Log($"[Refresh Path] Path no longer exists, refreshing parent: '{parentPath}'");
                if (!string.IsNullOrEmpty(parentPath))
                    RefreshPath(parentPath);
                return;
            }

            // Find the panel that owns this path
            var panel = FindPanelForPath(path);
            if (panel == null)
            {
                Log($"[Refresh Path] Panel not found for path: {path}, searching for parent panel");

                // Try to find parent panel
                var lastDot = path.LastIndexOf('.');
                var parentPath = lastDot >= 0 ? path.Substring(0, lastDot) : "";
                if (!string.IsNullOrEmpty(parentPath))
                {
                    panel = FindPanelForPath(parentPath);
                    if (panel != null)
                    {
                        Log($"[Refresh Path] Found parent panel for path '{parentPath}'");
                        path = parentPath;
                        element = NavigateToPath(jsonDocument.RootElement, path);
                    }
                }

                if (panel == null)
                {
                    Log($"[Refresh Path] Could not find any panel for path or parent paths");
                    return;
                }
            }

            Log($"[Refresh Path] Found panel at path '{path}', ValueKind: {element.ValueKind}");

            // If this is an object, rebuild the panel
            if (element.ValueKind == JsonValueKind.Object)
            {
                Log($"[Refresh Path] Rebuilding object panel for path '{path}'");
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(element.GetRawText());
                panel._contentContainer.Items.Clear();
                panel.BuildFromDictionary(dict, panel._contentContainer, panel._contentContainer.Orientation, path);
            }
            // If this is an array, rebuild the list
            else if (element.ValueKind == JsonValueKind.Array)
            {
                Log($"[Refresh Path] Detected array path '{path}', rebuilding parent panel");

                // Get the parent panel that owns this array
                var lastDot = path.LastIndexOf('.');
                var parentPath = lastDot >= 0 ? path.Substring(0, lastDot) : "";

                if (!string.IsNullOrEmpty(parentPath))
                {
                    var parentPanel = FindPanelForPath(parentPath);
                    if (parentPanel != null)
                    {
                        Log($"[Refresh Path] Rebuilding parent panel '{parentPath}' that contains the array");
                        var parentElement = NavigateToPath(jsonDocument.RootElement, parentPath);
                        var parentDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(parentElement.GetRawText());

                        // Clear and rebuild the entire parent panel
                        parentPanel._contentContainer.Items.Clear();
                        parentPanel.BuildFromDictionary(parentDict, parentPanel._contentContainer, parentPanel._contentContainer.Orientation, parentPath);
                        return;
                    }
                }

                // If we can't find the parent panel, try to rebuild the array directly
                Log($"[Refresh Path] Could not find parent panel for array, rebuilding array directly");
                var list = element.EnumerateArray().ToList();
                panel._contentContainer.Items.Clear();
                var newContainer = panel.BuildFromList(list, panel._contentContainer.Orientation, path);
                panel._contentContainer.Items.Add(new StackLayoutItem(newContainer, HorizontalAlignment.Stretch, true));
            }
            else
            {
                Log($"[Refresh Path] Unknown ValueKind {element.ValueKind} for path '{path}'");
            }

            Log("[Refresh Path] Refresh completed");
        }

        /// <summary>
        /// Finds the panel that owns a specific path.
        /// </summary>
        private FullJsonEditorPanel FindPanelForPath(string path)
        {
            // If this panel owns the path, return it
            if (path.StartsWith(_panelPath) || _panelPath == "")
                return this;

            // Otherwise, search child panels
            foreach (var control in _contentContainer.Controls.OfType<FullJsonEditorPanel>())
            {
                var foundPanel = control.FindPanelForPath(path);
                if (foundPanel != null)
                    return foundPanel;
            }

            return null;
        }

        /// <summary>
        /// Finds the parent panel that contains the specified panel.
        /// </summary>
        private FullJsonEditorPanel FindParentPanel(FullJsonEditorPanel childPanel)
        {
            // Check if the panel is directly in this container
            foreach (var item in _contentContainer.Items)
            {
                if (item.Control == childPanel)
                    return this;
            }

            // Check nested panels
            foreach (var control in _contentContainer.Controls.OfType<FullJsonEditorPanel>())
            {
                var parentPanel = control.FindParentPanel(childPanel);
                if (parentPanel != null)
                    return parentPanel;
            }

            return null;
        }

        /// <summary>
        /// Finds the index of a panel in its parent container.
        /// </summary>
        private int FindPanelIndexInParent(FullJsonEditorPanel parentPanel, FullJsonEditorPanel childPanel)
        {
            for (int i = 0; i < parentPanel._contentContainer.Items.Count; i++)
            {
                if (parentPanel._contentContainer.Items[i].Control == childPanel)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Creates the header with Show and Load buttons.
        /// </summary>
        private Control CreateHeader()
        {
            var headerPanel = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Padding = new Padding(5)
            };

            var showButton = new Button { Text = "Show JSON" };
            var loadButton = new Button { Text = "Load JSON" };
            var validateButton = new Button { Text = "Validate" };

            // Attach event handlers
            showButton.Click += ShowJsonDialog;
            loadButton.Click += LoadJsonDialog;
            validateButton.Click += ValidateAndShowErrors;

            headerPanel.Items.Add(new StackLayoutItem(showButton));
            headerPanel.Items.Add(new StackLayoutItem(loadButton));
            headerPanel.Items.Add(new StackLayoutItem(validateButton));

            return headerPanel;
        }

        /// <summary>
        /// Updates the visibility of the header based on the ShowHeader property.
        /// </summary>
        private void UpdateHeaderVisibility()
        {
            if (_rootLayout == null || _rootLayout.Items.Count == 0)
                return;

            // Check if header is currently visible
            bool headerVisible = _rootLayout.Items.Count > 0 && _rootLayout.Items[0].Control is StackLayout;

            // Show header if needed and not already visible
            if (_showHeader && !headerVisible)
            {
                var header = CreateHeader();
                _rootLayout.Items.Insert(0, new StackLayoutItem(header, HorizontalAlignment.Stretch));
            }
            // Hide header if needed and currently visible
            else if (!_showHeader && headerVisible)
            {
                _rootLayout.Items.RemoveAt(0);
            }
        }

        /// <summary>
        /// Shows a dialog with the current JSON.
        /// </summary>
        private void ShowJsonDialog(object sender, EventArgs e)
        {
            Log("[Show JSON] Starting to show JSON dialog");

            try
            {
                // Always get the latest JSON from the UI
                var json = ToJson();
                Log($"[Show JSON] Current JSON: {json}");

                // Create dialog and content separately
                var dialog = new Dialog
                {
                    Title = "Current JSON",
                    ClientSize = new Size(600, 400)
                };

                var textArea = new TextArea
                {
                    Text = json,
                    ReadOnly = true,
                    Font = Fonts.Monospace(10)
                };

                var closeButton = new Button { Text = "Close" };
                closeButton.Click += (s, args) => dialog.Close();

                var layout = new StackLayout
                {
                    Padding = 10,
                    Spacing = 5,
                    Items =
            {
                new Label { Text = "Current JSON:" },
                new StackLayoutItem(textArea, HorizontalAlignment.Stretch, true),
                new StackLayoutItem(closeButton, HorizontalAlignment.Right)
            }
                };

                dialog.Content = layout;
                dialog.ShowModal(this);

                Log("[Show JSON] Dialog shown successfully");
            }
            catch (Exception ex)
            {
                Log($"[Show JSON] Error: {ex.Message}");
                MessageBox.Show(this, $"Error showing JSON: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
            }
        }

        /// <summary>
        /// Shows a dialog to load new JSON.
        /// </summary>
        private void LoadJsonDialog(object sender, EventArgs e)
        {
            // Create dialog and content separately
            var dialog = new Dialog<string>
            {
                Title = "Load JSON",
                ClientSize = new Size(600, 400)
            };

            var jsonTextArea = new TextArea
            {
                Text = GetCurrentJson(),
                Font = Fonts.Monospace(10)
            };

            var loadButton = new Button { Text = "Load" };
            var cancelButton = new Button { Text = "Cancel" };

            // Attach event handlers
            loadButton.Click += (s, args) => dialog.Close(jsonTextArea.Text);
            cancelButton.Click += (s, args) => dialog.Close(null);

            var buttonPanel = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Items =
                {
                    new StackLayoutItem(loadButton),
                    new StackLayoutItem(cancelButton)
                }
            };

            var layout = new StackLayout
            {
                Padding = 10,
                Spacing = 5,
                Items =
                {
                    new Label { Text = "Enter new JSON:" },
                    new StackLayoutItem(jsonTextArea, HorizontalAlignment.Stretch, true),
                    new StackLayoutItem(buttonPanel, HorizontalAlignment.Right)
                }
            };

            dialog.Content = layout;

            var result = dialog.ShowModal(this);
            if (!string.IsNullOrEmpty(result))
            {
                try
                {
                    Log($"[Load JSON] New JSON to load: {result}");
                    UpdateJson(result);
                    Log("[Load JSON] JSON loaded successfully");
                }
                catch (Exception ex)
                {
                    Log($"[Load JSON] Error: {ex.Message}");
                    MessageBox.Show(this, $"Error loading JSON: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                }
            }
        }

        /// <summary>
        /// Validates the current JSON and shows any errors.
        /// </summary>
        private void ValidateAndShowErrors(object sender, EventArgs e)
        {
            var errors = ValidateFields();

            // Create dialog and content separately
            var dialog = new Dialog
            {
                Title = "Validation Results",
                ClientSize = new Size(500, 300)
            };

            Control errorContent;

            if (errors.Any())
            {
                var errorList = new StackLayout { Spacing = 5 };
                foreach (var error in errors)
                {
                    errorList.Items.Add(new Label { Text = $"• {error}" });
                }

                // Use a Scrollable control instead of ScrollView
                var scrollable = new Scrollable
                {
                    Content = errorList,
                    Border = BorderType.None
                };

                errorContent = new StackLayout
                {
                    Spacing = 5,
                    Items =
                    {
                        new Label { Text = "Validation errors found:" },
                        new StackLayoutItem(scrollable, HorizontalAlignment.Stretch, true)
                    }
                };
            }
            else
            {
                errorContent = new Label { Text = "No validation errors found." };
            }

            var closeButton = new Button { Text = "Close" };
            closeButton.Click += (s, args) => dialog.Close();

            var layout = new StackLayout
            {
                Padding = 10,
                Spacing = 5,
                Items =
                {
                    new StackLayoutItem(errorContent, HorizontalAlignment.Stretch, true),
                    new StackLayoutItem(closeButton, HorizontalAlignment.Right)
                }
            };

            dialog.Content = layout;
            dialog.ShowModal(this);
        }

        /// <summary>
        /// Updates the panel with new JSON data.
        /// </summary>
        public void UpdateJson(string json)
        {
            try
            {
                Log($"[Update JSON] Starting with JSON: {json}");

                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    Log("[Update JSON] JSON is empty, using empty object");
                    json = "{}";
                }

                // Validate and normalize JSON
                var document = JsonDocument.Parse(json);
                json = JsonSerializer.Serialize(document, _jsonOptions);

                Log($"[Update JSON] Normalized JSON: {json}");

                // Update the current JSON in the root panel
                SetCurrentJson(json);

                // Only rebuild the entire UI if this is the root panel
                if (string.IsNullOrEmpty(_panelPath))
                {
                    var data = ConvertJsonToDictionary(json);

                    // Clear the current content
                    _contentContainer.Items.Clear();

                    // Clear the control cache
                    _controlCache.Clear();

                    // Clear the original types
                    OriginalTypes.Clear();

                    // Rebuild the UI with the new data
                    BuildFromDictionary(data, _contentContainer, _contentContainer.Orientation, "");

                    Log("[Update JSON] Root UI rebuilt successfully");
                }
                else
                {
                    // For nested panels, just refresh the relevant path
                    RefreshPath(_panelPath);

                    Log("[Update JSON] Nested UI refreshed successfully");
                }

                // Update control states based on app mode
                UpdateControlStates();

                Log("[Update JSON] UI updated successfully");
            }
            catch (JsonException ex)
            {
                Log($"[Update JSON] JSON error: {ex.Message}");
                MessageBox.Show(this, $"Invalid JSON: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
            }
            catch (Exception ex)
            {
                Log($"[Update JSON] General error: {ex.Message}");
                MessageBox.Show(this, $"Failed to update JSON: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
            }
        }

        /// <summary>
        /// Converts a JSON string to a dictionary using C#'s built-in System.Text.Json.
        /// </summary>
        private static IReadOnlyDictionary<string, JsonElement> ConvertJsonToDictionary(string json)
        {
            try
            {
                Log($"[Convert] Converting JSON: {json}");

                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    Log("[Convert] JSON is empty, using empty object");
                    json = "{}";
                }

                // Parse and validate JSON
                var document = JsonDocument.Parse(json);

                // If the root is not an object, wrap it in an object
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    Log("[Convert] Root is not an object, wrapping it");
                    json = $"{{\"value\": {json}}}";
                    document = JsonDocument.Parse(json);
                }

                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                Log($"[Convert] Converted successfully to dictionary with {result.Count} items");
                return result;
            }
            catch (JsonException ex)
            {
                Log($"[Convert] JSON error: {ex.Message}");
                // Return an empty dictionary if parsing fails
                return new Dictionary<string, JsonElement>();
            }
        }

        /// <summary>
        /// Recursively builds controls from each key/value pair in the dictionary.
        /// </summary>
        private void BuildFromDictionary(
            IReadOnlyDictionary<string, JsonElement> dict,
            StackLayout container,
            Orientation orientation,
            string path
        )
        {
            Log($"[Build] Building from dictionary with {dict.Count} items at path '{path}'");

            foreach (var kvp in dict)
            {
                string currentPath = string.IsNullOrEmpty(path) ? kvp.Key : $"{path}.{kvp.Key}";
                Log($"[Build] Processing property '{currentPath}' with ValueKind {kvp.Value.ValueKind}");

                // Create a label whose text is the translated key.
                var label = new Label
                {
                    Text = Translate(kvp.Key),
                    Tag = kvp.Key,
                    Font = Fonts.Monospace(fontSize),
                };

                // Create the control for the value.
                Control valueControl = BuildControlForValue(kvp.Value, orientation, currentPath);

                // Cache the control for later access
                _controlCache[currentPath] = valueControl;

                // For interactive controls, store its node path in the Tag.
                if (valueControl is TextBox || valueControl is CheckBox || valueControl is DateTimePicker)
                    valueControl.Tag = currentPath;

                // Record the original type.
                OriginalTypes[currentPath] = MapJsonElementToType(kvp.Value);

                // Create a "row" combining the label, value control, and action buttons.
                var row = new StackLayout { Orientation = Invert(orientation), Spacing = 5 };
                row.Items.Add(new StackLayoutItem(label));
                row.Items.Add(new StackLayoutItem(valueControl, HorizontalAlignment.Stretch, true));

                // Add action buttons for objects and arrays
                if (kvp.Value.ValueKind == JsonValueKind.Object || kvp.Value.ValueKind == JsonValueKind.Array)
                {
                    var addButton = new Button { Text = "+", Tag = currentPath, Width = 25 };
                    addButton.Click += (s, e) => AddElement(currentPath, kvp.Value.ValueKind);
                    row.Items.Add(new StackLayoutItem(addButton));
                }

                // Add delete button for all properties
                if (!string.IsNullOrEmpty(path)) // Don't allow deletion of root properties
                {
                    var deleteButton = new Button { Text = "×", Tag = currentPath, Width = 25 };
                    deleteButton.Click += (s, e) => DeleteElement(currentPath);
                    row.Items.Add(new StackLayoutItem(deleteButton));
                }

                // Wrap the row in a StackLayoutItem and add to the container.
                container.Items.Add(new StackLayoutItem(row, HorizontalAlignment.Stretch));
            }

            // Add a button to add new properties to objects (but not to root)
            if (!string.IsNullOrEmpty(path))
            {
                var addPropertyButton = new Button { Text = "Add Property", Tag = path };
                addPropertyButton.Click += (s, e) => AddNewProperty(path);
                container.Items.Add(new StackLayoutItem(addPropertyButton));
            }

            Log($"[Build] Built {container.Items.Count} items for path '{path}'");
        }

        /// <summary>
        /// Recursively builds controls for each element in a JSON array.
        /// </summary>
        /// <summary>
        /// Recursively builds controls for each element in a JSON array.
        /// </summary>
        private Control BuildFromList(
            IEnumerable<JsonElement> list,
            Orientation orientation,
            string path
        )
        {
            var container = new StackLayout { Orientation = orientation, Spacing = 5 };
            int index = 0;

            Log($"[Build List] Building list with {list.Count()} items at path '{path}'");

            foreach (var item in list)
            {
                // Capture the current index to avoid closure issues
                int currentIndex = index;
                string itemPath = $"{path}[{currentIndex}]";
                Log($"[Build List] Processing array item '{itemPath}' with ValueKind {item.ValueKind}");

                // Create a label for the index.
                var label = new Label { Text = $"[{currentIndex}]", Tag = currentIndex };

                Control itemControl = BuildControlForValue(item, orientation, itemPath);

                // Cache the control for later access
                _controlCache[itemPath] = itemControl;

                if (itemControl is TextBox || itemControl is CheckBox || itemControl is DateTimePicker)
                    itemControl.Tag = itemPath;

                OriginalTypes[itemPath] = MapJsonElementToType(item);

                var row = new StackLayout { Orientation = Invert(orientation), Spacing = 5 };
                row.Items.Add(new StackLayoutItem(label));
                row.Items.Add(new StackLayoutItem(itemControl, HorizontalAlignment.Stretch, true));

                // Add delete button for array items
                var deleteButton = new Button { Text = "×", Tag = itemPath, Width = 25 };
                // Use currentIndex instead of index to avoid closure issues
                deleteButton.Click += (s, e) => DeleteArrayElement(path, currentIndex);
                row.Items.Add(new StackLayoutItem(deleteButton));

                container.Items.Add(new StackLayoutItem(row, HorizontalAlignment.Stretch));
                index++;
            }

            // Add a button to add new items to the array
            var addButton = new Button { Text = "Add Item", Tag = path };
            addButton.Click += (s, e) => AddArrayItem(path);
            container.Items.Add(new StackLayoutItem(addButton));

            return container;
        }

        /// <summary>
        /// Returns the appropriate control for a given JSON element.
        /// Handles special cases for buttons, dates, and images.
        /// </summary>
        private Control BuildControlForValue(
            JsonElement value,
            Orientation orientation,
            string path
        )
        {
            Log($"[Build Control] Building control for path '{path}' with ValueKind {value.ValueKind}");

            switch (value.ValueKind)
            {
                case JsonValueKind.Null:
                    Log($"[Build Control] Creating null label for path '{path}'");
                    return new Label
                    {
                        Text = "null",
                        Font = Fonts.Monospace(fontSize),
                        TextColor = Colors.Gray
                    };

                case JsonValueKind.Object:
                    Log($"[Build Control] Creating nested panel for object at path '{path}'");
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value.GetRawText());
                    var nestedPanel = new FullJsonEditorPanel(dict, Invert(orientation), path, this.OriginalTypes, _showHeader, _showUploadButton);
                    // Set the root panel reference for the nested panel
                    nestedPanel._rootPanel = this._rootPanel;
                    return nestedPanel;

                case JsonValueKind.Array:
                    Log($"[Build Control] Creating list for array at path '{path}'");
                    return BuildFromList(value.EnumerateArray(), Invert(orientation), path);

                case JsonValueKind.True:
                case JsonValueKind.False:
                    Log($"[Build Control] Creating checkbox for boolean at path '{path}'");
                    var checkBox = new CheckBox
                    {
                        Checked = value.GetBoolean(),
                        Tag = path,
                        Font = Fonts.Monospace(fontSize),
                    };
                    // Handle checkbox changes
                    checkBox.CheckedChanged += (sender, e) => {
                        Log($"[Checkbox] Changed at path '{path}' to {checkBox.Checked}");
                        UpdateValueAtPath(path, checkBox.Checked.ToString());
                    };
                    return checkBox;

                case JsonValueKind.Number:
                    if (value.TryGetInt64(out long l))
                    {
                        Log($"[Build Control] Creating textbox for long value {l} at path '{path}'");
                        var numberTextBox = new TextBox
                        {
                            Text = l.ToString(),
                            Tag = path,
                            Font = Fonts.Monospace(fontSize),
                        };
                        // Handle text changes
                        numberTextBox.TextChanged += (sender, e) => {
                            Log($"[TextBox] Changed at path '{path}' to {numberTextBox.Text}");
                            UpdateValueAtPath(path, numberTextBox.Text);
                        };
                        return numberTextBox;
                    }
                    else
                    {
                        double d = value.GetDouble();
                        Log($"[Build Control] Creating textbox for double value {d} at path '{path}'");
                        var doubleTextBox = new TextBox
                        {
                            Text = d.ToString(),
                            Tag = path,
                            Font = Fonts.Monospace(fontSize),
                        };
                        // Handle text changes
                        doubleTextBox.TextChanged += (sender, e) => {
                            Log($"[TextBox] Changed at path '{path}' to {doubleTextBox.Text}");
                            UpdateValueAtPath(path, doubleTextBox.Text);
                        };
                        return doubleTextBox;
                    }

                case JsonValueKind.String:
                    string stringValue = value.GetString();
                    Log($"[Build Control] Processing string value '{stringValue}' at path '{path}'");

                    // Check for button syntax
                    if (stringValue.StartsWith("button:"))
                    {
                        var buttonText = stringValue.Substring(7); // Remove "button:" prefix
                        Log($"[Build Control] Creating button with text '{buttonText}' at path '{path}'");
                        var button = new Button { Text = buttonText, Tag = path };
                        button.Click += (sender, e) =>
                        {
                            if (AppMode)
                            {
                                Log($"[App Mode] Button '{buttonText}' clicked at path '{path}'");
                            }
                        };
                        return button;
                    }

                    // Check for date syntax or ISO8601 format
                    if (stringValue.StartsWith("date:") || IsISO8601Date(stringValue))
                    {
                        string dateValue = stringValue.StartsWith("date:") ? stringValue.Substring(5) : stringValue;
                        if (DateTime.TryParse(dateValue, out DateTime dateTime))
                        {
                            Log($"[Build Control] Creating date picker for date '{dateValue}' at path '{path}'");
                            var datePicker = new DateTimePicker
                            {
                                Value = dateTime,
                                Tag = path,
                                Font = Fonts.Monospace(fontSize),
                                Mode = DateTimePickerMode.DateTime
                            };

                            // In app mode, make the picker read-only but still clickable to log
                            if (AppMode)
                            {
                                datePicker.Enabled = false;
                                datePicker.MouseDoubleClick += (sender, e) =>
                                {
                                    Log($"[App Mode] Date '{dateValue}' clicked at path '{path}'");
                                };
                            }

                            // Handle date changes
                            datePicker.ValueChanged += (sender, e) => {
                                Log($"[DatePicker] Changed at path '{path}' to {datePicker.Value}");
                                UpdateValueAtPath(path, datePicker.Value?.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                            };
                            return datePicker;
                        }
                    }

                    // Check for images (base64, URLs, or file extensions)
                    if (IsBase64Image(stringValue, out ImageType imageType) ||
                        IsImageUrl(stringValue) ||
                        HasImageExtension(stringValue))
                    {
                        Log($"[Build Control] Detected potential image at path '{path}'");

                        // Create an upload/change button
                        var uploadButton = new Button { Text = "Upload/Change Image", Tag = path };
                        uploadButton.Click += (s, e) => UploadImageAsBase64(path);

                        // For base64 images, show the existing image
                        if (IsBase64Image(stringValue, out ImageType type) && type != ImageType.Unknown)
                        {
                            try
                            {
                                Log($"[Build Control] Creating image view for {type} at path '{path}'");
                                byte[] imageBytes = Convert.FromBase64String(stringValue);
                                var image = new Bitmap(imageBytes);
                                var imageView = new ImageView
                                {
                                    Image = image,
                                    Tag = path,
                                    Width = 200,
                                    Height = 150,
                                    //ScaleMode = ScaleMode.Fit
                                };

                                // In app mode, make the image clickable to log
                                if (AppMode)
                                {
                                    imageView.Cursor = Cursors.Pointer;
                                    imageView.MouseDown += (sender, e) =>
                                    {
                                        Log($"[App Mode] Image clicked at path '{path}'");
                                    };
                                }

                                // Create container with image and button
                                var containerWithImage = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };
                                containerWithImage.Items.Add(new StackLayoutItem(imageView, HorizontalAlignment.Center));
                                containerWithImage.Items.Add(new StackLayoutItem(uploadButton, HorizontalAlignment.Center));

                                return containerWithImage;
                            }
                            catch (Exception ex)
                            {
                                Log($"[Build Control] Error loading image at path '{path}': {ex.Message}");
                                // Fall back to showing text box with upload button
                            }
                        }

                        // Create container with text (for URLs) and upload button
                        var containerWithUpload = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };

                        // For URLs, show the URL as a clickable link
                        if (IsImageUrl(stringValue))
                        {
                            var urlLabel = new Label
                            {
                                Text = $"URL: {stringValue}",
                                Tag = path,
                                TextColor = Colors.Blue,
                                Font = Fonts.Monospace(fontSize),
                                Cursor = Cursors.Pointer,
                                Wrap = WrapMode.Word
                            };

                            urlLabel.MouseDoubleClick += (sender, e) => {
                                try
                                {
                                    Process.Start(new Uri(stringValue).ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log($"[Build Control] Error opening URL '{stringValue}': {ex.Message}");
                                }
                            };

                            containerWithUpload.Items.Add(new StackLayoutItem(urlLabel, HorizontalAlignment.Stretch, true));
                        }
                        else
                        {
                            // For base64 or files with extensions, show text box
                            var textBox = new TextBox
                            {
                                Text = IsImageUrl(stringValue) ? stringValue : "[Base64 Image Data]",
                                Tag = path,
                                Width = cWidth,
                                Font = Fonts.Monospace(fontSize),
                                ReadOnly = IsImageUrl(stringValue)
                            };

                            // Don't allow editing of image URLs in the textbox
                            if (!IsImageUrl(stringValue))
                            {
                                textBox.TextChanged += (sender, e) => {
                                    Log($"[TextBox] Changed at path '{path}' to {textBox.Text}");
                                    UpdateValueAtPath(path, textBox.Text);
                                };
                            }

                            containerWithUpload.Items.Add(new StackLayoutItem(textBox, HorizontalAlignment.Stretch, true));
                        }

                        containerWithUpload.Items.Add(new StackLayoutItem(uploadButton, HorizontalAlignment.Center));

                        return containerWithUpload;
                    }

                    // Default to a text box with upload button (if enabled)
                    Log($"[Build Control] Creating default textbox for string value at path '{path}'");
                    var defaultTextBox = new TextBox
                    {
                        Text = stringValue,
                        Tag = path,
                        Width = cWidth,
                        Font = Fonts.Monospace(fontSize),
                    };

                    // Handle text changes
                    defaultTextBox.TextChanged += (sender, e) => {
                        Log($"[TextBox] Changed at path '{path}' to {defaultTextBox.Text}");
                        UpdateValueAtPath(path, defaultTextBox.Text);
                    };

                    // Add upload button if it might be an image or if enabled
                    if (_showUploadButton &&
                        (stringValue.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                         stringValue.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                         stringValue.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                         stringValue.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                         stringValue.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                         IsBase64Image(stringValue, out _)))
                    {
                        var uploadButton = new Button { Text = "Upload Image", Tag = path };
                        uploadButton.Click += (s, e) => UploadImageAsBase64(path);

                        var containerWithUpload = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };
                        containerWithUpload.Items.Add(new StackLayoutItem(defaultTextBox, HorizontalAlignment.Stretch, true));
                        containerWithUpload.Items.Add(new StackLayoutItem(uploadButton));

                        return containerWithUpload;
                    }
                    else
                    {
                        // Just return the textbox without upload button
                        return defaultTextBox;
                    }

                default:
                    Log($"[Build Control] Creating fallback textbox for value '{value}' at path '{path}'");
                    var fallbackTextBox = new TextBox
                    {
                        Text = value.ToString(),
                        Tag = path,
                        Width = cWidth,
                        Font = Fonts.Monospace(fontSize),
                    };
                    return fallbackTextBox;
            }
        }

        /// <summary>
        /// Flips the orientation: Horizontal becomes Vertical and vice versa.
        /// </summary>
        private Orientation Invert(Orientation orientation)
        {
            return orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
        }

        /// <summary>
        /// Dummy translation method. Replace this with a proper localization if desired.
        /// </summary>
        private string Translate(string key)
        {
            return key;
        }

        /// <summary>
        /// Maps a JsonElement to its corresponding .NET type.
        /// </summary>
        private Type MapJsonElementToType(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    return element.TryGetInt64(out _) ? typeof(long) : typeof(double);
                case JsonValueKind.String:
                    return typeof(string);
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return typeof(bool);
                case JsonValueKind.Object:
                    return typeof(object);
                case JsonValueKind.Array:
                    return typeof(Array);
                case JsonValueKind.Null:
                    return null;
                default:
                    return typeof(object);
            }
        }

        /// <summary>
        /// Updates the enabled state of controls based on the AppMode setting.
        /// </summary>
        private void UpdateControlStates()
        {
            UpdateControlStatesRecursively(_contentContainer);
        }

        private void UpdateControlStatesRecursively(Control control)
        {
            if (control is TextBox textBox)
            {
                textBox.Enabled = !AppMode;
            }
            else if (control is CheckBox checkBox)
            {
                checkBox.Enabled = !AppMode;
            }
            else if (control is DateTimePicker datePicker)
            {
                if (AppMode)
                {
                    datePicker.Enabled = false;
                    // Already set up mouse double-click handler in BuildControlForValue
                }
                else
                {
                    datePicker.Enabled = true;
                }
            }
            else if (control is Button button)
            {
                // In app mode, only keep buttons that start with "button:" enabled
                if (AppMode && !(button.Text.StartsWith("button:") || button.Text == "Add Property" || button.Text == "Add Item"))
                {
                    button.Enabled = false;
                }
                else
                {
                    button.Enabled = true;
                }
            }
            else if (control is FullJsonEditorPanel editorPanel)
            {
                editorPanel.UpdateControlStates();
            }
            else if (control is Panel containerPanel && containerPanel.Content != null)
            {
                UpdateControlStatesRecursively(containerPanel.Content);
            }
            else if (control is StackLayout layout)
            {
                foreach (var item in layout.Items)
                {
                    if (item.Control != null)
                        UpdateControlStatesRecursively(item.Control);
                }
            }
        }

        /// <summary>
        /// Checks if a string is a valid ISO8601 date.
        /// </summary>
        private bool IsISO8601Date(string value)
        {
            // Basic ISO8601 pattern - matches dates like "2023-04-25T14:30:00Z" or "2023-04-25"
            return Regex.IsMatch(value, @"^\d{4}-\d{2}-\d{2}(T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})?)?$");
        }

        

        /// <summary>
        /// Opens a file dialog to select an image and converts it to base64.
        /// </summary>
        private void UploadImageAsBase64(string path)
        {
            Log($"[Upload Image] Starting image upload for path '{path}'");

            var fileDialog = new OpenFileDialog
            {
                Title = "Select an image",
                Filters =
        {
            new FileFilter("Image Files (*.png;*.jpg;*.jpeg;*.gif)", ".png", ".jpg", ".jpeg", ".gif")
        }
            };

            if (fileDialog.ShowDialog(this) == DialogResult.Ok)
            {
                try
                {
                    byte[] imageBytes = File.ReadAllBytes(fileDialog.FileName);
                    string base64 = Convert.ToBase64String(imageBytes);

                    // Detect image type from file extension
                    ImageType imageType = ImageType.Unknown;
                    string extension = Path.GetExtension(fileDialog.FileName).ToLowerInvariant();
                    if (extension == ".png")
                        imageType = ImageType.Png;
                    else if (extension == ".jpg" || extension == ".jpeg")
                        imageType = ImageType.Jpeg;

                    Log($"[Upload Image] Selected image: {fileDialog.FileName}, Type: {imageType}, Size: {imageBytes.Length} bytes");
                    Log($"[Upload Image] Updating image at path '{path}'");

                    // Update the JSON with the new image
                    UpdateValueAtPath(path, base64);
                    Log($"[Upload Image] Successfully updated image at path '{path}'");
                }
                catch (Exception ex)
                {
                    Log($"[Upload Image] Error: {ex.Message}");
                    MessageBox.Show(this, $"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                }
            }
            else
            {
                Log("[Upload Image] User cancelled image selection");
            }
        }

        /// <summary>
        /// Changes an existing image at the specified path.
        /// </summary>
        private void ChangeImage(string path)
        {
            Log($"[Change Image] Changing image at path '{path}'");
            UploadImageAsBase64(path);
        }

        /// <summary>
        /// Updates a value at a specific path in the JSON.
        /// </summary>
        private void UpdateValueAtPath(string path, string value)
        {
            try
            {
                Log($"[Update Value] Updating value at path '{path}' to '{value}'");
                var json = GetCurrentJson();
                var updatedJson = UpdateValueInJson(json, path, value);
                Log($"[Update Value] Updated JSON: {updatedJson}");

                // Update the current JSON in the root panel
                SetCurrentJson(updatedJson);

                // Refresh the UI for the specific path
                RefreshPath(path);

                Log("[Update Value] UI updated successfully");
            }
            catch (Exception ex)
            {
                Log($"[Update Value] Error: {ex.Message}");
                MessageBox.Show(this, $"Error updating value: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
            }
        }

        /// <summary>
        /// Updates a value in the JSON string at the specified path.
        /// </summary>
        private string UpdateValueInJson(string json, string path, string newValue)
        {
            try
            {
                Log($"[Update Value JSON] Updating value at path '{path}' to '{newValue}' in JSON: {json}");

                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    Log("[Update Value JSON] JSON is empty, using empty object");
                    json = "{}";
                }

                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Handle top-level property update
                if (string.IsNullOrEmpty(path))
                {
                    // This shouldn't happen in our use case
                    throw new ArgumentException("Cannot update root property without a name.");
                }

                // Split the path into parts
                var parts = path.Split('.');

                // Navigate to the parent of the target
                var parentPath = string.Join(".", parts.Take(parts.Length - 1));
                var propertyName = parts.Last();

                Log($"[Update Value JSON] Parent path: '{parentPath}', Property name: '{propertyName}'");

                // Handle array indices
                if (propertyName.Contains("[") && propertyName.Contains("]"))
                {
                    var match = Regex.Match(propertyName, @"(.+)\[(\d+)\]");
                    if (match.Success)
                    {
                        propertyName = match.Groups[1].Value;
                        var index = int.Parse(match.Groups[2].Value);

                        Log($"[Update Value JSON] Array update - Property: '{propertyName}', Index: {index}");

                        // Navigate to the array
                        var arrayElement = NavigateToPath(root, parentPath);
                        if (arrayElement.ValueKind != JsonValueKind.Array)
                            throw new InvalidOperationException($"Path '{parentPath}' does not point to an array.");

                        // Convert to a mutable representation
                        var list = JsonSerializer.Deserialize<List<object>>(arrayElement.GetRawText());

                        // Update the item at the specified index
                        if (index >= 0 && index < list.Count)
                        {
                            // Determine the type of the existing item
                            var existingItem = list[index];
                            var updatedItem = CreateUpdatedValue(existingItem, newValue);
                            list[index] = updatedItem;

                            // Update the array in the document
                            var result = UpdatePathInJson(json, parentPath, list);
                            Log($"[Update Value JSON] Array update result: {result}");
                            return result;
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException($"Index {index} is out of range for array with {list.Count} items.");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid array index format: {propertyName}");
                    }
                }

                // Navigate to the parent object
                var parentElement = string.IsNullOrEmpty(parentPath) ? root : NavigateToPath(root, parentPath);

                if (parentElement.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException($"Path '{parentPath}' does not point to an object.");

                // Convert to a mutable representation
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(parentElement.GetRawText());

                Log($"[Update Value JSON] Parent object has {dict.Count} properties");

                // Update the property
                if (dict.ContainsKey(propertyName))
                {
                    var existingValue = dict[propertyName];
                    var updatedValue = CreateUpdatedValue(existingValue, newValue);
                    dict[propertyName] = updatedValue;

                    Log($"[Update Value JSON] Updated property '{propertyName}' from '{existingValue}' to '{updatedValue}'");
                }
                else
                {
                    throw new InvalidOperationException($"Property '{propertyName}' not found.");
                }

                // Update the parent in the document
                var resultJson = UpdatePathInJson(json, parentPath, dict);
                Log($"[Update Value JSON] Final result: {resultJson}");
                return resultJson;
            }
            catch (JsonException ex)
            {
                Log($"[Update Value JSON] JSON error: {ex.Message}");
                throw new Exception($"Invalid JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Log($"[Update Value JSON] General error: {ex.Message}");
                throw new Exception($"Failed to update value at path '{path}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates an updated value based on the existing value type and new string value.
        /// </summary>
        private object CreateUpdatedValue(object existingValue, string newValue)
        {
            if (existingValue == null)
                return null;

            var existingType = existingValue.GetType();

            try
            {
                if (existingType == typeof(string))
                    return newValue;
                else if (existingType == typeof(bool))
                    return bool.Parse(newValue);
                else if (existingType == typeof(long))
                    return long.Parse(newValue);
                else if (existingType == typeof(double))
                    return double.Parse(newValue);
                else
                    return newValue; // Default to string for complex types
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to convert value '{newValue}' to type {existingType.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates a value based on its type.
        /// </summary>
        private (bool isValid, string errorMessage) ValidateValueByType(string value, string type)
        {
            switch (type)
            {
                case "String":
                    return (true, ""); // Strings are always valid

                case "Number":
                    if (double.TryParse(value, out double _))
                        return (true, "");
                    else
                        return (false, $"'{value}' is not a valid number.");

                case "Boolean":
                    if (bool.TryParse(value, out bool _))
                        return (true, "");
                    else
                        return (false, $"'{value}' is not a valid boolean. Use 'true' or 'false'.");

                case "Date":
                    if (IsISO8601Date(value))
                        return (true, "");
                    else
                        return (false, $"'{value}' is not a valid ISO8601 date format.");

                case "Image":
                    if (IsBase64Image(value, out _))
                        return (true, "");
                    else
                        return (false, $"'{value}' is not a valid base64 encoded image.");

                case "Object":
                case "Array":
                    return (true, ""); // Objects and arrays are handled separately

                default:
                    return (false, $"Unknown type: {type}");
            }
        }

        /// <summary>
        /// Checks if a string is a valid image URL (for common image services)
        /// </summary>
        private bool IsImageUrl(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 8)
                return false;

            // Must start with http:// or https://
            if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                var uri = new Uri(value);

                // Must be HTTP or HTTPS
                if (uri.Scheme != "http" && uri.Scheme != "https")
                    return false;

                // Remove protocol and any query parameters for pattern matching
                string cleanedUrl = uri.AbsoluteUri.Substring(uri.Scheme.Length + 3); // Remove "://" or "://"

                // Remove any query parameters or fragments
                int queryIndex = cleanedUrl.IndexOf('?');
                if (queryIndex >= 0)
                    cleanedUrl = cleanedUrl.Substring(0, queryIndex);

                int fragmentIndex = cleanedUrl.IndexOf('#');
                if (fragmentIndex >= 0)
                    cleanedUrl = cleanedUrl.Substring(0, fragmentIndex);

                // Check for specific image URL patterns - much more strict now
                return Regex.IsMatch(cleanedUrl,
                    @"^(?:" +
                    @"i\.imgur\.com/|" +                    // Imgur
                    @"images\.(?:unsplash|pixabay|pexels)\.com/|" + // Unsplash, Pixabay, Pexels
                    @"media\.giphy\.com/|" +                  // Giphy
                    @"(?:img|image|photo)\." +                // Common image subdomains
                    @")" +
                    @"(?:" +
                    @"/[^/]+\.(?:jpg|jpeg|png|gif|webp|bmp|svg)(?:/)?$|" +  // Ends with image extension
                    @"/[^/]+/[^/]+/\d+$|" +                // Imgur pattern (username/image/id)
                    @"/media/[^/]+\.(?:jpg|jpeg|png|gif|webp|bmp|svg)$|" + // Media folder with extension
                    @")",
                    RegexOptions.IgnoreCase);
            }
            catch (UriFormatException)
            {
                // Not a valid URI
                return false;
            }
            catch (Exception ex)
            {
                Log($"[IsImageUrl] Error checking URL '{value}': {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// Checks if a string has a common image file extension
        /// </summary>
        private bool HasImageExtension(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            string lowerValue = value.ToLowerInvariant();
            return lowerValue.EndsWith(".jpg") ||
                   lowerValue.EndsWith(".jpeg") ||
                   lowerValue.EndsWith(".png") ||
                   lowerValue.EndsWith(".gif") ||
                   lowerValue.EndsWith(".webp") ||
                   lowerValue.EndsWith(".bmp") ||
                   lowerValue.EndsWith(".svg");
        }

        /// <summary>
        /// Checks if a string is a valid base64 encoded image.
        /// </summary>
        private bool IsBase64Image(string value, out ImageType imageType)
        {
            imageType = ImageType.Unknown;

            if (string.IsNullOrEmpty(value) || value.Length < 100) // Base64 images are usually longer
                return false;

            // Check if it's a valid base64 string
            if (value.Length % 4 != 0 || !Regex.IsMatch(value, @"^[a-zA-Z0-9\+/]*={0,3}$"))
                return false;

            try
            {
                byte[] bytes = Convert.FromBase64String(value);

                // Check for image signatures
                if (bytes.Length < 8) // Need at least 8 bytes for reliable detection
                    return false;

                // JPEG signature: FF D8 FF
                if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                {
                    imageType = ImageType.Jpeg;
                    return true;
                }

                // PNG signature: 89 50 4E 47
                if (bytes.Length >= 8 &&
                    bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                {
                    imageType = ImageType.Png;
                    return true;
                }

                // GIF signature: GIF87a or GIF89a
                if (bytes.Length >= 6 &&
                    bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
                {
                    if ((bytes[3] == 0x38 && bytes[4] == 0x37 && bytes[5] == 0x61) || // GIF87a
                        (bytes[3] == 0x38 && bytes[4] == 0x39 && bytes[5] == 0x61)) // GIF89a
                    {
                        return true;
                    }
                }

                // BMP signature: BM
                if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D)
                {
                    return true;
                }

                // WebP signature
                if (bytes.Length >= 12 &&
                    bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Adds a new element to an object or array.
        /// </summary>
        private void AddElement(string path, JsonValueKind valueKind)
        {
            if (valueKind == JsonValueKind.Object)
            {
                AddNewProperty(path);
            }
            else if (valueKind == JsonValueKind.Array)
            {
                AddArrayItem(path);
            }
        }

        /// <summary>
        /// Adds a new property to an object.
        /// </summary>
        private void AddNewProperty(string parentPath)
        {
            Log($"[Add Property] Adding property to path '{parentPath}'");

            // Create a dialog first
            var dialog = new Dialog<string>
            {
                Title = "Add New Property",
                ClientSize = new Size(300, 200)
            };

            var propertyNameTextBox = new TextBox { ID = "propertyName" };
            var propertyTypeDropDown = new DropDown { ID = "propertyType", Items = { "String", "Number", "Boolean", "Object", "Array", "Date", "Image" } };
            var propertyValueTextBox = new TextBox { ID = "propertyValue" };
            var validationLabel = new Label { Text = "", TextColor = Colors.Red };

            var okButton = new Button { Text = "OK" };
            var cancelButton = new Button { Text = "Cancel" };

            // Create the layout
            var layout = new StackLayout
            {
                Padding = 10,
                Spacing = 5,
                Items =
                {
                    new Label { Text = "Property Name:" },
                    new StackLayoutItem(propertyNameTextBox, HorizontalAlignment.Stretch),
                    new Label { Text = "Property Type:" },
                    new StackLayoutItem(propertyTypeDropDown, HorizontalAlignment.Stretch),
                    new Label { Text = "Property Value:" },
                    new StackLayoutItem(propertyValueTextBox, HorizontalAlignment.Stretch),
                    new StackLayoutItem(validationLabel, HorizontalAlignment.Stretch)
                }
            };

            var buttonPanel = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Items =
                {
                    new StackLayoutItem(okButton),
                    new StackLayoutItem(cancelButton)
                }
            };

            layout.Items.Add(new StackLayoutItem(buttonPanel, HorizontalAlignment.Right));

            dialog.Content = layout;

            // Set up event handlers
            propertyTypeDropDown.SelectedIndexChanged += (sender, e) => {
                // Update placeholder text based on type
                switch (propertyTypeDropDown.SelectedKey)
                {
                    case "String":
                        propertyValueTextBox.PlaceholderText = "Enter a string value";
                        propertyValueTextBox.Enabled = true;
                        break;
                    case "Number":
                        propertyValueTextBox.PlaceholderText = "Enter a numeric value (e.g., 42 or 3.14)";
                        propertyValueTextBox.Enabled = true;
                        break;
                    case "Boolean":
                        propertyValueTextBox.PlaceholderText = "Enter 'true' or 'false'";
                        propertyValueTextBox.Enabled = true;
                        break;
                    case "Date":
                        propertyValueTextBox.PlaceholderText = "Enter an ISO8601 date (e.g., 2023-04-25T14:30:00Z)";
                        propertyValueTextBox.Enabled = true;
                        break;
                    case "Image":
                        propertyValueTextBox.PlaceholderText = "Enter a base64 encoded image";
                        propertyValueTextBox.Enabled = true;
                        break;
                    case "Object":
                        propertyValueTextBox.PlaceholderText = "Creating empty object";
                        propertyValueTextBox.Enabled = false;
                        break;
                    case "Array":
                        propertyValueTextBox.PlaceholderText = "Creating empty array";
                        propertyValueTextBox.Enabled = false;
                        break;
                }

                // Clear validation when type changes
                validationLabel.Text = "";
            };

            // Now attach event handlers
            okButton.Click += (sender, e) => {
                var name = propertyNameTextBox.Text;
                var type = propertyTypeDropDown.SelectedKey;
                var value = propertyValueTextBox.Text;

                Log($"[Add Property] Dialog result - Name: '{name}', Type: '{type}', Value: '{value}'");

                // Validate input
                var (isValid, errorMessage) = ValidateValueByType(value, type);

                if (string.IsNullOrEmpty(name))
                {
                    validationLabel.Text = "Property name cannot be empty.";
                }
                else if (!isValid)
                {
                    validationLabel.Text = errorMessage;
                }
                else
                {
                    dialog.Close($"{name}|{type}|{value}");
                }
            };

            cancelButton.Click += (sender, e) => {
                dialog.Close(null);
            };

            // Initialize the placeholder text
            propertyTypeDropDown.SelectedIndex = 0; // Select "String" by default

            // Show the dialog
            var result = dialog.ShowModal(this);
            if (!string.IsNullOrEmpty(result))
            {
                var parts = result.Split('|');
                var name = parts[0];
                var type = parts[1];
                var value = parts[2];

                Log($"[Add Property] Adding property '{name}' of type '{type}' with value '{value}' to path '{parentPath}'");

                // Update the JSON with the new property
                try
                {
                    var json = GetCurrentJson();
                    Log($"[Add Property] Current JSON: {json}");
                    var updatedJson = AddPropertyToJson(json, parentPath, name, type, value);
                    Log($"[Add Property] Updated JSON: {updatedJson}");

                    // Update the current JSON in the root panel
                    SetCurrentJson(updatedJson);

                    // Refresh the UI for the specific path
                    RefreshPath(parentPath);

                    Log("[Add Property] Property added successfully");
                }
                catch (Exception ex)
                {
                    Log($"[Add Property] Error: {ex.Message}");
                    MessageBox.Show(this, $"Error adding property: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                }
            }
        }

        /// <summary>
        /// Adds a new item to an array.
        /// </summary>
        private void AddArrayItem(string arrayPath)
        {
            Log($"[Add Array Item] Adding item to array at path '{arrayPath}'");

            // Create a dialog first
            var dialog = new Dialog<string>
            {
                Title = "Add New Array Item",
                ClientSize = new Size(300, 200)
            };

            var itemTypeDropDown = new DropDown { ID = "itemType", Items = { "String", "Number", "Boolean", "Object", "Array", "Date", "Image" } };
            var itemValueTextBox = new TextBox { ID = "itemValue" };
            var validationLabel = new Label { Text = "", TextColor = Colors.Red };

            var okButton = new Button { Text = "OK" };
            var cancelButton = new Button { Text = "Cancel" };

            // Create the layout
            var layout = new StackLayout
            {
                Padding = 10,
                Spacing = 5,
                Items =
                {
                    new Label { Text = "Item Type:" },
                    new StackLayoutItem(itemTypeDropDown, HorizontalAlignment.Stretch),
                    new Label { Text = "Item Value:" },
                    new StackLayoutItem(itemValueTextBox, HorizontalAlignment.Stretch),
                    new StackLayoutItem(validationLabel, HorizontalAlignment.Stretch)
                }
            };

            var buttonPanel = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Items =
                {
                    new StackLayoutItem(okButton),
                    new StackLayoutItem(cancelButton)
                }
            };

            layout.Items.Add(new StackLayoutItem(buttonPanel, HorizontalAlignment.Right));

            dialog.Content = layout;

            // Set up event handlers
            itemTypeDropDown.SelectedIndexChanged += (sender, e) => {
                // Update placeholder text based on type
                switch (itemTypeDropDown.SelectedKey)
                {
                    case "String":
                        itemValueTextBox.PlaceholderText = "Enter a string value";
                        itemValueTextBox.Enabled = true;
                        break;
                    case "Number":
                        itemValueTextBox.PlaceholderText = "Enter a numeric value (e.g., 42 or 3.14)";
                        itemValueTextBox.Enabled = true;
                        break;
                    case "Boolean":
                        itemValueTextBox.PlaceholderText = "Enter 'true' or 'false'";
                        itemValueTextBox.Enabled = true;
                        break;
                    case "Date":
                        itemValueTextBox.PlaceholderText = "Enter an ISO8601 date (e.g., 2023-04-25T14:30:00Z)";
                        itemValueTextBox.Enabled = true;
                        break;
                    case "Image":
                        itemValueTextBox.PlaceholderText = "Enter a base64 encoded image";
                        itemValueTextBox.Enabled = true;
                        break;
                    case "Object":
                        itemValueTextBox.PlaceholderText = "Creating empty object";
                        itemValueTextBox.Enabled = false;
                        break;
                    case "Array":
                        itemValueTextBox.PlaceholderText = "Creating empty array";
                        itemValueTextBox.Enabled = false;
                        break;
                }

                // Clear validation when type changes
                validationLabel.Text = "";
            };

            // Now attach event handlers
            okButton.Click += (sender, e) => {
                var type = itemTypeDropDown.SelectedKey;
                var value = itemValueTextBox.Text;

                Log($"[Add Array Item] Dialog result - Type: '{type}', Value: '{value}'");

                // Validate input
                var (isValid, errorMessage) = ValidateValueByType(value, type);

                if (!isValid)
                {
                    validationLabel.Text = errorMessage;
                }
                else
                {
                    dialog.Close($"{type}|{value}");
                }
            };

            cancelButton.Click += (sender, e) => {
                dialog.Close(null);
            };

            // Initialize the placeholder text
            itemTypeDropDown.SelectedIndex = 0; // Select "String" by default

            // Show the dialog
            var result = dialog.ShowModal(this);
            if (!string.IsNullOrEmpty(result))
            {
                var parts = result.Split('|');
                var type = parts[0];
                var value = parts[1];

                Log($"[Add Array Item] Adding item of type '{type}' with value '{value}' to array at path '{arrayPath}'");

                // Update the JSON with the new array item
                try
                {
                    var json = GetCurrentJson();
                    Log($"[Add Array Item] Current JSON: {json}");
                    var updatedJson = AddItemToArrayJson(json, arrayPath, type, value);
                    Log($"[Add Array Item] Updated JSON: {updatedJson}");

                    // Update the current JSON in the root panel
                    SetCurrentJson(updatedJson);

                    // Refresh the UI for the specific path
                    RefreshPath(arrayPath);

                    Log("[Add Array Item] Item added successfully");
                }
                catch (Exception ex)
                {
                    Log($"[Add Array Item] Error: {ex.Message}");
                    MessageBox.Show(this, $"Error adding array item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                }
            }
        }

        /// <summary>
        /// Deletes an element at the specified path.
        /// </summary>
        private void DeleteElement(string path)
        {
            Log($"[Delete Element] Deleting element at path '{path}'");

            var result = MessageBox.Show(this, $"Are you sure you want to delete '{path}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxType.Question);
            if (result == DialogResult.Yes)
            {
                try
                {
                    // Get the freshest JSON by building it from the UI controls
                    var json = BuildJsonFromRoot();
                    Log($"[Delete Element] Current JSON from UI: {json}");

                    if (string.IsNullOrWhiteSpace(json) || json == "{}")
                    {
                        Log("[Delete Element] JSON is empty, cannot delete property");
                        MessageBox.Show(this, "Cannot delete property from empty JSON.", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                        return;
                    }

                    // Check if this is an array path
                    if (path.Contains("[") && path.Contains("]"))
                    {
                        // Extract array path and index
                        var match = Regex.Match(path, @"(.+)\[(\d+)\]");
                        if (match.Success)
                        {
                            var arrayPath = match.Groups[1].Value;
                            var index = int.Parse(match.Groups[2].Value);
                            var updatedJson = DeleteItemFromArrayJson(json, arrayPath, index);
                            Log($"[Delete Element] Updated JSON: {updatedJson}");
                            SetCurrentJson(updatedJson);
                            RefreshPath(arrayPath);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Invalid array path format: {path}");
                        }
                    }
                    else
                    {
                        // Delete object property
                        var updatedJson = DeletePropertyFromJson(json, path);
                        Log($"[Delete Element] Updated JSON: {updatedJson}");
                        SetCurrentJson(updatedJson);

                        // Get parent path for refresh
                        var lastDot = path.LastIndexOf('.');
                        var parentPath = lastDot >= 0 ? path.Substring(0, lastDot) : "";
                        RefreshPath(parentPath);
                    }

                    Log("[Delete Element] Element deleted successfully");
                }
                catch (Exception ex)
                {
                    Log($"[Delete Element] Error: {ex.Message}");
                    MessageBox.Show(this, $"Error deleting property: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                }
            }
        }

        /// <summary>
        /// Builds JSON from the root panel controls.
        /// </summary>
        private string BuildJsonFromRoot()
        {
            try
            {
                var rootJson = new Dictionary<string, object>();

                // Build from the root content container
                BuildJsonFromControls(_contentContainer, rootJson);

                var json = JsonSerializer.Serialize(rootJson, _jsonOptions);
                Log($"[BuildJsonFromRoot] Built JSON: {json}");
                return json;
            }
            catch (Exception ex)
            {
                Log($"[BuildJsonFromRoot] Error: {ex.Message}");
                // Fallback to stored JSON
                return _currentJson ?? "{}";
            }
        }

        /// <summary>
        /// Deletes an array item at the specified path and index.
        /// </summary>
        private void DeleteArrayElement(string arrayPath, int index)
        {
            Log($"[Delete Array Item] Deleting item at index {index} in array at path '{arrayPath}'");

            // Parse the complex array path
            var (basePath, indices) = ParseComplexArrayPath(arrayPath);
            Log($"[Delete Array Item] Base path: '{basePath}', Indices: [{string.Join(", ", indices)}]");

            var message = indices.Count > 1
                ? $"Are you sure you want to delete item at index {index} in nested array at path '{arrayPath}'?"
                : $"Are you sure you want to delete item at index {index} in array at path '{arrayPath}'?";

            var result = MessageBox.Show(this, message, "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxType.Question);
            if (result == DialogResult.Yes)
            {
                try
                {
                    var json = GetCurrentJson();
                    Log($"[Delete Array Item] Current JSON length: {json.Length}");

                    // For nested arrays, we need special handling
                    if (indices.Count > 1)
                    {
                        Log("[Delete Array Item] Handling nested array deletion");
                        // TODO: Implement nested array deletion for multiple levels
                        MessageBox.Show(this, "Nested array deletion is not yet fully implemented.", "Not Implemented", MessageBoxButtons.OK, MessageBoxType.Information);
                        return;
                    }

                    var updatedJson = DeleteItemFromArrayJson(json, basePath, index);
                    Log($"[Delete Array Item] Updated JSON length: {updatedJson.Length}");
                    SetCurrentJson(updatedJson);

                    // Refresh the UI for the specific path
                    RefreshPath(basePath);

                    Log("[Delete Array Item] Item deleted successfully");
                }
                catch (Exception ex)
                {
                    Log($"[Delete Array Item] Error: {ex.Message}");
                    MessageBox.Show(this, $"Error deleting array item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                }
            }
        }

        /// <summary>
        /// Adds a property to the JSON string.
        /// </summary>
        private string AddPropertyToJson(string json, string parentPath, string name, string type, string value)
        {
            try
            {
                Log($"[Add Property JSON] Adding property '{name}' of type '{type}' with value '{value}' to path '{parentPath}' in JSON: {json}");

                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    Log("[Add Property JSON] JSON is empty, using empty object");
                    json = "{}";
                }

                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Navigate to the parent object
                var parentElement = string.IsNullOrEmpty(parentPath) ? root : NavigateToPath(root, parentPath);

                if (parentElement.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException($"Path '{parentPath}' does not point to an object.");

                // Create a new property value based on type
                object newValue = CreateTypedValue(type, value);

                // Convert to a mutable representation
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(parentElement.GetRawText());

                // Add the new property
                dict[name] = newValue;

                // Update the parent in the document
                var resultJson = UpdatePathInJson(json, parentPath, dict);
                Log($"[Add Property JSON] Result: {resultJson}");
                return resultJson;
            }
            catch (JsonException ex)
            {
                Log($"[Add Property JSON] JSON error: {ex.Message}");
                throw new Exception($"Invalid JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Log($"[Add Property JSON] General error: {ex.Message}");
                throw new Exception($"Failed to add property: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Adds an item to an array in the JSON string.
        /// </summary>
        private string AddItemToArrayJson(string json, string arrayPath, string type, string value)
        {
            try
            {
                Log($"[Add Item Array JSON] Starting - Adding item of type '{type}' with value '{value}' to array at path '{arrayPath}'");

                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    Log("[Add Item Array JSON] JSON is empty, using empty object");
                    json = "{}";
                }

                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Navigate to the array
                var arrayElement = NavigateToPath(root, arrayPath);

                if (arrayElement.ValueKind != JsonValueKind.Array)
                    throw new InvalidOperationException($"Path '{arrayPath}' does not point to an array.");

                // Create a new item value based on type
                object newValue = CreateTypedValue(type, value);

                // Convert to a mutable representation
                var list = JsonSerializer.Deserialize<List<object>>(arrayElement.GetRawText());

                // Add the new item
                list.Add(newValue);

                // Update the array in the document using a direct replacement approach
                var resultJson = UpdateArrayInJson(json, arrayPath, list);
                Log($"[Add Item Array JSON] Result: {resultJson}");
                return resultJson;
            }
            catch (JsonException ex)
            {
                Log($"[Add Item Array JSON] JSON error: {ex.Message}");
                throw new Exception($"Invalid JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Log($"[Add Item Array JSON] General error: {ex.Message}");
                throw new Exception($"Failed to add array item: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// Deletes a property from the JSON string.
        /// </summary>
        private string DeletePropertyFromJson(string json, string path)
        {
            try
            {
                Log($"[Delete Property JSON] Deleting property at path '{path}' in JSON: {json}");

                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    Log("[Delete Property JSON] JSON is empty, using empty object");
                    json = "{}";
                }

                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Split the path into parent and property name
                var lastDot = path.LastIndexOf('.');
                string parentPath = lastDot >= 0 ? path.Substring(0, lastDot) : "";
                string propertyName = lastDot >= 0 ? path.Substring(lastDot + 1) : path;

                Log($"[Delete Property JSON] Parent path: '{parentPath}', Property name: '{propertyName}'");

                // Navigate to the parent object
                var parentElement = string.IsNullOrEmpty(parentPath) ? root : NavigateToPath(root, parentPath);

                if (parentElement.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException($"Path '{parentPath}' does not point to an object.");

                // Convert to a mutable representation
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(parentElement.GetRawText());

                Log($"[Delete Property JSON] Parent object has {dict.Count} properties");

                // Remove the property
                bool removed = dict.Remove(propertyName);
                Log($"[Delete Property JSON] Property '{propertyName}' removal {(removed ? "successful" : "failed")}");

                // Update the parent in the document
                var resultJson = UpdatePathInJson(json, parentPath, dict);
                Log($"[Delete Property JSON] Result: {resultJson}");
                return resultJson;
            }
            catch (JsonException ex)
            {
                Log($"[Delete Property JSON] JSON error: {ex.Message}");
                throw new Exception($"Invalid JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Log($"[Delete Property JSON] General error: {ex.Message}");
                throw new Exception($"Failed to delete property: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes an item from an array in the JSON string.
        /// </summary>
        private string DeleteItemFromArrayJson(string json, string arrayPath, int index)
        {
            try
            {
                Log($"[Delete Item Array JSON] Starting - Deleting item at index {index} in array at path '{arrayPath}'");

                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    Log("[Delete Item Array JSON] JSON is empty, using empty object");
                    json = "{}";
                }

                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Navigate to the array
                var arrayElement = NavigateToPath(root, arrayPath);

                if (arrayElement.ValueKind != JsonValueKind.Array)
                    throw new InvalidOperationException($"Path '{arrayPath}' does not point to an array.");

                // Convert to a mutable representation
                var list = JsonSerializer.Deserialize<List<object>>(arrayElement.GetRawText());

                Log($"[Delete Item Array JSON] Array has {list.Count} items before deletion");
                Log($"[Delete Item Array JSON] Item at index {index} is: {list[index]} (Type: {list[index]?.GetType().Name})");

                // Check if the item at the specified index is itself an array
                if (index >= 0 && index < list.Count && list[index] is List<object> nestedArray)
                {
                    Log($"[Delete Item Array JSON] Item at index {index} is a nested array with {nestedArray.Count} items");

                    // This is a nested array - ask user for confirmation
                    var nestedResult = MessageBox.Show(
                        this,
                        $"This is a nested array. Do you want to delete the entire nested array at index {index}?",
                        "Confirm Delete Nested Array",
                        MessageBoxButtons.YesNo,
                        MessageBoxType.Question
                    );

                    if (nestedResult == DialogResult.Yes)
                    {
                        list.RemoveAt(index);
                        Log($"[Delete Item Array JSON] Removed nested array at index {index}");
                    }
                    else
                    {
                        Log("[Delete Item Array JSON] User cancelled deletion of nested array");
                        return json; // Return original JSON unchanged
                    }
                }
                else
                {
                    // Regular array item deletion
                    if (index >= 0 && index < list.Count)
                    {
                        list.RemoveAt(index);
                        Log($"[Delete Item Array JSON] Removed regular item at index {index}");
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException($"Index {index} is out of range for array with {list.Count} items.");
                    }
                }

                Log($"[Delete Item Array JSON] Array has {list.Count} items after deletion");

                // Update the array in the document using a direct replacement approach
                var resultJson = UpdateArrayInJson(json, arrayPath, list);
                Log($"[Delete Item Array JSON] Result: {resultJson}");
                return resultJson;
            }
            catch (JsonException ex)
            {
                Log($"[Delete Item Array JSON] JSON error: {ex.Message}");
                throw new Exception($"Invalid JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Log($"[Delete Item Array JSON] General error: {ex.Message}");
                throw new Exception($"Failed to delete array item: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses a complex array path that might contain nested array indices
        /// </summary>
        private (string arrayPath, List<int> indices) ParseComplexArrayPath(string path)
        {
            var indices = new List<int>();
            string remainingPath = path;

            // Extract all array indices from the path
            while (remainingPath.Contains("[") && remainingPath.Contains("]"))
            {
                var match = Regex.Match(remainingPath, @"(.+)\[(\d+)\]$");
                if (match.Success)
                {
                    indices.Insert(0, int.Parse(match.Groups[2].Value)); // Insert at beginning to maintain order
                    remainingPath = match.Groups[1].Value;
                }
                else
                {
                    break;
                }
            }

            return (remainingPath, indices);
        }

        /// <summary>
        /// Navigates to a specific path in the JSON document.
        /// </summary>
        private JsonElement NavigateToPath(JsonElement element, string path)
        {
            Log($"[Navigate] Navigating to path '{path}' in element with ValueKind {element.ValueKind}");

            if (string.IsNullOrEmpty(path))
            {
                Log("[Navigate] Path is empty, returning root element");
                return element;
            }

            var parts = path.Split('.');
            var current = element;

            Log($"[Navigate] Path has {parts.Length} parts: [{string.Join(", ", parts)}]");
            Log($"[Navigate] Root element ValueKind: {current.ValueKind}");
            Log($"[Navigate] Root element has {(current.ValueKind == JsonValueKind.Object ? current.EnumerateObject().Count() : 0)} properties");

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                Log($"[Navigate] Processing part {i}: '{part}'");
                Log($"[Navigate] Current element ValueKind before processing: {current.ValueKind}");

                // Handle array indices
                if (part.Contains("[") && part.Contains("]"))
                {
                    var match = Regex.Match(part, @"(.+)\[(\d+)\]");
                    if (match.Success)
                    {
                        var arrayName = match.Groups[1].Value;
                        var arrayIndex = int.Parse(match.Groups[2].Value);

                        Log($"[Navigate] Array navigation - Name: '{arrayName}', Index: {arrayIndex}");

                        // Check if the current element is an array
                        if (current.ValueKind == JsonValueKind.Array)
                        {
                            // Get the array items
                            var arrayItems = new List<JsonElement>();
                            foreach (var item in current.EnumerateArray())
                            {
                                arrayItems.Add(item);
                            }

                            Log($"[Navigate] Array has {arrayItems.Count} items");

                            // Check if the index is valid
                            if (arrayIndex >= 0 && arrayIndex < arrayItems.Count)
                            {
                                current = arrayItems[arrayIndex];
                                Log($"[Navigate] Moved to array item at index {arrayIndex}, ValueKind: {current.ValueKind}");
                            }
                            else
                            {
                                throw new ArgumentOutOfRangeException($"Array index {arrayIndex} is out of range for array with {arrayItems.Count} items.");
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Cannot access array index {arrayIndex} because current element is not an array (ValueKind: {current.ValueKind}).");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid array index format: {part}");
                    }
                }
                else
                {
                    // Handle object properties
                    if (current.ValueKind != JsonValueKind.Object)
                    {
                        Log($"[Navigate] Cannot navigate to '{part}' because current element is not an object (ValueKind: {current.ValueKind})");
                        throw new InvalidOperationException($"Cannot navigate to '{part}' because current element is not an object.");
                    }

                    if (!current.TryGetProperty(part, out var next))
                    {
                        Log($"[Navigate] Property '{part}' not found in current object");

                        // List available properties for debugging
                        var availableProps = new List<string>();
                        foreach (var prop in current.EnumerateObject())
                        {
                            availableProps.Add(prop.Name);
                        }
                        Log($"[Navigate] Available properties: [{string.Join(", ", availableProps)}]");

                        throw new InvalidOperationException($"Property '{part}' not found.");
                    }

                    current = next;
                    Log($"[Navigate] Moved to property '{part}' with ValueKind {current.ValueKind}");
                }
            }

            Log($"[Navigate] Navigation successful, final element ValueKind: {current.ValueKind}");
            return current;
        }

        /// <summary>
        /// Updates a specific path in the JSON document with a new value.
        /// </summary>
        private string UpdatePathInJson(string json, string path, object newValue)
        {
            try
            {
                Log($"[Update Path] Starting - Updating path '{path}' with value '{newValue}'");

                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    Log("[Update Path] JSON is empty, using empty object");
                    json = "{}";
                }

                var document = JsonDocument.Parse(json);
                using var stream = new MemoryStream();

                // Write the document to a stream so we can modify it
                using (var writer = new Utf8JsonWriter(stream))
                {
                    document.WriteTo(writer);
                }

                stream.Position = 0;
                using var updatedDocument = JsonDocument.Parse(stream);
                var root = updatedDocument.RootElement;

                if (string.IsNullOrEmpty(path))
                {
                    // Update the root
                    var _result = JsonSerializer.Serialize(newValue, _jsonOptions);
                    Log($"[Update Path] Updated root: {_result}");
                    return _result;
                }

                // Split the path into parts
                var parts = path.Split('.');

                Log($"[Update Path] Path has {parts.Length} parts: [{string.Join(", ", parts)}]");

                // Navigate to the parent object
                JsonElement currentElement = root;
                Stack<JsonElement> elementStack = new Stack<JsonElement>();
                elementStack.Push(root);

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    var part = parts[i];
                    Log($"[Update Path] Processing part {i}: '{part}'");

                    if (currentElement.ValueKind == JsonValueKind.Object && currentElement.TryGetProperty(part, out var nextElement))
                    {
                        elementStack.Push(nextElement);
                        currentElement = nextElement;
                        Log($"[Update Path] Moved to property '{part}'");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Property '{part}' not found or not an object.");
                    }
                }

                // Update the final property
                var finalProperty = parts[parts.Length - 1];
                Log($"[Update Path] Updating final property '{finalProperty}' to '{newValue}'");

                // Recreate the JSON with the update
                var result = UpdateJsonElement(updatedDocument.RootElement, parts, finalProperty, newValue);
                Log($"[Update Path] Final result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Log($"[Update Path] Error: {ex.Message}");
                throw new Exception($"Failed to update path '{path}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Recursively updates a JSON element at the specified path.
        /// </summary>
        private string UpdateJsonElement(JsonElement element, string[] pathParts, string propertyName, object newValue)
        {
            if (pathParts.Length == 0)
            {
                // This is the root element, update the property directly
                if (element.ValueKind == JsonValueKind.Object)
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
                    dict[propertyName] = newValue;
                    return JsonSerializer.Serialize(dict, _jsonOptions);
                }
                else
                {
                    throw new InvalidOperationException("Cannot update property on non-object root element.");
                }
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
                var firstPart = pathParts[0];

                if (dict.ContainsKey(firstPart))
                {
                    if (pathParts.Length == 1)
                    {
                        // This is the property we want to update
                        dict[propertyName] = newValue;
                        return JsonSerializer.Serialize(dict, _jsonOptions);
                    }
                    else
                    {
                        // Recurse into the nested object
                        var nestedElement = JsonSerializer.Deserialize<JsonElement>(dict[firstPart].ToString());
                        var remainingParts = pathParts.Skip(1).ToArray();
                        var updatedNested = UpdateJsonElement(nestedElement, remainingParts, propertyName, newValue);
                        dict[firstPart] = JsonSerializer.Deserialize<object>(updatedNested);
                        return JsonSerializer.Serialize(dict, _jsonOptions);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Property '{firstPart}' not found in object.");
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                // Handle array updates if needed
                var list = JsonSerializer.Deserialize<List<object>>(element.GetRawText());
                // Array handling would go here if needed
                return JsonSerializer.Serialize(list, _jsonOptions);
            }
            else
            {
                throw new InvalidOperationException($"Cannot update property in element of type {element.ValueKind}");
            }
        }

        /// <summary>
        /// Updates an array at a specific path in the JSON document.
        /// </summary>
        private string UpdateArrayInJson(string json, string arrayPath, List<object> newArray)
        {
            try
            {
                Log($"[Update Array] Starting - Updating array at path '{arrayPath}' with {newArray.Count} items");

                var parts = arrayPath.Split('.');

                if (parts.Length == 1)
                {
                    // This is a top-level array in the root object
                    var rootDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    rootDict[parts[0]] = newArray;
                    return JsonSerializer.Serialize(rootDict, _jsonOptions);
                }
                else
                {
                    // This is a nested array
                    var parentPath = string.Join(".", parts.Take(parts.Length - 1));
                    var arrayName = parts.Last();

                    var parentElement = NavigateToPath(JsonDocument.Parse(json).RootElement, parentPath);
                    var parentDict = JsonSerializer.Deserialize<Dictionary<string, object>>(parentElement.GetRawText());

                    if (parentDict.ContainsKey(arrayName))
                    {
                        parentDict[arrayName] = newArray;

                        // Update the parent in the document
                        return UpdatePathInJson(json, parentPath, parentDict);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Array property '{arrayName}' not found in parent '{parentPath}'.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[Update Array] Error: {ex.Message}");
                throw new Exception($"Failed to update array: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// Creates a typed value based on type and string value.
        /// </summary>
        private object CreateTypedValue(string type, string value)
        {
            Log($"[Create Typed Value] Creating value of type '{type}' from string '{value}'");

            try
            {
                object result;
                switch (type)
                {
                    case "String":
                        result = value;
                        break;

                    case "Number":
                        if (long.TryParse(value, out long longValue))
                            result = longValue;
                        else if (double.TryParse(value, out double doubleValue))
                            result = doubleValue;
                        else
                            throw new ArgumentException($"'{value}' is not a valid number.");
                        break;

                    case "Boolean":
                        if (bool.TryParse(value, out bool boolValue))
                            result = boolValue;
                        else
                            throw new ArgumentException($"'{value}' is not a valid boolean. Use 'true' or 'false'.");
                        break;

                    case "Date":
                        if (IsISO8601Date(value))
                            result = value; // Store dates as strings
                        else
                            throw new ArgumentException($"'{value}' is not a valid ISO8601 date format.");
                        break;

                    case "Image":
                        if (IsBase64Image(value, out _))
                            result = value; // Store images as base64 strings
                        else
                            throw new ArgumentException($"'{value}' is not a valid base64 encoded image.");
                        break;

                    case "Object":
                        result = new Dictionary<string, object>();
                        break;

                    case "Array":
                        result = new List<object>();
                        break;

                    default:
                        throw new ArgumentException($"Unknown type: {type}");
                }

                Log($"[Create Typed Value] Created value: {result} of type {result?.GetType()}");
                return result;
            }
            catch (Exception ex)
            {
                Log($"[Create Typed Value] Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Recursively validates each interactive field.
        /// </summary>
        public List<string> ValidateFields()
        {
            List<string> errors = new List<string>();
            ValidateControlsRecursively(_contentContainer, errors);
            return errors;
        }

        private void ValidateControlsRecursively(Control control, List<string> errors)
        {
            if (control is TextBox textBox)
            {
                string path = textBox.Tag as string ?? "Unknown";
                Log($"[Validate] Validating TextBox at \"{path}\" with text: \"{textBox.Text}\"");

                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    errors.Add($"Field \"{path}\" is empty.");
                }
                else if (OriginalTypes.TryGetValue(path, out var originalType))
                {
                    if (originalType == typeof(long))
                    {
                        if (!long.TryParse(textBox.Text, out _))
                            errors.Add($"Field \"{path}\" expected a long but got '{textBox.Text}'.");
                    }
                    else if (originalType == typeof(double))
                    {
                        if (!double.TryParse(textBox.Text, out _))
                            errors.Add($"Field \"{path}\" expected a double but got '{textBox.Text}'.");
                    }
                    else if (IsBase64Image(textBox.Text, out _))
                    {
                        // Already validated in IsBase64Image
                    }
                }
            }
            else if (control is FullJsonEditorPanel editorPanel)
            {
                editorPanel.ValidateControlsRecursively(editorPanel.Content, errors);
            }
            else if (control is Panel containerPanel && containerPanel.Content != null)
            {
                ValidateControlsRecursively(containerPanel.Content, errors);
            }
            else if (control is StackLayout layout)
            {
                foreach (var item in layout.Items)
                {
                    if (item.Control != null)
                        ValidateControlsRecursively(item.Control, errors);
                }
            }
        }

        /// <summary>
        /// Recursively reconstructs and returns the JSON string representation of the current UI state.
        /// </summary>
        public string ToJson()
        {
            Log("[To JSON] Starting JSON serialization");

            var errors = ValidateFields();
            if (errors.Any())
            {
                Log("[To JSON] Validation errors: " + string.Join(", ", errors));
            }

            try
            {
                // Build the JSON from the current UI state instead of using _currentJson
                var jsonObject = new Dictionary<string, object>();

                // Build from the current UI controls
                BuildJsonFromControls(_contentContainer, jsonObject);

                // Update the stored JSON
                _currentJson = JsonSerializer.Serialize(jsonObject, _jsonOptions);

                Log($"[To JSON] Built JSON from UI: {_currentJson}");
                return _currentJson;
            }
            catch (Exception ex)
            {
                Log($"[To JSON] Error: {ex.Message}");
                // Fallback to stored JSON if UI building fails
                return _currentJson ?? "{}";
            }
        }

        /// <summary>
        /// Builds a JSON object from the current UI controls.
        /// </summary>
        private void BuildJsonFromControls(Control control, Dictionary<string, object> parent, string currentPath = "")
        {
            // Handle StackLayout
            if (control is StackLayout layout)
            {
                Log($"[BuildJsonFromControls] Processing StackLayout at path '{currentPath}' with {layout.Items.Count} items");

                foreach (var item in layout.Items)
                {
                    if (item != null && item.Control != null)
                    {
                        BuildJsonFromControls(item.Control, parent, currentPath);
                    }
                }
                return;
            }

            // Handle Panel
            if (control is Panel panel && panel.Content != null)
            {
                BuildJsonFromControls(panel.Content, parent, currentPath);
                return;
            }

            // Handle FullJsonEditorPanel
            if (control is FullJsonEditorPanel editorPanel)
            {
                // For nested panels, build their JSON and add to parent at the correct path
                var panelPath = editorPanel._panelPath;
                if (!string.IsNullOrEmpty(panelPath))
                {
                    // Extract the last part of the path to use as the key
                    var lastDot = panelPath.LastIndexOf('.');
                    var key = lastDot >= 0 ? panelPath.Substring(lastDot + 1) : panelPath;

                    // Create nested structure if needed
                    CreateNestedStructure(parent, panelPath, key);

                    var targetDict = GetNestedDictionary(parent, panelPath);
                    BuildJsonFromControls(editorPanel._contentContainer, targetDict, panelPath);
                }
                return;
            }

            // Handle individual interactive controls (TextBox, CheckBox, etc.)
            string propertyName = null;
            object propertyValue = null;

            if (control is TextBox textBox)
            {
                var tag = textBox.Tag as string;
                if (!string.IsNullOrEmpty(tag))
                {
                    propertyName = tag;
                    propertyValue = textBox.Text;
                    Log($"[BuildJsonFromControls] TextBox tag: '{tag}', value: '{textBox.Text}'");
                }
            }
            else if (control is CheckBox checkBox)
            {
                var tag = checkBox.Tag as string;
                if (!string.IsNullOrEmpty(tag))
                {
                    propertyName = tag;
                    propertyValue = checkBox.Checked;
                    Log($"[BuildJsonFromControls] CheckBox tag: '{tag}', value: '{checkBox.Checked}'");
                }
            }
            else if (control is DateTimePicker datePicker)
            {
                var tag = datePicker.Tag as string;
                if (!string.IsNullOrEmpty(tag))
                {
                    propertyName = tag;
                    propertyValue = datePicker.Value?.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    Log($"[BuildJsonFromControls] DateTimePicker tag: '{tag}', value: '{datePicker.Value}'");
                }
            }

            // If we have a property name and value, add it to the correct nested location
            if (!string.IsNullOrEmpty(propertyName) && propertyValue != null)
            {
                // Parse the property path to determine where it belongs
                var pathParts = propertyName.Split('.');

                if (pathParts.Length > 1)
                {
                    // This is a nested property
                    var objectPath = string.Join(".", pathParts.Take(pathParts.Length - 1));
                    var key = pathParts.Last();

                    CreateNestedStructure(parent, objectPath, key);
                    var targetDict = GetNestedDictionary(parent, objectPath);
                    targetDict[key] = propertyValue;

                    Log($"[BuildJsonFromControls] Added nested property '{objectPath}.{key}': {propertyValue}");
                }
                else
                {
                    // This is a root-level property
                    parent[propertyName] = propertyValue;
                    Log($"[BuildJsonFromControls] Added root property '{propertyName}': {propertyValue}");
                }
            }
        }

        /// <summary>
        /// Creates nested dictionary structure based on the path.
        /// </summary>
        private void CreateNestedStructure(Dictionary<string, object> root, string path, string key)
        {
            var pathParts = path.Split('.');
            var current = root;

            for (int i = 0; i < pathParts.Length; i++)
            {
                var part = pathParts[i];

                if (!current.ContainsKey(part))
                {
                    current[part] = new Dictionary<string, object>();
                    Log($"[CreateNestedStructure] Created nested object '{part}'");
                }

                if (current[part] is Dictionary<string, object> nestedDict)
                {
                    current = nestedDict;
                }
                else
                {
                    // This shouldn't happen, but just in case
                    current[part] = new Dictionary<string, object>();
                    current = (Dictionary<string, object>)current[part];
                }
            }
        }

        /// <summary>
        /// Gets the nested dictionary at the specified path.
        /// </summary>
        private Dictionary<string, object> GetNestedDictionary(Dictionary<string, object> root, string path)
        {
            var pathParts = path.Split('.');
            var current = root;

            for (int i = 0; i < pathParts.Length; i++)
            {
                var part = pathParts[i];

                if (current.ContainsKey(part) && current[part] is Dictionary<string, object> nestedDict)
                {
                    current = nestedDict;
                }
                else
                {
                    // Create it if it doesn't exist
                    current[part] = new Dictionary<string, object>();
                    current = (Dictionary<string, object>)current[part];
                }
            }

            return current;
        }
        /// <summary>
        /// Gets the value from a control based on its type.
        /// </summary>
        private object GetControlValue(Control control)
        {
            if (control is TextBox textBox)
            {
                return textBox.Text;
            }
            else if (control is CheckBox checkBox)
            {
                return checkBox.Checked;
            }
            else if (control is FullJsonEditorPanel panel)
            {
                // For nested panels, recursively get their JSON
                return panel.ToJson();
            }
            else if (control is DateTimePicker datePicker)
            {
                return datePicker.Value?.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            return null;
        }


    }

    /// <summary>
    /// Represents the type of an image.
    /// </summary>
    public enum ImageType
    {
        Unknown,
        Jpeg,
        Png
    }
}