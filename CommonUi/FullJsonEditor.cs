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
        private bool _showUploadButton = true;
        private IJsonNode _rootNode;
        private StackLayout _rootLayout;

        // Add a logging delegate
        public static LogDelegate Log = Console.WriteLine;

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
        /// Constructs a FullJsonEditorPanel from a JSON string.
        /// </summary>
        public FullJsonEditorPanel(
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
                _rootNode = JsonNodeFactory.CreateFromJson(jsonString);
            }
            catch (JsonException ex)
            {
                // If JSON is invalid, start with an empty object
                Log($"[Init] Invalid JSON: {ex.Message}");
                _rootNode = new JsonObjectNode();
            }

            InitializeUI();
            Log("[Init] String constructor completed successfully");
        }

        /// <summary>
        /// Initializes the UI components.
        /// </summary>
        private void InitializeUI()
        {
            // Create the root layout with header
            _rootLayout = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };

            // Create header if needed
            if (_showHeader)
            {
                var header = CreateHeader();
                _rootLayout.Items.Add(new StackLayoutItem(header, HorizontalAlignment.Stretch));
            }

            // Create a container for the content
            var contentContainer = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };
            _rootLayout.Items.Add(new StackLayoutItem(contentContainer, HorizontalAlignment.Stretch, true));

            // Add the root node's control to the content
            var rootControl = _rootNode.CreateControl(this);
            contentContainer.Items.Add(new StackLayoutItem(rootControl, HorizontalAlignment.Stretch, true));

            this.Content = _rootLayout;

            // Set control states based on app mode
            UpdateControlStates();
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
                // Get the latest JSON from the root node
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
                Text = ToJson(),
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

                // Create the new root node
                _rootNode = JsonNodeFactory.CreateFromJson(json);

                // Rebuild the UI
                _rootLayout.Items.Clear();
                InitializeUI();

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
        /// Updates the enabled state of controls based on the AppMode setting.
        /// </summary>
        private void UpdateControlStates()
        {
            UpdateControlStatesRecursively(_rootLayout);
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
                if (AppMode && !(button.Text.StartsWith("button:") || button.Text.Contains("Add")))
                {
                    button.Enabled = false;
                }
                else
                {
                    button.Enabled = true;
                }
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
    public interface IJsonNode
    {
        /// <summary>
        /// Creates a control for this node.
        /// </summary>
        Control CreateControl(FullJsonEditorPanel editorPanel);

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
    public static class JsonNodeFactory
    {
        /// <summary>
        /// Creates a JSON node from a JSON string.
        /// </summary>
        public static IJsonNode CreateFromJson(string json)
        {
            var document = JsonDocument.Parse(json);
            return CreateFromJsonElement(document.RootElement);
        }

        /// <summary>
        /// Creates a JSON node from a JsonElement.
        /// </summary>
        public static IJsonNode CreateFromJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var objNode = new JsonObjectNode();
                    foreach (var property in element.EnumerateObject())
                    {
                        objNode.Properties[property.Name] = CreateFromJsonElement(property.Value);
                    }
                    return objNode;

                case JsonValueKind.Array:
                    var arrayNode = new JsonArrayNode();
                    foreach (var item in element.EnumerateArray())
                    {
                        arrayNode.Items.Add(CreateFromJsonElement(item));
                    }
                    return arrayNode;

                case JsonValueKind.String:
                    return new JsonStringNode { Value = element.GetString() };

                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long l))
                        return new JsonNumberNode { Value = l };
                    else
                        return new JsonNumberNode { Value = element.GetDouble() };

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return new JsonBooleanNode { Value = element.GetBoolean() };

                case JsonValueKind.Null:
                    return new JsonNullNode();

                default:
                    throw new NotSupportedException($"Unsupported JSON value kind: {element.ValueKind}");
            }
        }

        /// <summary>
        /// Creates a new JSON node of the specified type.
        /// </summary>
        public static IJsonNode CreateNewNode(string type, string value = null)
        {
            switch (type)
            {
                case "String":
                    return new JsonStringNode { Value = value ?? "" };

                case "Number":
                    if (long.TryParse(value, out long l))
                        return new JsonNumberNode { Value = l };
                    else if (double.TryParse(value, out double d))
                        return new JsonNumberNode { Value = d };
                    else
                        return new JsonNumberNode { Value = 0 };

                case "Boolean":
                    if (bool.TryParse(value, out bool b))
                        return new JsonBooleanNode { Value = b };
                    else
                        return new JsonBooleanNode { Value = false };

                case "Object":
                    return new JsonObjectNode();

                case "Array":
                    return new JsonArrayNode();

                case "Null":
                    return new JsonNullNode();

                case "Date":
                    if (DateTime.TryParse(value, out DateTime date))
                        return new JsonStringNode { Value = date.ToString("yyyy-MM-ddTHH:mm:ssZ") };
                    else
                        return new JsonStringNode { Value = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ") };

                case "Image":
                    if (IsBase64Image(value, out ImageType imageType) && imageType != ImageType.Unknown)
                        return new JsonStringNode { Value = value };
                    else
                        return new JsonStringNode { Value = "" };

                default:
                    throw new ArgumentException($"Unknown node type: {type}");
            }
        }

        // Add this helper method to JsonNodeFactory class:
        private static bool IsBase64Image(string value, out ImageType imageType)
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
    public class JsonObjectNode : IJsonNode
    {
        public Dictionary<string, IJsonNode> Properties { get; set; } = new Dictionary<string, IJsonNode>();

        public Control CreateControl(FullJsonEditorPanel editorPanel)
        {
            var container = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };

            // Add a button to add new properties
            var addButton = new Button { Text = "Add Property" };
            addButton.Click += (sender, e) => AddProperty(editorPanel, container);
            container.Items.Add(new StackLayoutItem(addButton, HorizontalAlignment.Left));

            // Add controls for each property
            foreach (var kvp in Properties)
            {
                var propertyControl = CreatePropertyControl(editorPanel, kvp.Key, kvp.Value, container);
                container.Items.Add(new StackLayoutItem(propertyControl, HorizontalAlignment.Stretch));
            }

            return container;
        }

        private Control CreatePropertyControl(FullJsonEditorPanel editorPanel, string key, IJsonNode value, StackLayout container)
        {
            var row = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5 };

            // Property name label
            var nameLabel = new Label { Text = key, Width = 150 };
            row.Items.Add(new StackLayoutItem(nameLabel));

            // Property value control
            var valueControl = value.CreateControl(editorPanel);
            row.Items.Add(new StackLayoutItem(valueControl, HorizontalAlignment.Stretch, true));

            // Delete button
            var deleteButton = new Button { Text = "×", Width = 25 };
            deleteButton.Click += (sender, e) => DeleteProperty(editorPanel, key, container);
            row.Items.Add(new StackLayoutItem(deleteButton));

            return row;
        }


        private void AddProperty(FullJsonEditorPanel editorPanel, StackLayout container)
        {
            // Create a dialog for adding a new property
            var dialog = new Dialog<string>
            {
                Title = "Add New Property",
                ClientSize = new Size(300, 250)
            };

            var propertyNameTextBox = new TextBox { ID = "propertyName" };
            var propertyTypeDropDown = new DropDown { ID = "propertyType", Items = { "String", "Number", "Boolean", "Date", "Image", "Object", "Array", "Null" } };
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
            new Label { Text = "Property Value (optional):" },
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
                        propertyValueTextBox.PlaceholderText = "Enter a date (e.g., 2023-04-25T14:30:00Z)";
                        propertyValueTextBox.Enabled = true;
                        break;
                    case "Image":
                        propertyValueTextBox.PlaceholderText = "Enter a base64 encoded image or leave empty to upload";
                        propertyValueTextBox.Enabled = true;
                        break;
                    case "Null":
                        propertyValueTextBox.PlaceholderText = "Value will be null";
                        propertyValueTextBox.Enabled = false;
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

            okButton.Click += (sender, e) => {
                var name = propertyNameTextBox.Text;
                var type = propertyTypeDropDown.SelectedKey;
                var value = propertyValueTextBox.Text;

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
                    dialog.Close($"{name}|{type}|{value}");
                }
            };

            cancelButton.Click += (sender, e) => {
                dialog.Close(null);
            };

            // Initialize the placeholder text
            propertyTypeDropDown.SelectedIndex = 0; // Select "String" by default

            // Show the dialog
            var result = dialog.ShowModal(editorPanel);
            if (!string.IsNullOrEmpty(result))
            {
                var parts = result.Split('|');
                var name = parts[0];
                var type = parts[1];
                var value = parts.Length > 2 ? parts[2] : null;

                // Create the new node
                var newNode = JsonNodeFactory.CreateNewNode(type, value);

                // If it's an Image type with no value, open the image upload dialog
                if (type == "Image" && string.IsNullOrEmpty(value))
                {
                    var stringNode = newNode as JsonStringNode;
                    stringNode.UploadImageAsBase64(editorPanel, (newValue) => stringNode.Value = newValue);
                }

                // Add the property
                Properties[name] = newNode;

                // Rebuild the UI
                RebuildContainer(editorPanel, container);
            }
        }


        private void DeleteProperty(FullJsonEditorPanel editorPanel, string key, StackLayout container)
        {
            // Confirm deletion
            var result = MessageBox.Show(
                editorPanel,
                $"Are you sure you want to delete property '{key}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxType.Question
            );

            if (result == DialogResult.Yes)
            {
                // Remove the property
                Properties.Remove(key);

                // Rebuild the UI
                RebuildContainer(editorPanel, container);
            }
        }



        private void RebuildContainer(FullJsonEditorPanel editorPanel, StackLayout container)
        {
            // Clear the container except for the add button
            var addButton = container.Items[0].Control;
            container.Items.Clear();

            // Re-add the add button
            container.Items.Add(new StackLayoutItem(addButton, HorizontalAlignment.Left));

            // Re-add all property controls
            foreach (var kvp in Properties)
            {
                var propertyControl = CreatePropertyControl(editorPanel, kvp.Key, kvp.Value, container);
                container.Items.Add(new StackLayoutItem(propertyControl, HorizontalAlignment.Stretch));
            }
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
    public class JsonArrayNode : IJsonNode
    {
        public List<IJsonNode> Items { get; set; } = new List<IJsonNode>();

        public Control CreateControl(FullJsonEditorPanel editorPanel)
        {
            var container = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };

            // Add a button to add new items
            var addButton = new Button { Text = "Add Item" };
            addButton.Click += (sender, e) => AddItem(editorPanel, container);
            container.Items.Add(new StackLayoutItem(addButton, HorizontalAlignment.Left));

            // Add controls for each item
            for (int i = 0; i < Items.Count; i++)
            {
                var itemControl = CreateItemControl(editorPanel, i, Items[i], container);
                container.Items.Add(new StackLayoutItem(itemControl, HorizontalAlignment.Stretch));
            }

            return container;
        }


        private Control CreateItemControl(FullJsonEditorPanel editorPanel, int index, IJsonNode item, StackLayout container)
        {
            var row = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5 };

            // Item index label
            var indexLabel = new Label { Text = $"[{index}]", Width = 50 };
            row.Items.Add(new StackLayoutItem(indexLabel));

            // Create a container for the value control
            var valueContainer = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };

            // Item value control
            var valueControl = item.CreateControl(editorPanel);
            valueContainer.Items.Add(new StackLayoutItem(valueControl, HorizontalAlignment.Stretch, true));

            // Set up the callback for string nodes
            if (item is JsonStringNode stringNode)
            {
                stringNode.OnControlNeedsRebuild = (node) => {
                    // Find the index of the current item control
                    int itemIndex = -1;
                    for (int i = 0; i < container.Items.Count; i++)
                    {
                        if (container.Items[i].Control == row)
                        {
                            itemIndex = i;
                            break;
                        }
                    }

                    if (itemIndex >= 0)
                    {
                        // Recreate the item control
                        var newItemControl = CreateItemControl(editorPanel, index, item, container);

                        // Replace the old control with the new one
                        container.Items.RemoveAt(itemIndex);
                        container.Items.Insert(itemIndex, new StackLayoutItem(newItemControl, HorizontalAlignment.Stretch));
                    }
                };
            }

            row.Items.Add(new StackLayoutItem(valueContainer, HorizontalAlignment.Stretch, true));

            // Delete button
            var deleteButton = new Button { Text = "×", Width = 25 };
            deleteButton.Click += (sender, e) => DeleteItem(editorPanel, index, container);
            row.Items.Add(new StackLayoutItem(deleteButton));

            return row;
        }


        private void AddItem(FullJsonEditorPanel editorPanel, StackLayout container)
        {
            // Create a dialog for adding a new item
            var dialog = new Dialog<string>
            {
                Title = "Add New Array Item",
                ClientSize = new Size(300, 250)
            };

            var itemTypeDropDown = new DropDown { ID = "itemType", Items = { "String", "Number", "Boolean", "Date", "Image", "Object", "Array", "Null" } };
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
            new Label { Text = "Item Value (optional):" },
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
                        itemValueTextBox.PlaceholderText = "Enter a date (e.g., 2023-04-25T14:30:00Z)";
                        itemValueTextBox.Enabled = true;
                        break;
                    case "Image":
                        itemValueTextBox.PlaceholderText = "Enter a base64 encoded image or leave empty to upload";
                        itemValueTextBox.Enabled = true;
                        break;
                    case "Null":
                        itemValueTextBox.PlaceholderText = "Value will be null";
                        itemValueTextBox.Enabled = false;
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

            okButton.Click += (sender, e) => {
                var type = itemTypeDropDown.SelectedKey;
                var value = itemValueTextBox.Text;

                dialog.Close($"{type}|{value}");
            };

            cancelButton.Click += (sender, e) => {
                dialog.Close(null);
            };

            // Initialize the placeholder text
            itemTypeDropDown.SelectedIndex = 0; // Select "String" by default

            // Show the dialog
            var result = dialog.ShowModal(editorPanel);
            if (!string.IsNullOrEmpty(result))
            {
                var parts = result.Split('|');
                var type = parts[0];
                var value = parts.Length > 1 ? parts[1] : null;

                // Create the new node
                var newNode = JsonNodeFactory.CreateNewNode(type, value);

                // If it's an Image type with no value, open the image upload dialog
                if (type == "Image" && string.IsNullOrEmpty(value))
                {
                    var stringNode = newNode as JsonStringNode;
                    stringNode.UploadImageAsBase64(editorPanel, (newValue) => stringNode.Value = newValue);
                }

                // Add the item
                Items.Add(newNode);

                // Rebuild the UI
                RebuildContainer(editorPanel, container);
            }
        }


        private void DeleteItem(FullJsonEditorPanel editorPanel, int index, StackLayout container)
        {
            // Confirm deletion
            var result = MessageBox.Show(
                editorPanel,
                $"Are you sure you want to delete item at index {index}?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxType.Question
            );

            if (result == DialogResult.Yes)
            {
                // Remove the item
                Items.RemoveAt(index);

                // Rebuild the UI
                RebuildContainer(editorPanel, container);
            }
        }

        private void RebuildContainer(FullJsonEditorPanel editorPanel, StackLayout container)
        {
            // Clear the container except for the add button
            var addButton = container.Items[0].Control;
            container.Items.Clear();

            // Re-add the add button
            container.Items.Add(new StackLayoutItem(addButton, HorizontalAlignment.Left));

            // Re-add all item controls
            for (int i = 0; i < Items.Count; i++)
            {
                var itemControl = CreateItemControl(editorPanel, i, Items[i], container);
                container.Items.Add(new StackLayoutItem(itemControl, HorizontalAlignment.Stretch));
            }
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
    public class JsonStringNode : IJsonNode
    {
        public string Value { get; set; }
        public Action<JsonStringNode> OnControlNeedsRebuild { get; set; }

        public Control CreateControl(FullJsonEditorPanel editorPanel)
        {
            // Check for special string types
            if (Value.StartsWith("button:"))
            {
                var buttonText = Value.Substring(7); // Remove "button:" prefix
                var button = new Button { Text = buttonText };
                button.Click += (sender, e) =>
                {
                    if (editorPanel.AppMode)
                    {
                        FullJsonEditorPanel.Log($"[App Mode] Button '{buttonText}' clicked");
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
                    var container = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };

                    // Add the date picker
                    var datePicker = new DateTimePicker
                    {
                        Value = dateTime,
                        Mode = DateTimePickerMode.DateTime
                    };

                    // In app mode, make the picker read-only but still clickable to log
                    if (editorPanel.AppMode)
                    {
                        datePicker.Enabled = false;
                        datePicker.MouseDoubleClick += (sender, e) =>
                        {
                            FullJsonEditorPanel.Log($"[App Mode] Date '{dateValue}' clicked");
                        };
                    }

                    // Handle date changes
                    datePicker.ValueChanged += (sender, e) => {
                        Value = datePicker.Value?.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        // Notify that the control needs to be rebuilt
                        OnControlNeedsRebuild?.Invoke(this);
                    };
                    container.Items.Add(new StackLayoutItem(datePicker, HorizontalAlignment.Stretch, true));

                    // Add buttons in a horizontal layout
                    var _buttonContainer = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5 };

                    // Add Set/Edit Image button
                    var _imageButton = new Button { Text = "Set/Edit Image" };
                    _imageButton.Click += (s, e) => UploadImageAsBase64(editorPanel, (newValue) => {
                        Value = newValue;
                        // Notify that the control needs to be rebuilt
                        OnControlNeedsRebuild?.Invoke(this);
                    });
                    _buttonContainer.Items.Add(new StackLayoutItem(_imageButton));

                    // Add "Add text instead" button
                    var textButton = new Button { Text = "Add text instead" };
                    textButton.Click += (s, e) => {
                        Value = ""; // Reset to empty string
                                    // Notify that the control needs to be rebuilt
                        OnControlNeedsRebuild?.Invoke(this);
                    };
                    _buttonContainer.Items.Add(new StackLayoutItem(textButton));

                    container.Items.Add(new StackLayoutItem(_buttonContainer, HorizontalAlignment.Left));

                    return container;
                }
            }
            // Check for images (base64, URLs, or file extensions)
            bool isImage = IsBase64Image(Value, out ImageType imageType) || IsImageUrl(Value) || HasImageExtension(Value);

            // If this is an image, create an image container
            if (isImage)
            {
                var imageContainer = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };

                if (IsBase64Image(Value, out ImageType type) && type != ImageType.Unknown)
                {
                    try
                    {
                        byte[] imageBytes = Convert.FromBase64String(Value);
                        var image = new Bitmap(imageBytes);
                        var imageView = new ImageView
                        {
                            Image = image,
                            Width = 200,
                            Height = 150
                        };

                        // In app mode, make the image clickable to log
                        if (editorPanel.AppMode)
                        {
                            imageView.Cursor = Cursors.Pointer;
                            imageView.MouseDown += (sender, e) =>
                            {
                                FullJsonEditorPanel.Log($"[App Mode] Image clicked");
                            };
                        }

                        imageContainer.Items.Add(new StackLayoutItem(imageView, HorizontalAlignment.Center));
                    }
                    catch (Exception ex)
                    {
                        FullJsonEditorPanel.Log($"[JsonStringNode] Error loading image: {ex.Message}");
                        // Fall back to showing just the text box and buttons
                    }
                }
                else if (IsImageUrl(Value))
                {
                    // For URLs, show the URL as a clickable link
                    var urlLabel = new Label
                    {
                        Text = $"URL: {Value}",
                        TextColor = Colors.Blue,
                        Cursor = Cursors.Pointer,
                        Wrap = WrapMode.Word
                    };

                    urlLabel.MouseDoubleClick += (sender, e) => {
                        try
                        {
                            Process.Start(new Uri(Value).ToString());
                        }
                        catch (Exception ex)
                        {
                            FullJsonEditorPanel.Log($"[JsonStringNode] Error opening URL '{Value}': {ex.Message}");
                        }
                    };

                    imageContainer.Items.Add(new StackLayoutItem(urlLabel, HorizontalAlignment.Stretch, true));
                }

                // Add buttons in a horizontal layout
                var imageButtonContainer = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5 };

                // Add Set/Edit Image button
                var _imageButton = new Button { Text = "Set/Edit Image" };
                _imageButton.Click += (s, e) => UploadImageAsBase64(editorPanel, (newValue) => {
                    Value = newValue;
                    // Notify that the control needs to be rebuilt
                    OnControlNeedsRebuild?.Invoke(this);
                });
                imageButtonContainer.Items.Add(new StackLayoutItem(_imageButton));

                // Add "Add text instead" button for images
                var textInsteadButton = new Button { Text = "Add text instead" };
                textInsteadButton.Click += (s, e) => {
                    Value = ""; // Reset to empty string
                                // Notify that the control needs to be rebuilt
                    OnControlNeedsRebuild?.Invoke(this);
                };
                imageButtonContainer.Items.Add(new StackLayoutItem(textInsteadButton));

                imageContainer.Items.Add(new StackLayoutItem(imageButtonContainer, HorizontalAlignment.Left));

                return imageContainer;
            }

            // Default to a text field
            var textContainer = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };

            // Add text box
            var textBox = new TextBox { Text = Value };
            textBox.TextChanged += (sender, e) => {
                Value = textBox.Text;
                // Notify that the control needs to be rebuilt
                OnControlNeedsRebuild?.Invoke(this);
            };
            textContainer.Items.Add(new StackLayoutItem(textBox, HorizontalAlignment.Stretch, true));

            // Add buttons in a horizontal layout
            var buttonContainer = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5 };

            // Add Set/Edit Image button for all text fields
            var imageButton = new Button { Text = "Set/Edit Image" };
            imageButton.Click += (s, e) => UploadImageAsBase64(editorPanel, (newValue) => {
                Value = newValue;
                // Notify that the control needs to be rebuilt
                OnControlNeedsRebuild?.Invoke(this);
            });
            buttonContainer.Items.Add(new StackLayoutItem(imageButton));

            // Add Set Date button for all text fields
            var dateButton = new Button { Text = "Set Date" };
            dateButton.Click += (s, e) => {
                SetDate(editorPanel, (newDate) => {
                    Value = newDate;
                    // Notify that the control needs to be rebuilt
                    OnControlNeedsRebuild?.Invoke(this);
                });
            };
            buttonContainer.Items.Add(new StackLayoutItem(dateButton));

            textContainer.Items.Add(new StackLayoutItem(buttonContainer, HorizontalAlignment.Left));

            return textContainer;
        }


        // Update the RefreshParentContainer method to work with the correct structure:

        private void RefreshParentContainer(FullJsonEditorPanel editorPanel, Control container)
        {
            // Find the parent of the container
            var parent = FindParentContainer(container);
            if (parent != null)
            {
                // Store the current scroll position if possible
                var scrollable = parent as Scrollable;
                var scrollPosition = scrollable?.ScrollPosition ?? Point.Empty;

                // Force a refresh by rebuilding the parent
                if (parent is StackLayout parentLayout)
                {
                    // Get the index of the container in the parent
                    int index = -1;
                    for (int i = 0; i < parentLayout.Items.Count; i++)
                    {
                        if (parentLayout.Items[i].Control == container)
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index >= 0)
                    {
                        // Remove and re-add the container to force a refresh
                        var item = parentLayout.Items[index];
                        parentLayout.Items.RemoveAt(index);
                        parentLayout.Items.Insert(index, item);
                    }
                }

                // Restore the scroll position
                if (scrollable != null && scrollPosition != Point.Empty)
                {
                    scrollable.ScrollPosition = scrollPosition;
                }
            }
        }

        // Add this helper method to find the parent container:

        // Add this helper method to find the parent container:

        private Control FindParentContainer(Control control)
        {
            // Try to find the parent by traversing up the visual tree
            var parent = control.Parent;

            // Continue traversing up until we find a StackLayout or Panel
            while (parent != null && !(parent is StackLayout) && !(parent is Panel))
            {
                parent = parent.Parent;
            }

            return parent;
        }





        // Add this method to JsonStringNode class to handle date setting:


        private void SetDate(FullJsonEditorPanel editorPanel, Action<string> onDateSet)
        {
            // Create a dialog to set a date
            var dialog = new Dialog
            {
                Title = "Set Date",
                ClientSize = new Size(300, 200)
            };

            var datePicker = new DateTimePicker { Mode = DateTimePickerMode.DateTime };
            var okButton = new Button { Text = "OK" };
            var cancelButton = new Button { Text = "Cancel" };

            // Create the layout
            var layout = new StackLayout
            {
                Padding = 10,
                Spacing = 5,
                Items =
            {
                new Label { Text = "Select a date:" },
                new StackLayoutItem(datePicker, HorizontalAlignment.Stretch),
                new StackLayoutItem(new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                    Items =
                    {
                        new StackLayoutItem(okButton),
                        new StackLayoutItem(cancelButton)
                    }
                }, HorizontalAlignment.Right)
            }
            };

            dialog.Content = layout;

            // Set up event handlers
            okButton.Click += (sender, e) => {
                if (datePicker.Value.HasValue)
                {
                    onDateSet(datePicker.Value.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    dialog.Close();
                }
            };

            cancelButton.Click += (sender, e) => {
                dialog.Close();
            };

            // Show the dialog
            dialog.ShowModal(editorPanel);
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
                FullJsonEditorPanel.Log($"[IsImageUrl] Error checking URL '{value}': {ex.Message}");
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

        public void UploadImageAsBase64(FullJsonEditorPanel editorPanel, Action<string> onImageUploaded)
        {
            var fileDialog = new OpenFileDialog
            {
                Title = "Select an image",
                Filters =
            {
                new FileFilter("Image Files (*.png;*.jpg;*.jpeg;*.gif;*.webp)", ".png", ".jpg", ".jpeg", ".gif", ".webp")
            }
            };

            if (fileDialog.ShowDialog(editorPanel) == DialogResult.Ok)
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

                    FullJsonEditorPanel.Log($"[Upload Image] Selected image: {fileDialog.FileName}, Type: {imageType}, Size: {imageBytes.Length} bytes");

                    // Call the callback with the new base64 value
                    onImageUploaded(base64);

                    FullJsonEditorPanel.Log($"[Upload Image] Image updated successfully");
                }
                catch (Exception ex)
                {
                    FullJsonEditorPanel.Log($"[Upload Image] Error: {ex.Message}");
                    MessageBox.Show(editorPanel, $"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                }
            }
            else
            {
                FullJsonEditorPanel.Log("[Upload Image] User cancelled image selection");
            }
        }

    }

    /// <summary>
    /// Represents a JSON number node.
    /// </summary>
    public class JsonNumberNode : IJsonNode
    {
        public object Value { get; set; }

        public Control CreateControl(FullJsonEditorPanel editorPanel)
        {
            var textBox = new TextBox { Text = Value.ToString() };
            textBox.TextChanged += (sender, e) => {
                if (Value is long || (Value is double && double.TryParse(textBox.Text, out double d)))
                {
                    if (Value is long)
                    {
                        if (long.TryParse(textBox.Text, out long l))
                            Value = l;
                    }
                    else
                    {
                        if (double.TryParse(textBox.Text, out double _d))
                            Value = _d;
                    }
                }
                else
                {
                    // Try to parse as long first, then double
                    if (long.TryParse(textBox.Text, out long l))
                        Value = l;
                    else if (double.TryParse(textBox.Text, out double _d))
                        Value = _d;
                }
            };
            return textBox;
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
    public class JsonBooleanNode : IJsonNode
    {
        public bool Value { get; set; }

        public Control CreateControl(FullJsonEditorPanel editorPanel)
        {
            var checkBox = new CheckBox { Checked = Value };
            checkBox.CheckedChanged += (sender, e) => {
                Value = checkBox.Checked ?? false;
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
    public class JsonNullNode : IJsonNode
    {
        public Control CreateControl(FullJsonEditorPanel editorPanel)
        {
            return new Label
            {
                Text = "null",
                TextColor = Colors.Gray
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
    public enum ImageType
    {
        Unknown,
        Jpeg,
        Png
    }
}