using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Eto.Forms;
using Eto.Drawing;
using System.Text.Json;

namespace JsonEditorExample
{
    public class JsonEditorPanel : Panel
    {
        /// <summary>
        /// For each node path (e.g. "Parent.Child" or "array[0]"), the original JSON type is stored.
        /// For numbers, only long and double are supported.
        /// </summary>
        public Dictionary<string, Type> OriginalTypes { get; private set; } = new Dictionary<string, Type>();

        /// <summary>
        /// Constructs a JsonEditorPanel from a JSON object expressed as an IReadOnlyDictionary.
        /// </summary>
        public JsonEditorPanel(IReadOnlyDictionary<string, JsonElement> data, Orientation orientation)
            : this(data, orientation, "", null)
        {
        }

        /// <summary>
        /// Constructs a JsonEditorPanel from a JSON string.
        /// </summary>
        public JsonEditorPanel(string json, Orientation orientation)
            : this(ConvertJsonToDictionary(json), orientation, "", null)
        {
        }

        /// <summary>
        /// Private constructor that supports recursive calls.
        /// </summary>
        private JsonEditorPanel(IReadOnlyDictionary<string, JsonElement> data, Orientation orientation, string path, Dictionary<string, Type> originalTypes)
        {
            if (originalTypes != null)
                this.OriginalTypes = originalTypes;
            else
                this.OriginalTypes = new Dictionary<string, Type>();

            Debug.WriteLine($"[Panel Build] Creating JsonEditorPanel at path \"{path}\" with orientation {orientation}");

            // Create a container StackLayout.
            var rootStack = new StackLayout { Orientation = orientation, Spacing = 5 };

            BuildFromDictionary(data, rootStack, orientation, path);

            this.Content = rootStack;

            // Disable all interactive fields by default.
            DisableAllFields();
        }

        /// <summary>
        /// Converts a JSON string to a dictionary using C#’s built‑in System.Text.Json.
        /// </summary>
        private static IReadOnlyDictionary<string, JsonElement> ConvertJsonToDictionary(string json)
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        }

        /// <summary>
        /// Recursively builds controls from each key/value pair in the dictionary.
        /// </summary>
        private void BuildFromDictionary(IReadOnlyDictionary<string, JsonElement> dict, StackLayout container, Orientation orientation, string path)
        {
            foreach (var kvp in dict)
            {
                string currentPath = string.IsNullOrEmpty(path) ? kvp.Key : $"{path}.{kvp.Key}";
                Debug.WriteLine($"[Build Dictionary] Node \"{currentPath}\" with ValueKind {kvp.Value.ValueKind}");

                // Create a label whose text is the translated key.
                var label = new Label { Text = Translate(kvp.Key), Tag = kvp.Key };

                // Create the control for the value.
                Control valueControl = BuildControlForValue(kvp.Value, orientation, currentPath);

                // For interactive controls, store its node path in the Tag.
                if (valueControl is TextBox || valueControl is CheckBox)
                    valueControl.Tag = currentPath;

                // Record the original type (for a number, a long or double).
                OriginalTypes[currentPath] = MapJsonElementToType(kvp.Value);

                // Create a "row" combining the label and value control.
                var row = new StackLayout { Orientation = Invert(orientation), Spacing = 5 };
                row.Items.Add(label);
                row.Items.Add(valueControl);

                // Wrap the row in a StackLayoutItem and add to the container.
                container.Items.Add(new StackLayoutItem(row));
            }
        }

        /// <summary>
        /// Recursively builds controls for each element in a JSON array.
        /// </summary>
        private Control BuildFromList(IEnumerable<JsonElement> list, Orientation orientation, string path)
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

                if (itemControl is TextBox || itemControl is CheckBox)
                    itemControl.Tag = itemPath;

                OriginalTypes[itemPath] = MapJsonElementToType(item);

                var row = new StackLayout { Orientation = Invert(orientation), Spacing = 5 };
                row.Items.Add(label);
                row.Items.Add(itemControl);

                container.Items.Add(new StackLayoutItem(row));
                index++;
            }
            return container;
        }

        /// <summary>
        /// Returns the appropriate control for a given JSON element.
        /// For numbers, distinguishes between long and double.
        /// </summary>
        private Control BuildControlForValue(JsonElement value, Orientation orientation, string path)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.Null:
                    Debug.WriteLine($"[Build Value] At \"{path}\": Encountered null");
                    return new Label { Text = "null" };

                case JsonValueKind.Object:
                    Debug.WriteLine($"[Build Value] At \"{path}\": Recursively building object");
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value.GetRawText());
                        return new JsonEditorPanel(dict, Invert(orientation), path, this.OriginalTypes);
                    }

                case JsonValueKind.Array:
                    Debug.WriteLine($"[Build Value] At \"{path}\": Recursively building array");
                    return BuildFromList(value.EnumerateArray(), Invert(orientation), path);

                case JsonValueKind.True:
                case JsonValueKind.False:
                    Debug.WriteLine($"[Build Value] At \"{path}\": Creating CheckBox for boolean: {value.GetBoolean()}");
                    {
                        var cb = new CheckBox { Checked = value.GetBoolean(), Tag = path };
                        return cb;
                    }

                case JsonValueKind.Number:
                    {
                        // First try to create a long.
                        if (value.TryGetInt64(out long l))
                        {
                            Debug.WriteLine($"[Build Value] At \"{path}\": Creating TextBox for long: {l}");
                            var tb = new TextBox { Text = l.ToString(), Tag = path };
                            return tb;
                        }
                        else
                        {
                            double d = value.GetDouble();
                            Debug.WriteLine($"[Build Value] At \"{path}\": Creating TextBox for double: {d}");
                            var tb = new TextBox { Text = d.ToString(), Tag = path };
                            return tb;
                        }
                    }

                case JsonValueKind.String:
                    Debug.WriteLine($"[Build Value] At \"{path}\": Creating TextBox for string: {value.GetString()}");
                    {
                        var tb = new TextBox { Text = value.GetString(), Tag = path };
                        return tb;
                    }

                default:
                    Debug.WriteLine($"[Build Value] At \"{path}\": Defaulting to TextBox for value: {value}");
                    {
                        var tb = new TextBox { Text = value.ToString(), Tag = path };
                        return tb;
                    }
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
        /// Maps a JsonElement to its corresponding .NET type. For numbers, returns long (if possible) or double.
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
        /// Recursively disables interactive controls.  
        /// Because StackLayout.Items returns a collection of StackLayoutItem, we access each item’s Control.
        /// </summary>
        private void DisableAllFields()
        {
            DisableControlsRecursively(this.Content);
        }

        private void DisableControlsRecursively(Control control)
        {
            if (control is TextBox tb)
            {
                Debug.WriteLine($"[Disable] Disabling TextBox (path \"{tb.Tag}\") with text: \"{tb.Text}\"");
                tb.Enabled = false;
            }
            else if (control is CheckBox cb)
            {
                Debug.WriteLine($"[Disable] Disabling CheckBox (path \"{cb.Tag}\") with state: {cb.Checked}");
                cb.Enabled = false;
            }
            else if (control is Panel panel && panel.Content != null)
            {
                DisableControlsRecursively(panel.Content);
            }
            else if (control is StackLayout layout)
            {
                foreach (var item in layout.Items)
                {
                    if (item.Control != null)
                        DisableControlsRecursively(item.Control);
                }
            }
        }

        /// <summary>
        /// Recursively validates each interactive field.  
        /// For numeric fields, it checks that the text can be parsed as long or double as expected.
        /// </summary>
        public List<string> ValidateFields()
        {
            List<string> errors = new List<string>();
            ValidateControlsRecursively(this.Content, errors);
            return errors;
        }

        private void ValidateControlsRecursively(Control control, List<string> errors)
        {
            if (control is TextBox tb)
            {
                string path = tb.Tag as string ?? "Unknown";
                Debug.WriteLine($"[Validate] Validating TextBox at \"{path}\" with text: \"{tb.Text}\"");
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    errors.Add($"Field \"{path}\" is empty.");
                }
                else if (OriginalTypes.TryGetValue(path, out var originalType))
                {
                    if (originalType == typeof(long))
                    {
                        if (!long.TryParse(tb.Text, out _))
                            errors.Add($"Field \"{path}\" expected a long but got '{tb.Text}'.");
                    }
                    else if (originalType == typeof(double))
                    {
                        if (!double.TryParse(tb.Text, out _))
                            errors.Add($"Field \"{path}\" expected a double but got '{tb.Text}'.");
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
            object result = SerializeValue(this.Content as Control, "");
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(result, options);
        }

        /// <summary>
        /// Converts a control’s current state to its corresponding JSON value.
        /// </summary>
        private object SerializeValue(Control control, string path)
        {
            if (control is TextBox tb)
            {
                if (OriginalTypes.TryGetValue(path, out Type originalType))
                {
                    if (originalType == typeof(long))
                    {
                        if (long.TryParse(tb.Text, out long l))
                            return l;
                        else
                            return tb.Text;
                    }
                    else if (originalType == typeof(double))
                    {
                        if (double.TryParse(tb.Text, out double d))
                            return d;
                        else
                            return tb.Text;
                    }
                    else
                    {
                        return tb.Text;
                    }
                }
                return tb.Text;
            }
            else if (control is CheckBox cb)
            {
                return cb.Checked ?? false;
            }
            else if (control is Panel panel && panel.Content is Control inner)
            {
                return SerializeValue(inner, path);
            }
            else if (control is StackLayout layout)
            {
                return SerializeContainer(layout, path);
            }
            else if (control is Label lbl)
            {
                return lbl.Text == "null" ? null : lbl.Text;
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
        /// Checks whether the container appears to represent an array (all rows begin with an index label).
        /// </summary>
        private bool IsArrayContainer(StackLayout container)
        {
            foreach (var item in container.Items)
            {
                if (item.Control is StackLayout row && row.Items.Count > 0 && row.Items[0].Control is Label lbl)
                {
                    string text = lbl.Text;
                    if (!(text.StartsWith("[") && text.EndsWith("]")))
                        return false;
                }
            }
            return true;
        }
    }
}
