using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Eto.Drawing;
using Eto.Forms;

namespace JsonEditorExample
{
    public class FullJsonEditorPanel : Panel
    {
        private int fontSize = 12;
        private int cWidth = 200;
        private bool _appMode = false;
        private bool _showHeader = true;
        private string _currentJson = "{}"; // Initialize with empty object instead of empty string
        private StackLayout _rootLayout;
        private StackLayout _contentContainer;

        /// <summary>
        /// Gets or sets whether the panel is in app mode, which limits interactions to buttons and date fields.
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
        /// Gets or sets whether the header with Show/Load buttons is visible.
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
        /// For each node path (e.g. "Parent.Child" or "array[0]"), the original JSON type is stored.
        /// </summary>
        public Dictionary<string, Type> OriginalTypes { get; private set; } = new Dictionary<string, Type>();

        /// <summary>
        /// Constructs a FullJsonEditorPanel from a JSON object expressed as an IReadOnlyDictionary.
        /// </summary>
        public FullJsonEditorPanel(IReadOnlyDictionary<string, JsonElement> data, Orientation orientation, bool showHeader = true)
            : this(data, orientation, "", null, showHeader) { }

        /// <summary>
        /// Constructs a FullJsonEditorPanel from a JSON string.
        /// </summary>
        public FullJsonEditorPanel(string json, Orientation orientation, bool showHeader = true)
        {
            _showHeader = showHeader;

            // Ensure we have valid JSON
            if (string.IsNullOrWhiteSpace(json))
            {
                json = "{}";
            }

            try
            {
                // Validate the JSON
                var document = JsonDocument.Parse(json);
                _currentJson = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (JsonException)
            {
                // If the JSON is invalid, start with an empty object
                _currentJson = "{}";
            }

            var data = ConvertJsonToDictionary(_currentJson);
            Initialize(data, orientation, "", null, showHeader);
        }

        /// <summary>
        /// Private constructor that supports recursive calls.
        /// </summary>
        private FullJsonEditorPanel(
            IReadOnlyDictionary<string, JsonElement> data,
            Orientation orientation,
            string path,
            Dictionary<string, Type> originalTypes,
            bool showHeader = true
        )
        {
            _showHeader = showHeader;
            Initialize(data, orientation, path, originalTypes, showHeader);
        }

        /// <summary>
        /// Common initialization logic for all constructors.
        /// </summary>
        private void Initialize(
            IReadOnlyDictionary<string, JsonElement> data,
            Orientation orientation,
            string path,
            Dictionary<string, Type> originalTypes,
            bool showHeader = true
        )
        {
            if (originalTypes != null)
                OriginalTypes = originalTypes;
            else
                OriginalTypes = new Dictionary<string, Type>();

            Debug.WriteLine($"[Panel Build] Creating FullJsonEditorPanel at path \"{path}\" with orientation {orientation}");

            // Create the root layout with header
            _rootLayout = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };

            // Create header if needed
            if (_showHeader && string.IsNullOrEmpty(path)) // Only show header for the root panel
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
            if (_rootLayout != null && _rootLayout.Items.Count > 0)
            {
                if (_showHeader && (_rootLayout.Items[0].Control is StackLayout headerPanel))
                {
                    // Header is already visible
                    return;
                }
                else if (!_showHeader && (_rootLayout.Items[0].Control is StackLayout))
                {
                    // Remove the header
                    _rootLayout.Items.RemoveAt(0);
                }
                else if (_showHeader && !(_rootLayout.Items[0].Control is StackLayout))
                {
                    // Add the header
                    var header = CreateHeader();
                    _rootLayout.Items.Insert(0, new StackLayoutItem(header, HorizontalAlignment.Stretch));
                }
            }
        }

        /// <summary>
        /// Shows a dialog with the current JSON.
        /// </summary>
        private void ShowJsonDialog(object sender, EventArgs e)
        {
            var json = ToJson();

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
                Text = _currentJson,
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
                    UpdateJson(result);
                }
                catch (Exception ex)
                {
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
                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    json = "{}";
                }

                // Validate and normalize the JSON
                var document = JsonDocument.Parse(json);
                _currentJson = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });

                var data = ConvertJsonToDictionary(_currentJson);

                // Clear the current content
                _contentContainer.Items.Clear();

                // Clear the original types
                OriginalTypes.Clear();

                // Rebuild the UI with the new data
                BuildFromDictionary(data, _contentContainer, _contentContainer.Orientation, "");

                // Update control states based on app mode
                UpdateControlStates();
            }
            catch (JsonException ex)
            {
                throw new Exception($"Invalid JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts a JSON string to a dictionary using C#'s built-in System.Text.Json.
        /// </summary>
        private static IReadOnlyDictionary<string, JsonElement> ConvertJsonToDictionary(string json)
        {
            try
            {
                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    json = "{}";
                }

                // Parse and validate the JSON
                var document = JsonDocument.Parse(json);

                // If the root is not an object, wrap it in an object
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    json = $"{{\"value\": {json}}}";
                    document = JsonDocument.Parse(json);
                }

                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"[ConvertJsonToDictionary] Error parsing JSON: {ex.Message}");
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
            foreach (var kvp in dict)
            {
                string currentPath = string.IsNullOrEmpty(path) ? kvp.Key : $"{path}.{kvp.Key}";
                Debug.WriteLine($"[Build Dictionary] Node \"{currentPath}\" with ValueKind {kvp.Value.ValueKind}");

                // Create a label whose text is the translated key.
                var label = new Label
                {
                    Text = Translate(kvp.Key),
                    Tag = kvp.Key,
                    Font = Fonts.Monospace(fontSize),
                };

                // Create the control for the value.
                Control valueControl = BuildControlForValue(kvp.Value, orientation, currentPath);

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
        }

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
            foreach (var item in list)
            {
                string itemPath = $"{path}[{index}]";
                Debug.WriteLine($"[Build List] Array node \"{itemPath}\" with ValueKind {item.ValueKind}");

                // Create a label for the index.
                var label = new Label { Text = $"[{index}]", Tag = index };

                Control itemControl = BuildControlForValue(item, orientation, itemPath);

                if (itemControl is TextBox || itemControl is CheckBox || itemControl is DateTimePicker)
                    itemControl.Tag = itemPath;

                OriginalTypes[itemPath] = MapJsonElementToType(item);

                var row = new StackLayout { Orientation = Invert(orientation), Spacing = 5 };
                row.Items.Add(new StackLayoutItem(label));
                row.Items.Add(new StackLayoutItem(itemControl, HorizontalAlignment.Stretch, true));

                // Add delete button for array items
                var deleteButton = new Button { Text = "×", Tag = itemPath, Width = 25 };
                deleteButton.Click += (s, e) => DeleteArrayElement(path, index);
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
            switch (value.ValueKind)
            {
                case JsonValueKind.Null:
                    return new Label
                    {
                        Text = "null",
                        Font = Fonts.Monospace(fontSize),
                        TextColor = Colors.Gray
                    };

                case JsonValueKind.Object:
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value.GetRawText());
                    return new FullJsonEditorPanel(dict, Invert(orientation), path, this.OriginalTypes, _showHeader);

                case JsonValueKind.Array:
                    return BuildFromList(value.EnumerateArray(), Invert(orientation), path);

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return new CheckBox
                    {
                        Checked = value.GetBoolean(),
                        Tag = path,
                        Font = Fonts.Monospace(fontSize),
                    };

                case JsonValueKind.Number:
                    if (value.TryGetInt64(out long l))
                    {
                        var numberTextBox = new TextBox
                        {
                            Text = l.ToString(),
                            Tag = path,
                            Font = Fonts.Monospace(fontSize),
                        };
                        return numberTextBox;
                    }
                    else
                    {
                        double d = value.GetDouble();
                        var doubleTextBox = new TextBox
                        {
                            Text = d.ToString(),
                            Tag = path,
                            Font = Fonts.Monospace(fontSize),
                        };
                        return doubleTextBox;
                    }

                case JsonValueKind.String:
                    string stringValue = value.GetString();

                    // Check for button syntax
                    if (stringValue.StartsWith("button:"))
                    {
                        var buttonText = stringValue.Substring(7); // Remove "button:" prefix
                        var button = new Button { Text = buttonText, Tag = path };
                        button.Click += (sender, e) =>
                        {
                            if (AppMode)
                            {
                                Debug.WriteLine($"[App Mode] Button '{buttonText}' clicked at path '{path}'");
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
                                    Debug.WriteLine($"[App Mode] Date '{dateValue}' clicked at path '{path}'");
                                };
                            }

                            return datePicker;
                        }
                    }

                    // Check for base64 encoded image
                    if (IsBase64Image(stringValue, out ImageType imageType))
                    {
                        try
                        {
                            byte[] imageBytes = Convert.FromBase64String(stringValue);
                            var image = new Bitmap(imageBytes);
                            var imageView = new ImageView { Image = image, Tag = path };

                            // In app mode, make the image clickable to log
                            if (AppMode)
                            {
                                imageView.Cursor = Cursors.Pointer;
                                imageView.MouseDown += (sender, e) =>
                                {
                                    Debug.WriteLine($"[App Mode] Image clicked at path '{path}'");
                                };
                            }

                            // Add a button to change the image
                            var changeImageButton = new Button { Text = "Change Image", Tag = path };
                            changeImageButton.Click += (s, e) => ChangeImage(path);

                            var container = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };
                            container.Items.Add(new StackLayoutItem(imageView));
                            container.Items.Add(new StackLayoutItem(changeImageButton));

                            return container;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Image Error] Failed to load image at path '{path}': {ex.Message}");
                            // Fall back to a text box if image loading fails
                        }
                    }

                    // Default to a text box
                    var defaultTextBox = new TextBox
                    {
                        Text = stringValue,
                        Tag = path,
                        Width = cWidth,
                        Font = Fonts.Monospace(fontSize),
                    };

                    // Add an "Upload Image" button for potential base64 images
                    var uploadButton = new Button { Text = "Upload Image", Tag = path };
                    uploadButton.Click += (s, e) => UploadImageAsBase64(path);

                    var containerWithUpload = new StackLayout { Orientation = Orientation.Vertical, Spacing = 5 };
                    containerWithUpload.Items.Add(new StackLayoutItem(defaultTextBox, HorizontalAlignment.Stretch, true));
                    containerWithUpload.Items.Add(new StackLayoutItem(uploadButton));

                    return containerWithUpload;

                default:
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
            else if (control is Panel panel && panel.Content != null)
            {
                UpdateControlStatesRecursively(panel.Content);
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
        /// Checks if a string is a valid base64 encoded image.
        /// </summary>
        private bool IsBase64Image(string value, out ImageType imageType)
        {
            imageType = ImageType.Unknown;

            // Check if it's a valid base64 string
            if (value.Length % 4 != 0 || !Regex.IsMatch(value, @"^[a-zA-Z0-9\+/]*={0,3}$"))
                return false;

            try
            {
                byte[] bytes = Convert.FromBase64String(value);

                // Check for image signatures
                if (bytes.Length < 4)
                    return false;

                // JPEG signature: FF D8 FF
                if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                {
                    imageType = ImageType.Jpeg;
                    return true;
                }

                // PNG signature: 89 50 4E 47
                if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                {
                    imageType = ImageType.Png;
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
        /// Opens a file dialog to select an image and converts it to base64.
        /// </summary>
        private void UploadImageAsBase64(string path)
        {
            var fileDialog = new OpenFileDialog
            {
                Title = "Select an image",
                Filters =
                {
                    new FileFilter("Image Files (*.png;*.jpg;*.jpeg)", ".png", ".jpg", ".jpeg")
                }
            };

            if (fileDialog.ShowDialog(this) == DialogResult.Ok)
            {
                try
                {
                    byte[] imageBytes = File.ReadAllBytes(fileDialog.FileName);
                    string base64 = Convert.ToBase64String(imageBytes);

                    // Update the JSON with the new image
                    UpdateValueAtPath(path, base64);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                }
            }
        }

        /// <summary>
        /// Changes an existing image at the specified path.
        /// </summary>
        private void ChangeImage(string path)
        {
            UploadImageAsBase64(path);
        }

        /// <summary>
        /// Updates the value at a specific path in the JSON.
        /// </summary>
        private void UpdateValueAtPath(string path, string value)
        {
            try
            {
                var json = _currentJson;
                var updatedJson = UpdateValueInJson(json, path, value);
                UpdateJson(updatedJson);
            }
            catch (Exception ex)
            {
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
                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Navigate to the parent of the target
                var parts = path.Split('.');
                var parentPath = string.Join(".", parts.Take(parts.Length - 1));
                var propertyName = parts.Last();

                // Handle array indices
                if (propertyName.Contains("[") && propertyName.Contains("]"))
                {
                    var match = Regex.Match(propertyName, @"(.+)\[(\d+)\]");
                    if (match.Success)
                    {
                        propertyName = match.Groups[1].Value;
                        var index = int.Parse(match.Groups[2].Value);

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
                            return UpdatePathInJson(json, parentPath, list);
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException($"Index {index} is out of range for array with {list.Count} items.");
                        }
                    }
                }

                // Navigate to the parent object
                var parentElement = string.IsNullOrEmpty(parentPath) ? root : NavigateToPath(root, parentPath);

                if (parentElement.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException($"Path '{parentPath}' does not point to an object.");

                // Convert to a mutable representation
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(parentElement.GetRawText());

                // Update the property
                if (dict.ContainsKey(propertyName))
                {
                    var existingValue = dict[propertyName];
                    var updatedValue = CreateUpdatedValue(existingValue, newValue);
                    dict[propertyName] = updatedValue;
                }
                else
                {
                    throw new InvalidOperationException($"Property '{propertyName}' not found.");
                }

                // Update the parent in the document
                return UpdatePathInJson(json, parentPath, dict);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update value at path '{path}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates an updated value based on the existing value type and the new string value.
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
            // Create the dialog first
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

                // Update the JSON with the new property
                try
                {
                    var json = _currentJson;
                    var updatedJson = AddPropertyToJson(json, parentPath, name, type, value);
                    UpdateJson(updatedJson);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Error adding property: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                }
            }
        }

        /// <summary>
        /// Adds a new item to an array.
        /// </summary>
        private void AddArrayItem(string arrayPath)
        {
            // Create the dialog first
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

                // Update the JSON with the new array item
                try
                {
                    var json = _currentJson;
                    var updatedJson = AddItemToArrayJson(json, arrayPath, type, value);
                    UpdateJson(updatedJson);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Error adding array item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                }
            }
        }

        /// <summary>
        /// Deletes an element at the specified path.
        /// </summary>
        private void DeleteElement(string path)
        {
            var result = MessageBox.Show(this, $"Are you sure you want to delete '{path}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxType.Question);
            if (result == DialogResult.Yes)
            {
                try
                {
                    var json = _currentJson;
                    var updatedJson = DeletePropertyFromJson(json, path);
                    UpdateJson(updatedJson);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Error deleting property: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                }
            }
        }

        /// <summary>
        /// Deletes an array item at the specified path and index.
        /// </summary>
        private void DeleteArrayElement(string arrayPath, int index)
        {
            var result = MessageBox.Show(this, $"Are you sure you want to delete item at index {index} in '{arrayPath}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxType.Question);
            if (result == DialogResult.Yes)
            {
                try
                {
                    var json = _currentJson;
                    var updatedJson = DeleteItemFromArrayJson(json, arrayPath, index);
                    UpdateJson(updatedJson);
                }
                catch (Exception ex)
                {
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
                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    json = "{}";
                }

                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Navigate to the parent object
                var parentElement = string.IsNullOrEmpty(parentPath) ? root : NavigateToPath(root, parentPath);

                if (parentElement.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException($"Path '{parentPath}' does not point to an object.");

                // Create the new property value based on type
                object newValue = CreateTypedValue(type, value);

                // Convert to a mutable representation
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(parentElement.GetRawText());

                // Add the new property
                dict[name] = newValue;

                // Update the parent in the document
                return UpdatePathInJson(json, parentPath, dict);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Invalid JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
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
                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    json = "{}";
                }

                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Navigate to the parent array
                var arrayElement = NavigateToPath(root, arrayPath);

                if (arrayElement.ValueKind != JsonValueKind.Array)
                    throw new InvalidOperationException($"Path '{arrayPath}' does not point to an array.");

                // Create the new item value based on type
                object newValue = CreateTypedValue(type, value);

                // Convert to a mutable representation
                var list = JsonSerializer.Deserialize<List<object>>(arrayElement.GetRawText());

                // Add the new item
                list.Add(newValue);

                // Update the array in the document
                return UpdatePathInJson(json, arrayPath, list);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Invalid JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
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
                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    json = "{}";
                }

                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Split the path into parent and property name
                var lastDot = path.LastIndexOf('.');
                string parentPath = lastDot >= 0 ? path.Substring(0, lastDot) : "";
                string propertyName = lastDot >= 0 ? path.Substring(lastDot + 1) : path;

                // Navigate to the parent object
                var parentElement = string.IsNullOrEmpty(parentPath) ? root : NavigateToPath(root, parentPath);

                if (parentElement.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException($"Path '{parentPath}' does not point to an object.");

                // Convert to a mutable representation
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(parentElement.GetRawText());

                // Remove the property
                dict.Remove(propertyName);

                // Update the parent in the document
                return UpdatePathInJson(json, parentPath, dict);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Invalid JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
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
                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
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

                // Remove the item at the specified index
                if (index >= 0 && index < list.Count)
                {
                    list.RemoveAt(index);
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"Index {index} is out of range for array with {list.Count} items.");
                }

                // Update the array in the document
                return UpdatePathInJson(json, arrayPath, list);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Invalid JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete array item: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Navigates to a specific path in the JSON document.
        /// </summary>
        private JsonElement NavigateToPath(JsonElement element, string path)
        {
            if (string.IsNullOrEmpty(path))
                return element;

            var parts = path.Split('.');
            var current = element;

            foreach (var part in parts)
            {
                if (current.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException($"Cannot navigate to '{part}' because current element is not an object.");

                if (!current.TryGetProperty(part, out var next))
                    throw new InvalidOperationException($"Property '{part}' not found.");

                current = next;
            }

            return current;
        }

        /// <summary>
        /// Updates a specific path in the JSON document with a new value.
        /// </summary>
        private string UpdatePathInJson(string json, string path, object newValue)
        {
            try
            {
                // Ensure we have valid JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    json = "{}";
                }

                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (string.IsNullOrEmpty(path))
                {
                    // Update the root
                    return JsonSerializer.Serialize(newValue, new JsonSerializerOptions { WriteIndented = true });
                }

                // Split the path into parts
                var parts = path.Split('.');

                // Create a mutable representation of the entire JSON
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                // Navigate to the parent object
                var current = dict;
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (!current.TryGetValue(parts[i], out var next))
                        throw new InvalidOperationException($"Property '{parts[i]}' not found.");

                    if (next is Dictionary<string, object> nextDict)
                    {
                        current = nextDict;
                    }
                    else if (next is List<object> nextList && i < parts.Length - 1 && int.TryParse(parts[i + 1], out int arrayIndex))
                    {
                        // Handle array navigation
                        if (arrayIndex >= 0 && arrayIndex < nextList.Count)
                        {
                            if (arrayIndex < nextList.Count - 1)
                            {
                                // Continue navigating through the array
                                if (nextList[arrayIndex] is Dictionary<string, object> arrayItemDict)
                                {
                                    current = arrayItemDict;
                                    i++; // Skip the array index part
                                }
                                else
                                {
                                    throw new InvalidOperationException($"Array item at index {arrayIndex} is not an object.");
                                }
                            }
                            else
                            {
                                // This is the last part, update the array item
                                nextList[arrayIndex] = newValue;
                                return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                            }
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException($"Array index {arrayIndex} is out of range.");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Cannot navigate to '{parts[i]}' because current element is not an object or array.");
                    }
                }

                // Update the final property
                current[parts[parts.Length - 1]] = newValue;

                return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (JsonException ex)
            {
                throw new Exception($"Invalid JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update path '{path}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a typed value based on type and string value.
        /// </summary>
        private object CreateTypedValue(string type, string value)
        {
            switch (type)
            {
                case "String":
                    return value;

                case "Number":
                    if (long.TryParse(value, out long longValue))
                        return longValue;
                    else if (double.TryParse(value, out double doubleValue))
                        return doubleValue;
                    else
                        throw new ArgumentException($"'{value}' is not a valid number.");

                case "Boolean":
                    if (bool.TryParse(value, out bool boolValue))
                        return boolValue;
                    else
                        throw new ArgumentException($"'{value}' is not a valid boolean. Use 'true' or 'false'.");

                case "Date":
                    if (IsISO8601Date(value))
                        return value; // Store dates as strings
                    else
                        throw new ArgumentException($"'{value}' is not a valid ISO8601 date format.");

                case "Image":
                    if (IsBase64Image(value, out _))
                        return value; // Store images as base64 strings
                    else
                        throw new ArgumentException($"'{value}' is not a valid base64 encoded image.");

                case "Object":
                    return new Dictionary<string, object>();

                case "Array":
                    return new List<object>();

                default:
                    throw new ArgumentException($"Unknown type: {type}");
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
                Debug.WriteLine($"[Validate] Validating TextBox at \"{path}\" with text: \"{textBox.Text}\"");

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
            else if (control is Panel panel && panel.Content != null)
            {
                ValidateControlsRecursively(panel.Content, errors);
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
            var errors = ValidateFields();
            if (errors.Any())
            {
                Debug.WriteLine("[ToJson] Validation errors: " + string.Join(", ", errors));
            }

            object result = SerializeValue(_contentContainer as Control, "");
            var options = new JsonSerializerOptions { WriteIndented = true };

            try
            {
                return JsonSerializer.Serialize(result, options);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ToJson] Error serializing JSON: {ex.Message}");
                return "{}"; // Return empty object if serialization fails
            }
        }

        /// <summary>
        /// Converts a control's current state to its corresponding JSON value.
        /// </summary>
        private object SerializeValue(Control control, string path)
        {
            if (control is TextBox textBox)
            {
                if (OriginalTypes.TryGetValue(path, out Type originalType))
                {
                    if (originalType == typeof(long))
                    {
                        if (long.TryParse(textBox.Text, out long l))
                            return l;
                        else
                            return textBox.Text;
                    }
                    else if (originalType == typeof(double))
                    {
                        if (double.TryParse(textBox.Text, out double d))
                            return d;
                        else
                            return textBox.Text;
                    }
                    else
                    {
                        return textBox.Text;
                    }
                }
                return textBox.Text;
            }
            else if (control is CheckBox checkBox)
            {
                return checkBox.Checked ?? false;
            }
            else if (control is DateTimePicker datePicker)
            {
                return datePicker.Value?.ToString("yyyy-MM-ddTHH:mm:ss");
            }
            else if (control is Button button)
            {
                if (button.Text.StartsWith("button:"))
                    return button.Text;
                return null; // Buttons are not serialized unless they have special syntax
            }
            else if (control is ImageView)
            {
                // For images, we need to find the associated text box with the base64 data
                // This is a simplified approach
                return null;
            }
            else if (control is Panel panel && panel.Content is Control inner)
            {
                return SerializeValue(inner, path);
            }
            else if (control is StackLayout layout)
            {
                return SerializeContainer(layout, path);
            }
            else if (control is Label label)
            {
                // Handle null values properly
                if (label.Text == "null")
                    return null;
                return label.Text;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Depending on whether the container represents an array or an object, serializes its children accordingly.
        /// </summary>
        private object SerializeContainer(StackLayout container, string basePath)
        {
            if (IsArrayContainer(container))
            {
                List<object> list = new List<object>();
                int index = 0;
                foreach (var item in container.Items)
                {
                    // Each item should be a row (StackLayout wrapped in a StackLayoutItem).
                    if (item.Control is StackLayout row)
                    {
                        string childPath = $"{basePath}[{index}]";
                        if (row.Items.Count >= 2 && row.Items[1].Control != null)
                        {
                            object value = SerializeValue(row.Items[1].Control, childPath);
                            list.Add(value);
                        }
                        index++;
                    }
                }
                return list;
            }
            else
            {
                Dictionary<string, object> obj = new Dictionary<string, object>();
                foreach (var item in container.Items)
                {
                    if (item.Control is StackLayout row && row.Items.Count >= 2)
                    {
                        // The first item is the label; extract the key.
                        var lblItem = row.Items[0].Control as Label;
                        string key = lblItem?.Tag as string ?? lblItem?.Text;
                        string childPath = string.IsNullOrEmpty(basePath) ? key : $"{basePath}.{key}";

                        if (row.Items[1].Control != null)
                        {
                            object value = SerializeValue(row.Items[1].Control, childPath);
                            obj[key] = value;
                        }
                    }
                }
                return obj;
            }
        }

        /// <summary>
        /// Checks whether the container appears to represent an array.
        /// </summary>
        private bool IsArrayContainer(StackLayout container)
        {
            foreach (var item in container.Items)
            {
                if (item.Control is StackLayout row && row.Items.Count > 0 && row.Items[0].Control is Label label)
                {
                    string text = label.Text;
                    if (!(text.StartsWith("[") && text.EndsWith("]")))
                        return false;
                }
            }
            return true;
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