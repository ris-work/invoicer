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
using Terminal.Gui;

namespace TUIJsonEditorExample
{
    // Define a logging delegate for better control
    public delegate void TUILogDelegate(string message);

    public class TUIFullJsonEditorPanel : FrameView
    {
        private int fontSize = 12;
        private int cWidth = 200;
        private bool _appMode = false;
        private bool _showHeader = true;
        private bool _showUploadButton = true;
        public ITUIJsonNode _rootNode;
        private ScrollView _scrollView;
        private View _contentView;

        public ITUIJsonNode RootNode => _rootNode;

        // Add a logging delegate
        public static TUILogDelegate Log = Console.WriteLine;

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
        /// Constructs a TUIFullJsonEditorPanel from a JSON string.
        /// </summary>
        public TUIFullJsonEditorPanel(
            string jsonString,
            bool showHeader = true,
            bool showUploadButton = true
        )
        {
            Log($"[Init] String constructor called with showHeader: {showHeader}, showUploadButton: {showUploadButton}");

            _showHeader = showHeader;
            _showUploadButton = showUploadButton;

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
                jsonString = JsonSerializer.Serialize(document, _jsonOptions);

                Log($"[Init] Normalized JSON: {jsonString}");

                // Create the root node from the JSON
                _rootNode = TUIJsonNodeFactory.CreateFromJson(jsonString);
            }
            catch (JsonException ex)
            {
                // If JSON is invalid, start with an empty object
                Log($"[Init] Invalid JSON: {ex.Message}");
                _rootNode = new TUIJsonObjectNode();
            }

            InitializeUI();
            Log("[Init] String constructor completed successfully");
        }

        /// <summary>
        /// Initializes the UI components.
        /// </summary>
        private void InitializeUI()
        {
            Title = "JSON Editor";

            // Create a scrollable view for the content
            _scrollView = new ScrollView()
            {
                ShowVerticalScrollIndicator = true,
                ShowHorizontalScrollIndicator = true,
                ContentSize = new Size(Dim.Fill(), Dim.Fill())
            };

            // Create the content container
            _contentView = new View()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            // Create a vertical layout for the content
            var layout = new View()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Auto()
            };

            // Create header if needed
            if (_showHeader)
            {
                var header = CreateHeader();
                layout.Add(header);
            }

            // Add the root node's control to the content
            var rootControl = _rootNode.CreateControl(this);
            layout.Add(rootControl);

            _contentView.Add(layout);
            _scrollView.Content = _contentView;
            Add(_scrollView);

            // Set control states based on app mode
            UpdateControlStates();
        }

        public View CreateControl()
        {
            // Add the root node's control to the content
            var rootControl = _rootNode.CreateControl(this);

            // Set the parent references for the root node
            if (_rootNode is TUIJsonObjectNode objectNode)
            {
                if (rootControl is View rootLayout)
                {
                    objectNode.SetParentReferences(this, rootLayout);
                }
            }
            else if (_rootNode is TUIJsonArrayNode arrayNode)
            {
                if (rootControl is View rootLayout)
                {
                    arrayNode.SetParentReferences(this, rootLayout);
                }
            }

            return rootControl;
        }

        /// <summary>
        /// Creates the header with Show and Load buttons.
        /// </summary>
        private View CreateHeader()
        {
            var headerPanel = new View()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 3
            };

            var showButton = new Button("Show JSON")
            {
                X = 0,
                Y = 0
            };
            var loadButton = new Button("Load JSON")
            {
                X = 20,
                Y = 0
            };
            var validateButton = new Button("Validate")
            {
                X = 40,
                Y = 0
            };
            var refreshButton = new Button("Refresh UI")
            {
                X = 60,
                Y = 0
            };

            // Attach event handlers
            showButton.Clicked += ShowJsonDialog;
            loadButton.Clicked += LoadJsonDialog;
            validateButton.Clicked += ValidateAndShowErrors;
            refreshButton.Clicked += (sender, e) => RefreshUI();

            headerPanel.Add(showButton, loadButton, validateButton, refreshButton);

            return headerPanel;
        }

        private void RefreshUI()
        {
            try
            {
                Log("[Refresh UI] Starting full UI refresh");

                // Get the current JSON
                var currentJson = ToJson();

                // Update the root node with the current JSON
                _rootNode = TUIJsonNodeFactory.CreateFromJson(currentJson);

                // Rebuild the entire UI
                InitializeUI();

                Log("[Refresh UI] Full UI refresh completed successfully");
            }
            catch (Exception ex)
            {
                Log($"[Refresh UI] Error: {ex.Message}");
                MessageBox.ErrorQuery("Error", $"Error refreshing UI: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Updates the visibility of the header based on the ShowHeader property.
        /// </summary>
        private void UpdateHeaderVisibility()
        {
            if (_contentView == null || _contentView.Subviews.Count == 0)
                return;

            // Get the main layout view
            var layout = _contentView.Subviews[0];

            // Check if header is currently visible
            bool headerVisible = layout.Subviews.Count > 0 && layout.Subviews[0] is View;

            // Show header if needed and not already visible
            if (_showHeader && !headerVisible)
            {
                var header = CreateHeader();
                layout.Subviews.Insert(0, header);
                layout.SetNeedsDisplay();
            }
            // Hide header if needed and currently visible
            else if (!_showHeader && headerVisible)
            {
                layout.Remove(layout.Subviews[0]);
                layout.SetNeedsDisplay();
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
                // Get the latest JSON from the root node
                var json = ToJson();
                Log($"[Show JSON] Current JSON: {json}");

                // Create a dialog to display the JSON
                var dialog = new Dialog()
                {
                    Title = "Current JSON",
                    Width = Dim.Percent(80),
                    Height = Dim.Percent(80)
                };

                var textView = new TextView()
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill() - 1,
                    Text = json,
                    ReadOnly = true
                };

                var closeButton = new Button("Close")
                {
                    X = Pos.Center(),
                    Y = Pos.Bottom(textView)
                };
                closeButton.Clicked += () => { Application.RequestStop(); };

                dialog.Add(textView, closeButton);
                Application.Run(dialog);

                Log("[Show JSON] Dialog shown successfully");
            }
            catch (Exception ex)
            {
                Log($"[Show JSON] Error: {ex.Message}");
                MessageBox.ErrorQuery("Error", $"Error showing JSON: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Shows a dialog to load new JSON.
        /// </summary>
        private void LoadJsonDialog(object sender, EventArgs e)
        {
            // Create dialog and content
            var dialog = new Dialog()
            {
                Title = "Load JSON",
                Width = Dim.Percent(80),
                Height = Dim.Percent(80)
            };

            var jsonTextView = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1,
                Text = ToJson()
            };

            var loadButton = new Button("Load")
            {
                X = Pos.Center() - 10,
                Y = Pos.Bottom(jsonTextView)
            };
            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 10,
                Y = Pos.Bottom(jsonTextView)
            };

            // Attach event handlers
            loadButton.Clicked += () => {
                try
                {
                    Log($"[Load JSON] New JSON to load: {jsonTextView.Text.ToString()}");
                    UpdateJson(jsonTextView.Text.ToString());
                    Log("[Load JSON] JSON loaded successfully");
                    Application.RequestStop();
                }
                catch (Exception ex)
                {
                    Log($"[Load JSON] Error: {ex.Message}");
                    MessageBox.ErrorQuery("Error", $"Error loading JSON: {ex.Message}", "OK");
                }
            };

            cancelButton.Clicked += () => { Application.RequestStop(); };

            dialog.Add(jsonTextView, loadButton, cancelButton);
            Application.Run(dialog);
        }

        /// <summary>
        /// Validates the current JSON and shows any errors.
        /// </summary>
        private void ValidateAndShowErrors(object sender, EventArgs e)
        {
            var errors = ValidateFields();

            // Create dialog and content
            var dialog = new Dialog()
            {
                Title = "Validation Results",
                Width = Dim.Percent(60),
                Height = Dim.Percent(60)
            };

            View errorContent;

            if (errors.Any())
            {
                var errorText = string.Join("\n", errors.Select(e => $"• {e}"));
                var errorLabel = new Label(errorText)
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill() - 1,
                    Height = Dim.Fill() - 1,
                    TextAlignment = TextAlignment.Left
                };

                var scrollView = new ScrollView()
                {
                    X = 0,
                    Y = 1,
                    Width = Dim.Fill(),
                    Height = Dim.Fill() - 2,
                    ContentSize = new Size(errorLabel.Frame.Width, errorLabel.Frame.Height),
                    ShowVerticalScrollIndicator = true,
                    ShowHorizontalScrollIndicator = true
                };
                scrollView.Add(errorLabel);

                errorContent = new View()
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };
                errorContent.Add(new Label("Validation errors found:") { X = 0, Y = 0 });
                errorContent.Add(scrollView);
            }
            else
            {
                errorContent = new Label("No validation errors found.")
                {
                    X = Pos.Center(),
                    Y = Pos.Center()
                };
            }

            var closeButton = new Button("Close")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(errorContent)
            };
            closeButton.Clicked += () => { Application.RequestStop(); };

            dialog.Add(errorContent, closeButton);
            Application.Run(dialog);
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

                // Create the new root node
                _rootNode = TUIJsonNodeFactory.CreateFromJson(json);

                // Rebuild the UI
                InitializeUI();

                Log("[Update JSON] UI updated successfully");
            }
            catch (JsonException ex)
            {
                Log($"[Update JSON] JSON error: {ex.Message}");
                MessageBox.ErrorQuery("Error", $"Invalid JSON: {ex.Message}", "OK");
            }
            catch (Exception ex)
            {
                Log($"[Update JSON] General error: {ex.Message}");
                MessageBox.ErrorQuery("Error", $"Failed to update JSON: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Updates the enabled state of controls based on the AppMode setting.
        /// </summary>
        private void UpdateControlStates()
        {
            UpdateControlStatesRecursively(_contentView);
        }

        private void UpdateControlStatesRecursively(View control)
        {
            if (control is TextField textField)
            {
                textField.ReadOnly = AppMode;
            }
            else if (control is CheckBox checkBox)
            {
                // In Terminal.Gui, CheckBox doesn't have an Enabled property
                // We'll need to handle this differently if needed
            }
            else if (control is Button button)
            {
                // In app mode, only keep buttons that start with "button:" enabled
                if (AppMode && !(button.Text.StartsWith("button:") || button.Text.Contains("Add")))
                {
                    button.Enabled = false;
                }
                else
                {
                    button.Enabled = true;
                }
            }

            // Recursively update child controls
            foreach (var child in control.Subviews)
            {
                UpdateControlStatesRecursively(child);
            }
        }

        /// <summary>
        /// Recursively validates each interactive field.
        /// </summary>
        public List<string> ValidateFields()
        {
            List<string> errors = new List<string>();
            _rootNode.Validate(errors);
            return errors;
        }

        /// <summary>
        /// Returns the JSON string representation of the current UI state.
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
                // Get the JSON from the root node
                var json = _rootNode.ToJson();

                // Parse and re-serialize with indentation
                var document = JsonDocument.Parse(json);
                var indentedJson = JsonSerializer.Serialize(document, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                Log($"[To JSON] Built JSON: {indentedJson}");
                return indentedJson;
            }
            catch (Exception ex)
            {
                Log($"[To JSON] Error: {ex.Message}");
                return "{}";
            }
        }
    }

    /// <summary>
    /// Interface for all JSON nodes in the tree.
    /// </summary>
    public interface ITUIJsonNode
    {
        /// <summary>
        /// Creates a control for this node.
        /// </summary>
        View CreateControl(TUIFullJsonEditorPanel editorPanel);

        /// <summary>
        /// Converts this node to JSON.
        /// </summary>
        string ToJson();

        /// <summary>
        /// Validates this node and adds any errors to the list.
        /// </summary>
        void Validate(List<string> errors);
    }

    /// <summary>
    /// Factory class for creating JSON nodes.
    /// </summary>
    public static class TUIJsonNodeFactory
    {
        /// <summary>
        /// Creates a JSON node from a JSON string.
        /// </summary>
        public static ITUIJsonNode CreateFromJson(string json)
        {
            var document = JsonDocument.Parse(json);
            return CreateFromJsonElement(document.RootElement);
        }

        /// <summary>
        /// Creates a JSON node from a JsonElement.
        /// </summary>
        public static ITUIJsonNode CreateFromJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var objNode = new TUIJsonObjectNode();
                    foreach (var property in element.EnumerateObject())
                    {
                        objNode.Properties[property.Name] = CreateFromJsonElement(property.Value);
                    }
                    return objNode;

                case JsonValueKind.Array:
                    var arrayNode = new TUIJsonArrayNode();
                    foreach (var item in element.EnumerateArray())
                    {
                        arrayNode.Items.Add(CreateFromJsonElement(item));
                    }
                    return arrayNode;

                case JsonValueKind.String:
                    return new TUIJsonStringNode { Value = element.GetString() };

                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long l))
                        return new TUIJsonNumberNode { Value = l };
                    else
                        return new TUIJsonNumberNode { Value = element.GetDouble() };

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return new TUIJsonBooleanNode { Value = element.GetBoolean() };

                case JsonValueKind.Null:
                    return new TUIJsonNullNode();

                default:
                    throw new NotSupportedException($"Unsupported JSON value kind: {element.ValueKind}");
            }
        }

        /// <summary>
        /// Creates a new JSON node of the specified type.
        /// </summary>
        public static ITUIJsonNode CreateNewNode(string type, string value = null)
        {
            switch (type)
            {
                case "String":
                    return new TUIJsonStringNode { Value = value ?? "" };

                case "Number":
                    if (long.TryParse(value, out long l))
                        return new TUIJsonNumberNode { Value = l };
                    else if (double.TryParse(value, out double d))
                        return new TUIJsonNumberNode { Value = d };
                    else
                        return new TUIJsonNumberNode { Value = 0 };

                case "Boolean":
                    if (bool.TryParse(value, out bool b))
                        return new TUIJsonBooleanNode { Value = b };
                    else
                        return new TUIJsonBooleanNode { Value = false };

                case "Object":
                    return new TUIJsonObjectNode();

                case "Array":
                    return new TUIJsonArrayNode();

                case "Null":
                    return new TUIJsonNullNode();

                case "Date":
                    if (DateTime.TryParse(value, out DateTime date))
                        return new TUIJsonStringNode { Value = date.ToString("yyyy-MM-ddTHH:mm:ssZ") };
                    else
                        return new TUIJsonStringNode { Value = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ") };

                case "Image":
                    if (IsBase64Image(value, out TUIImageType imageType) && imageType != TUIImageType.Unknown)
                        return new TUIJsonStringNode { Value = value };
                    else
                        return new TUIJsonStringNode { Value = "" };

                default:
                    throw new ArgumentException($"Unknown node type: {type}");
            }
        }

        // Add this helper method to TUIJsonNodeFactory class:
        private static bool IsBase64Image(string value, out TUIImageType imageType)
        {
            imageType = TUIImageType.Unknown;

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
                    imageType = TUIImageType.Jpeg;
                    return true;
                }

                // PNG signature: 89 50 4E 47
                if (bytes.Length >= 8 &&
                    bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                {
                    imageType = TUIImageType.Png;
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
    }

    /// <summary>
    /// Represents a JSON object node.
    /// </summary>
    public class TUIJsonObjectNode : ITUIJsonNode
    {
        public Dictionary<string, ITUIJsonNode> Properties { get; set; } = new Dictionary<string, ITUIJsonNode>();

        // Direct reference to the parent editor panel
        private TUIFullJsonEditorPanel _editorPanel;

        // Direct reference to the parent container
        private View _parentContainer;

        // Method to set the parent references
        public void SetParentReferences(TUIFullJsonEditorPanel editorPanel, View parentContainer)
        {
            _editorPanel = editorPanel;
            _parentContainer = parentContainer;
        }

        public void RebuildPropertyControl(TUIFullJsonEditorPanel editorPanel, string key)
        {
            if (!Properties.ContainsKey(key))
                return;

            // Find the index of the property control in the parent container
            int index = -1;
            for (int i = 0; i < _parentContainer.Subviews.Count; i++)
            {
                var item = _parentContainer.Subviews[i];
                if (item is View row && row.Subviews.Count > 0)
                {
                    var firstItem = row.Subviews[0];
                    if (firstItem is Label label && label.Text == key)
                    {
                        index = i;
                        break;
                    }
                }
            }

            if (index >= 0)
            {
                // Remove the old property control
                _parentContainer.Remove(_parentContainer.Subviews[index]);

                // Create the new property control
                var propertyControl = CreatePropertyControl(editorPanel, key, Properties[key]);

                // Insert the new property control at the same position
                _parentContainer.Subviews.Insert(index, propertyControl);
                _parentContainer.SetNeedsDisplay();
            }
        }

        public View CreateControl(TUIFullJsonEditorPanel editorPanel)
        {
            var container = new View()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Auto()
            };

            // Add a button to add new properties
            var addButton = new Button("Add Property")
            {
                X = 0,
                Y = Pos.Top(container),
                Width = 15
            };
            addButton.Clicked += (sender, e) => AddProperty(editorPanel, container);
            container.Add(addButton);

            // Add controls for each property
            int y = 1;
            foreach (var kvp in Properties)
            {
                var propertyControl = CreatePropertyControl(editorPanel, kvp.Key, kvp.Value);
                propertyControl.Y = y;
                container.Add(propertyControl);
                y += propertyControl.Frame.Height;
            }

            return container;
        }

        private View CreatePropertyControl(TUIFullJsonEditorPanel editorPanel, string key, ITUIJsonNode value)
        {
            var row = new View()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Auto()
            };

            // Property name label
            var nameLabel = new Label(key)
            {
                X = 0,
                Y = 0,
                Width = 20
            };
            row.Add(nameLabel);

            // Create a container for the value control
            var valueContainer = new View()
            {
                X = Pos.Right(nameLabel) + 1,
                Y = 0,
                Width = Dim.Fill() - 25,
                Height = Dim.Auto()
            };

            // Property value control
            var valueControl = value.CreateControl(editorPanel);
            valueContainer.Add(valueControl);

            // Set up parent references for string nodes
            if (value is TUIJsonStringNode stringNode)
            {
                stringNode.SetParentReferences(editorPanel, valueContainer, key, -1);
            }

            row.Add(valueContainer);

            // Delete button
            var deleteButton = new Button("×")
            {
                X = Pos.Right(valueContainer) + 1,
                Y = 0,
                Width = 3
            };
            deleteButton.Clicked += (sender, e) => DeleteProperty(editorPanel, key);
            row.Add(deleteButton);

            // Set the height of the row based on the tallest child
            int maxHeight = Math.Max(nameLabel.Frame.Height, Math.Max(valueControl.Frame.Height, deleteButton.Frame.Height));
            row.Height = maxHeight;

            return row;
        }

        private void AddProperty(TUIFullJsonEditorPanel editorPanel, View container)
        {
            // Create a dialog for adding a new property
            var dialog = new Dialog()
            {
                Title = "Add New Property",
                Width = Dim.Percent(60),
                Height = Dim.Percent(60)
            };

            var propertyNameField = new TextField("")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill()
            };
            var propertyTypeDropDown = new ListView()
            {
                X = 0,
                Y = 3,
                Width = Dim.Fill(),
                Height = 8,
                Source = new List<string> { "String", "Number", "Boolean", "Date", "Image", "Object", "Array", "Null" }
            };
            propertyTypeDropDown.SelectedItem = 0;

            var propertyValueField = new TextField("")
            {
                X = 0,
                Y = 12,
                Width = Dim.Fill()
            };

            var validationLabel = new Label("")
            {
                X = 0,
                Y = 14,
                Width = Dim.Fill(),
                TextColor = Color.Red
            };

            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = Pos.Bottom(validationLabel) + 1
            };
            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 10,
                Y = Pos.Bottom(validationLabel) + 1
            };

            // Add labels
            dialog.Add(
                new Label("Property Name:") { X = 0, Y = 0 },
                propertyNameField,
                new Label("Property Type:") { X = 0, Y = 2 },
                propertyTypeDropDown,
                new Label("Property Value (optional):") { X = 0, Y = 11 },
                propertyValueField,
                validationLabel,
                okButton,
                cancelButton
            );

            // Set up event handlers
            propertyTypeDropDown.SelectedItemChanged += (sender, e) => {
                // Update placeholder text based on type
                switch (propertyTypeDropDown.Source.ToList()[propertyTypeDropDown.SelectedItem])
                {
                    case "String":
                        propertyValueField.Text = "";
                        propertyValueField.ReadOnly = false;
                        break;
                    case "Number":
                        propertyValueField.Text = "";
                        propertyValueField.ReadOnly = false;
                        break;
                    case "Boolean":
                        propertyValueField.Text = "";
                        propertyValueField.ReadOnly = false;
                        break;
                    case "Date":
                        propertyValueField.Text = "";
                        propertyValueField.ReadOnly = false;
                        break;
                    case "Image":
                        propertyValueField.Text = "";
                        propertyValueField.ReadOnly = false;
                        break;
                    case "Null":
                        propertyValueField.Text = "Value will be null";
                        propertyValueField.ReadOnly = true;
                        break;
                    case "Object":
                        propertyValueField.Text = "Creating empty object";
                        propertyValueField.ReadOnly = true;
                        break;
                    case "Array":
                        propertyValueField.Text = "Creating empty array";
                        propertyValueField.ReadOnly = true;
                        break;
                }

                // Clear validation when type changes
                validationLabel.Text = "";
            };

            okButton.Clicked += () => {
                var name = propertyNameField.Text.ToString();
                var type = propertyTypeDropDown.Source.ToList()[propertyTypeDropDown.SelectedItem];
                var value = propertyValueField.Text.ToString();

                if (string.IsNullOrEmpty(name))
                {
                    validationLabel.Text = "Property name cannot be empty.";
                }
                else if (Properties.ContainsKey(name))
                {
                    validationLabel.Text = "Property already exists.";
                }
                else
                {
                    // Create the new node
                    var newNode = TUIJsonNodeFactory.CreateNewNode(type, value);

                    // If it's an Image type with no value, open the image upload dialog
                    if (type == "Image" && string.IsNullOrEmpty(value))
                    {
                        var stringNode = newNode as TUIJsonStringNode;
                        stringNode.UploadImageAsBase64(editorPanel, (newValue) => stringNode.Value = newValue);
                    }

                    // Add the property
                    Properties[name] = newNode;

                    // Rebuild the UI
                    RebuildContainer(editorPanel, container);
                    Application.RequestStop();
                }
            };

            cancelButton.Clicked += () => {
                Application.RequestStop();
            };

            Application.Run(dialog);
        }

        private void DeleteProperty(TUIFullJsonEditorPanel editorPanel, string key)
        {
            // Confirm deletion
            bool result = MessageBox.Query("Confirm Delete", $"Are you sure you want to delete property '{key}'?", "Yes", "No") == 0;

            if (result)
            {
                // Remove the property
                Properties.Remove(key);

                // Find the index of the property control in the parent container
                int index = -1;
                for (int i = 0; i < _parentContainer.Subviews.Count; i++)
                {
                    var item = _parentContainer.Subviews[i];
                    if (item is View row && row.Subviews.Count > 0)
                    {
                        var firstItem = row.Subviews[0];
                        if (firstItem is Label label && label.Text == key)
                        {
                            index = i;
                            break;
                        }
                    }
                }

                if (index >= 0)
                {
                    // Remove the property control
                    _parentContainer.Remove(_parentContainer.Subviews[index]);
                    _parentContainer.SetNeedsDisplay();
                }
            }
        }

        private void RebuildContainer(TUIFullJsonEditorPanel editorPanel, View container)
        {
            // Clear the container except for the add button
            var addButton = container.Subviews[0];
            container.RemoveAll();

            // Re-add the add button
            container.Add(addButton);

            // Re-add all property controls
            int y = 1;
            foreach (var kvp in Properties)
            {
                var propertyControl = CreatePropertyControl(editorPanel, kvp.Key, kvp.Value);
                propertyControl.Y = y;
                container.Add(propertyControl);
                y += propertyControl.Frame.Height;
            }

            container.SetNeedsDisplay();
        }

        public string ToJson()
        {
            var properties = new List<string>();
            foreach (var kvp in Properties)
            {
                properties.Add($"\"{kvp.Key}\": {kvp.Value.ToJson()}");
            }
            return $"{{{string.Join(", ", properties)}}}";
        }

        public void Validate(List<string> errors)
        {
            // Validate each property
            foreach (var kvp in Properties)
            {
                kvp.Value.Validate(errors);
            }
        }
    }

    /// <summary>
    /// Represents a JSON array node.
    /// </summary>
    public class TUIJsonArrayNode : ITUIJsonNode
    {
        public List<ITUIJsonNode> Items { get; set; } = new List<ITUIJsonNode>();

        // Direct reference to the parent editor panel
        private TUIFullJsonEditorPanel _editorPanel;

        // Direct reference to the parent container
        private View _parentContainer;

        // Method to set the parent references
        public void SetParentReferences(TUIFullJsonEditorPanel editorPanel, View parentContainer)
        {
            _editorPanel = editorPanel;
            _parentContainer = parentContainer;
        }

        public void RebuildItemControl(TUIFullJsonEditorPanel editorPanel, int index)
        {
            if (index < 0 || index >= Items.Count)
                return;

            // Find the index of the item control in the parent container
            int controlIndex = -1;
            for (int i = 0; i < _parentContainer.Subviews.Count; i++)
            {
                var item = _parentContainer.Subviews[i];
                if (item is View row && row.Subviews.Count > 0)
                {
                    var firstItem = row.Subviews[0];
                    if (firstItem is Label label && label.Text == $"[{index}]")
                    {
                        controlIndex = i;
                        break;
                    }
                }
            }

            if (controlIndex >= 0)
            {
                // Remove the old item control
                _parentContainer.Remove(_parentContainer.Subviews[controlIndex]);

                // Create the new item control
                var itemControl = CreateItemControl(editorPanel, index, Items[index]);

                // Insert the new item control at the same position
                _parentContainer.Subviews.Insert(controlIndex, itemControl);
                _parentContainer.SetNeedsDisplay();
            }
        }

        public View CreateControl(TUIFullJsonEditorPanel editorPanel)
        {
            var container = new View()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Auto()
            };

            // Add a button to add new items
            var addButton = new Button("Add Item")
            {
                X = 0,
                Y = Pos.Top(container),
                Width = 10
            };
            addButton.Clicked += (sender, e) => AddItem(editorPanel, container);
            container.Add(addButton);

            // Add controls for each item
            int y = 1;
            for (int i = 0; i < Items.Count; i++)
            {
                var itemControl = CreateItemControl(editorPanel, i, Items[i]);
                itemControl.Y = y;
                container.Add(itemControl);
                y += itemControl.Frame.Height;
            }

            return container;
        }

        private View CreateItemControl(TUIFullJsonEditorPanel editorPanel, int index, ITUIJsonNode item)
        {
            var row = new View()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Auto()
            };

            // Item index label
            var indexLabel = new Label($"[{index}]")
            {
                X = 0,
                Y = 0,
                Width = 8
            };
            row.Add(indexLabel);

            // Create a container for the value control
            var valueContainer = new View()
            {
                X = Pos.Right(indexLabel) + 1,
                Y = 0,
                Width = Dim.Fill() - 15,
                Height = Dim.Auto()
            };

            // Item value control
            var valueControl = item.CreateControl(editorPanel);
            valueContainer.Add(valueControl);

            // Set up parent references for string nodes
            if (item is TUIJsonStringNode stringNode)
            {
                stringNode.SetParentReferences(editorPanel, valueContainer, null, index);
            }

            row.Add(valueContainer);

            // Delete button
            var deleteButton = new Button("×")
            {
                X = Pos.Right(valueContainer) + 1,
                Y = 0,
                Width = 3
            };
            deleteButton.Clicked += (sender, e) => DeleteItem(editorPanel, index);
            row.Add(deleteButton);

            // Set the height of the row based on the tallest child
            int maxHeight = Math.Max(indexLabel.Frame.Height, Math.Max(valueControl.Frame.Height, deleteButton.Frame.Height));
            row.Height = maxHeight;

            return row;
        }

        private void AddItem(TUIFullJsonEditorPanel editorPanel, View container)
        {
            // Create a dialog for adding a new item
            var dialog = new Dialog()
            {
                Title = "Add New Array Item",
                Width = Dim.Percent(60),
                Height = Dim.Percent(60)
            };

            var itemTypeDropDown = new ListView()
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = 8,
                Source = new List<string> { "String", "Number", "Boolean", "Date", "Image", "Object", "Array", "Null" }
            };
            itemTypeDropDown.SelectedItem = 0;

            var itemValueField = new TextField("")
            {
                X = 0,
                Y = 10,
                Width = Dim.Fill()
            };

            var validationLabel = new Label("")
            {
                X = 0,
                Y = 12,
                Width = Dim.Fill(),
                TextColor = Color.Red
            };

            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = Pos.Bottom(validationLabel) + 1
            };
            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 10,
                Y = Pos.Bottom(validationLabel) + 1
            };

            // Add labels
            dialog.Add(
                new Label("Item Type:") { X = 0, Y = 0 },
                itemTypeDropDown,
                new Label("Item Value (optional):") { X = 0, Y = 9 },
                itemValueField,
                validationLabel,
                okButton,
                cancelButton
            );

            // Set up event handlers
            itemTypeDropDown.SelectedItemChanged += (sender, e) => {
                // Update placeholder text based on type
                switch (itemTypeDropDown.Source.ToList()[itemTypeDropDown.SelectedItem])
                {
                    case "String":
                        itemValueField.Text = "";
                        itemValueField.ReadOnly = false;
                        break;
                    case "Number":
                        itemValueField.Text = "";
                        itemValueField.ReadOnly = false;
                        break;
                    case "Boolean":
                        itemValueField.Text = "";
                        itemValueField.ReadOnly = false;
                        break;
                    case "Date":
                        itemValueField.Text = "";
                        itemValueField.ReadOnly = false;
                        break;
                    case "Image":
                        itemValueField.Text = "";
                        itemValueField.ReadOnly = false;
                        break;
                    case "Null":
                        itemValueField.Text = "Value will be null";
                        itemValueField.ReadOnly = true;
                        break;
                    case "Object":
                        itemValueField.Text = "Creating empty object";
                        itemValueField.ReadOnly = true;
                        break;
                    case "Array":
                        itemValueField.Text = "Creating empty array";
                        itemValueField.ReadOnly = true;
                        break;
                }

                // Clear validation when type changes
                validationLabel.Text = "";
            };

            okButton.Clicked += () => {
                var type = itemTypeDropDown.Source.ToList()[itemTypeDropDown.SelectedItem];
                var value = itemValueField.Text.ToString();

                // Create the new node
                var newNode = TUIJsonNodeFactory.CreateNewNode(type, value);

                // If it's an Image type with no value, open the image upload dialog
                if (type == "Image" && string.IsNullOrEmpty(value))
                {
                    var stringNode = newNode as TUIJsonStringNode;
                    stringNode.UploadImageAsBase64(editorPanel, (newValue) => stringNode.Value = newValue);
                }

                // Add the item
                Items.Add(newNode);

                // Rebuild the UI
                RebuildContainer(editorPanel, container);
                Application.RequestStop();
            };

            cancelButton.Clicked += () => {
                Application.RequestStop();
            };

            Application.Run(dialog);
        }

        private void DeleteItem(TUIFullJsonEditorPanel editorPanel, int index)
        {
            // Confirm deletion
            bool result = MessageBox.Query("Confirm Delete", $"Are you sure you want to delete item at index {index}?", "Yes", "No") == 0;

            if (result)
            {
                // Remove the item
                Items.RemoveAt(index);

                // Find the index of the item control in the parent container
                int controlIndex = -1;
                for (int i = 0; i < _parentContainer.Subviews.Count; i++)
                {
                    var item = _parentContainer.Subviews[i];
                    if (item is View row && row.Subviews.Count > 0)
                    {
                        var firstItem = row.Subviews[0];
                        if (firstItem is Label label && label.Text == $"[{index}]")
                        {
                            controlIndex = i;
                            break;
                        }
                    }
                }

                if (controlIndex >= 0)
                {
                    // Remove the item control
                    _parentContainer.Remove(_parentContainer.Subviews[controlIndex]);
                    _parentContainer.SetNeedsDisplay();
                }
            }
        }

        private void RebuildContainer(TUIFullJsonEditorPanel editorPanel, View container)
        {
            // Clear the container except for the add button
            var addButton = container.Subviews[0];
            container.RemoveAll();

            // Re-add the add button
            container.Add(addButton);

            // Re-add all item controls
            int y = 1;
            for (int i = 0; i < Items.Count; i++)
            {
                var itemControl = CreateItemControl(editorPanel, i, Items[i]);
                itemControl.Y = y;
                container.Add(itemControl);
                y += itemControl.Frame.Height;
            }

            container.SetNeedsDisplay();
        }

        public string ToJson()
        {
            var items = new List<string>();
            foreach (var item in Items)
            {
                items.Add(item.ToJson());
            }
            return $"[{string.Join(", ", items)}]";
        }

        public void Validate(List<string> errors)
        {
            // Validate each item
            foreach (var item in Items)
            {
                item.Validate(errors);
            }
        }
    }

    /// <summary>
    /// Represents a JSON string node.
    /// </summary>
    public class TUIJsonStringNode : ITUIJsonNode
    {
        public string Value { get; set; }
        public Action<TUIJsonStringNode> OnControlNeedsRebuild { get; set; }

        // Direct reference to the parent container
        private View _parentContainer;

        // Direct reference to the parent property name (for objects) or index (for arrays)
        private string _parentKey;
        private int _parentIndex;

        // Direct reference to the parent editor panel
        private TUIFullJsonEditorPanel _editorPanel;

        public void SetParentReferences(TUIFullJsonEditorPanel editorPanel, View parentContainer, string parentKey = null, int parentIndex = -1)
        {
            _editorPanel = editorPanel;
            _parentContainer = parentContainer;
            _parentKey = parentKey;
            _parentIndex = parentIndex;
        }

        public View CreateControl(TUIFullJsonEditorPanel editorPanel)
        {
            _editorPanel = editorPanel;

            // Check for special string types
            if (Value.StartsWith("button:"))
            {
                var buttonText = Value.Substring(7); // Remove "button:" prefix
                var button = new Button(buttonText)
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill()
                };
                button.Clicked += () =>
                {
                    if (editorPanel.AppMode)
                    {
                        TUIFullJsonEditorPanel.Log($"[App Mode] Button '{buttonText}' clicked");
                    }
                };
                return button;
            }

            // Check for date syntax or ISO8601 format
            if (Value.StartsWith("date:") || IsISO8601Date(Value))
            {
                string dateValue = Value.StartsWith("date:") ? Value.Substring(5) : Value;
                if (DateTime.TryParse(dateValue, out DateTime dateTime))
                {
                    var container = new View()
                    {
                        X = 0,
                        Y = 0,
                        Width = Dim.Fill(),
                        Height = Dim.Auto()
                    };

                    // Add the date field
                    var dateField = new TextField(dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"))
                    {
                        X = 0,
                        Y = 0,
                        Width = Dim.Fill()
                    };

                    // In app mode, make the field read-only but still clickable to log
                    if (editorPanel.AppMode)
                    {
                        dateField.ReadOnly = true;
                        dateField.MouseClick += (sender, e) =>
                        {
                            TUIFullJsonEditorPanel.Log($"[App Mode] Date '{dateValue}' clicked");
                        };
                    }

                    // Handle date changes
                    dateField.TextChanged += (sender, e) => {
                        if (DateTime.TryParse(dateField.Text.ToString(), out DateTime newDate))
                        {
                            Value = newDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                            RefreshParentContainer();
                        }
                    };
                    container.Add(dateField);

                    // Add buttons in a horizontal layout
                    var buttonContainer = new View()
                    {
                        X = 0,
                        Y = Pos.Bottom(dateField),
                        Width = Dim.Fill(),
                        Height = 1
                    };

                    // Add Set/Edit Image button
                    var imageButton = new Button("Set/Edit Image")
                    {
                        X = 0,
                        Y = 0,
                        Width = 15
                    };
                    imageButton.Clicked += (s, e) => UploadImageAsBase64(_editorPanel, (newValue) => {
                        Value = newValue;
                        RefreshParentContainer();
                    });
                    buttonContainer.Add(imageButton);

                    // Add "Add text instead" button
                    var textButton = new Button("Add text instead")
                    {
                        X = Pos.Right(imageButton) + 1,
                        Y = 0,
                        Width = 15
                    };
                    textButton.Clicked += (s, e) => {
                        Value = ""; // Reset to empty string
                        RefreshParentContainer();
                    };
                    buttonContainer.Add(textButton);

                    container.Add(buttonContainer);
                    container.Height = dateField.Frame.Height + buttonContainer.Frame.Height;

                    return container;
                }
            }

            // Check for images (base64, URLs, or file extensions)
            bool isImage = IsBase64Image(Value, out TUIImageType imageType) || IsImageUrl(Value) || HasImageExtension(Value);

            // Create a container for the text field and buttons
            var textContainer = new View()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Auto()
            };

            // Add text field
            var textField = new TextField(Value ?? "")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill()
            };
            textField.TextChanged += (sender, e) => {
                Value = textField.Text.ToString();
                // Don't refresh on every text change, only when needed
            };
            textContainer.Add(textField);

            // Add buttons in a horizontal layout
            var buttonContainer = new View()
            {
                X = 0,
                Y = Pos.Bottom(textField),
                Width = Dim.Fill(),
                Height = 1
            };

            // Add Set/Edit Image button for all text fields
            var imageButton = new Button("Set/Edit Image")
            {
                X = 0,
                Y = 0,
                Width = 15
            };
            imageButton.Clicked += (s, e) => UploadImageAsBase64(_editorPanel, (newValue) => {
                Value = newValue;
                RefreshParentContainer();
            });
            buttonContainer.Add(imageButton);

            // Add Set Date button for all text fields
            var dateButton = new Button("Set Date")
            {
                X = Pos.Right(imageButton) + 1,
                Y = 0,
                Width = 10
            };
            dateButton.Clicked += (s, e) => SetDate();
            buttonContainer.Add(dateButton);

            textContainer.Add(buttonContainer);
            textContainer.Height = textField.Frame.Height + buttonContainer.Frame.Height;

            // If this is already an image, show the image info
            if (isImage)
            {
                var imageContainer = new View()
                {
                    X = 0,
                    Y = Pos.Bottom(textContainer),
                    Width = Dim.Fill(),
                    Height = Dim.Auto()
                };

                if (IsBase64Image(Value, out TUIImageType type) && type != TUIImageType.Unknown)
                {
                    byte[] imageBytes = Convert.FromBase64String(Value);
                    var imageLabel = new Label($"Base64 Image: {imageBytes.Length} bytes, Type: {type}")
                    {
                        X = 0,
                        Y = 0,
                        Width = Dim.Fill()
                    };
                    imageContainer.Add(imageLabel);

                    // In app mode, make the image clickable to log
                    if (editorPanel.AppMode)
                    {
                        imageLabel.MouseClick += (sender, e) =>
                        {
                            TUIFullJsonEditorPanel.Log($"[App Mode] Image clicked");
                        };
                    }
                }
                else if (IsImageUrl(Value))
                {
                    // For URLs, show the URL as a label
                    var urlLabel = new Label($"URL: {Value}")
                    {
                        X = 0,
                        Y = 0,
                        Width = Dim.Fill()
                    };
                    imageContainer.Add(urlLabel);
                }

                // Add "Add text instead" button for images
                var imageButtonContainer = new View()
                {
                    X = 0,
                    Y = Pos.Bottom(imageContainer.Subviews.LastOrDefault() ?? imageContainer),
                    Width = Dim.Fill(),
                    Height = 1
                };

                var textInsteadButton = new Button("Add text instead")
                {
                    X = 0,
                    Y = 0,
                    Width = 15
                };
                textInsteadButton.Clicked += (s, e) => {
                    Value = ""; // Reset to empty string
                    RefreshParentContainer();
                };
                imageButtonContainer.Add(textInsteadButton);

                imageContainer.Add(imageButtonContainer);
                imageContainer.Height = imageContainer.Subviews.Sum(v => v.Frame.Height);

                return imageContainer;
            }

            return textContainer;
        }

        // Update the RefreshParentContainer method to work with the correct structure:
        private void RefreshParentContainer()
        {
            if (_parentContainer != null && _editorPanel != null)
            {
                if (_parentKey != null)
                {
                    // We're in an object, refresh the specific property
                    var objectNode = FindParentObjectNode();
                    if (objectNode != null)
                    {
                        objectNode.RebuildPropertyControl(_editorPanel, _parentKey);
                    }
                }
                else if (_parentIndex >= 0)
                {
                    // We're in an array, refresh the specific item
                    var arrayNode = FindParentArrayNode();
                    if (arrayNode != null)
                    {
                        arrayNode.RebuildItemControl(_editorPanel, _parentIndex);
                    }
                }
            }
        }

        // Helper method to find the parent object node
        private TUIJsonObjectNode FindParentObjectNode()
        {
            // Navigate up the visual tree to find the TUIJsonObjectNode that contains this string node
            var current = _parentContainer;
            while (current != null)
            {
                // Check if current control is a View with a property control
                if (current is View view && view.Subviews.Count > 0)
                {
                    var firstItem = view.Subviews[0];
                    if (firstItem is Label label && label.Text == _parentKey)
                    {
                        // Found the parent object, return it
                        return _editorPanel.RootNode as TUIJsonObjectNode;
                    }
                }

                // Move up to the parent
                current = current.SuperView;
            }

            return null;
        }

        // Helper method to find the parent array node
        private TUIJsonArrayNode FindParentArrayNode()
        {
            // Navigate up the visual tree to find the TUIJsonArrayNode that contains this string node
            var current = _parentContainer;
            while (current != null)
            {
                // Check if current control is a View with an item control
                if (current is View view && view.Subviews.Count > 0)
                {
                    var firstItem = view.Subviews[0];
                    if (firstItem is Label label && label.Text == $"[{_parentIndex}]")
                    {
                        // Found the parent array, return it
                        return _editorPanel.RootNode as TUIJsonArrayNode;
                    }
                }

                // Move up to the parent
                current = current.SuperView;
            }

            return null;
        }

        // Add this method to TUIJsonStringNode class to handle date setting:
        private void SetDate()
        {
            // Create a dialog to set a date
            var dialog = new Dialog()
            {
                Title = "Set Date",
                Width = Dim.Percent(50),
                Height = Dim.Percent(50)
            };

            var dateField = new TextField(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"))
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill()
            };

            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = Pos.Bottom(dateField) + 1
            };
            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 10,
                Y = Pos.Bottom(dateField) + 1
            };

            // Add labels
            dialog.Add(
                new Label("Enter a date (yyyy-MM-ddTHH:mm:ssZ):") { X = 0, Y = 0 },
                dateField,
                okButton,
                cancelButton
            );

            // Set up event handlers
            okButton.Clicked += () => {
                if (DateTime.TryParse(dateField.Text.ToString(), out DateTime date))
                {
                    Value = date.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    RefreshParentContainer();
                    Application.RequestStop();
                }
                else
                {
                    MessageBox.ErrorQuery("Error", "Invalid date format", "OK");
                }
            };

            cancelButton.Clicked += () => {
                Application.RequestStop();
            };

            Application.Run(dialog);
        }

        public string ToJson()
        {
            return $"\"{Value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        }

        public void Validate(List<string> errors)
        {
            // String values are always valid
        }

        private bool IsISO8601Date(string value)
        {
            // Basic ISO8601 pattern - matches dates like "2023-04-25T14:30:00Z" or "2023-04-25"
            return Regex.IsMatch(value, @"^\d{4}-\d{2}-\d{2}(T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})?)?$");
        }

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

                // Check for specific image URL patterns
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
                TUIFullJsonEditorPanel.Log($"[IsImageUrl] Error checking URL '{value}': {ex.Message}");
                return false;
            }
        }

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

        private bool IsBase64Image(string value, out TUIImageType imageType)
        {
            imageType = TUIImageType.Unknown;

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
                    imageType = TUIImageType.Jpeg;
                    return true;
                }

                // PNG signature: 89 50 4E 47
                if (bytes.Length >= 8 &&
                    bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                {
                    imageType = TUIImageType.Png;
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

        public void UploadImageAsBase64(TUIFullJsonEditorPanel editorPanel, Action<string> onImageUploaded)
        {
            // Create a dialog for file selection
            var dialog = new Dialog()
            {
                Title = "Enter Image Path",
                Width = Dim.Percent(60),
                Height = Dim.Percent(30)
            };

            var pathField = new TextField("")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill()
            };

            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = Pos.Bottom(pathField) + 1
            };
            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 10,
                Y = Pos.Bottom(pathField) + 1
            };

            // Add labels
            dialog.Add(
                new Label("Enter path to image file:") { X = 0, Y = 0 },
                pathField,
                okButton,
                cancelButton
            );

            // Set up event handlers
            okButton.Clicked += () => {
                try
                {
                    string path = pathField.Text.ToString();
                    if (File.Exists(path))
                    {
                        byte[] imageBytes = File.ReadAllBytes(path);
                        string base64 = Convert.ToBase64String(imageBytes);

                        // Detect image type from file extension
                        TUIImageType imageType = TUIImageType.Unknown;
                        string extension = Path.GetExtension(path).ToLowerInvariant();
                        if (extension == ".png")
                            imageType = TUIImageType.Png;
                        else if (extension == ".jpg" || extension == ".jpeg")
                            imageType = TUIImageType.Jpeg;

                        TUIFullJsonEditorPanel.Log($"[Upload Image] Selected image: {path}, Type: {imageType}, Size: {imageBytes.Length} bytes");

                        // Call the callback with the new base64 value
                        onImageUploaded?.Invoke(base64);

                        TUIFullJsonEditorPanel.Log($"[Upload Image] Image updated successfully");
                        Application.RequestStop();
                    }
                    else
                    {
                        MessageBox.ErrorQuery("Error", "File not found", "OK");
                    }
                }
                catch (Exception ex)
                {
                    TUIFullJsonEditorPanel.Log($"[Upload Image] Error: {ex.Message}");
                    MessageBox.ErrorQuery("Error", $"Error loading image: {ex.Message}", "OK");
                }
            };

            cancelButton.Clicked += () => {
                TUIFullJsonEditorPanel.Log("[Upload Image] User cancelled image selection");
                Application.RequestStop();
            };

            Application.Run(dialog);
        }
    }

    /// <summary>
    /// Represents a JSON number node.
    /// </summary>
    public class TUIJsonNumberNode : ITUIJsonNode
    {
        public object Value { get; set; }

        public View CreateControl(TUIFullJsonEditorPanel editorPanel)
        {
            var textField = new TextField(Value.ToString())
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill()
            };

            textField.TextChanged += (sender, e) => {
                if (Value is long || (Value is double && double.TryParse(textField.Text.ToString(), out double d)))
                {
                    if (Value is long)
                    {
                        if (long.TryParse(textField.Text.ToString(), out long l))
                            Value = l;
                    }
                    else
                    {
                        if (double.TryParse(textField.Text.ToString(), out double _d))
                            Value = _d;
                    }
                }
                else
                {
                    // Try to parse as long first, then double
                    if (long.TryParse(textField.Text.ToString(), out long l))
                        Value = l;
                    else if (double.TryParse(textField.Text.ToString(), out double _d))
                        Value = _d;
                }
            };

            return textField;
        }

        public string ToJson()
        {
            return Value.ToString();
        }

        public void Validate(List<string> errors)
        {
            // Number values are always valid
        }
    }

    /// <summary>
    /// Represents a JSON boolean node.
    /// </summary>
    public class TUIJsonBooleanNode : ITUIJsonNode
    {
        public bool Value { get; set; }

        public View CreateControl(TUIFullJsonEditorPanel editorPanel)
        {
            var checkBox = new CheckBox("")
            {
                X = 0,
                Y = 0,
                Checked = Value
            };

            checkBox.Toggled += (sender, e) => {
                Value = checkBox.Checked;
            };

            return checkBox;
        }

        public string ToJson()
        {
            return Value ? "true" : "false";
        }

        public void Validate(List<string> errors)
        {
            // Boolean values are always valid
        }
    }

    /// <summary>
    /// Represents a JSON null node.
    /// </summary>
    public class TUIJsonNullNode : ITUIJsonNode
    {
        public View CreateControl(TUIFullJsonEditorPanel editorPanel)
        {
            return new Label("null")
            {
                X = 0,
                Y = 0,
                ColorScheme = Colors.Base,
                TextColor = Color.Gray
            };
        }

        public string ToJson()
        {
            return "null";
        }

        public void Validate(List<string> errors)
        {
            // Null values are always valid
        }
    }

    /// <summary>
    /// Represents the type of an image.
    /// </summary>
    public enum TUIImageType
    {
        Unknown,
        Jpeg,
        Png
    }
}