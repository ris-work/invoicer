using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using RV.InvNew.Common;

namespace CommonUi
{
    /// <summary>
    /// A panel for adding and removing multiple tags.
    /// Tags are serialized as a string with pipe (|) delimiter.
    /// Example: "amoxicillin|clavulanate|antibiotic"
    /// </summary>
    public class TagEntryPanel : Panel, ILookupSupportedChildPanel
    {
        private TableLayout tagsContainer;
        private TextBox newTagTextBox;
        private Button addTagButton;

        private readonly Dictionary<string, Func<object>> actionsMap =
            new Dictionary<string, Func<object>>();
        private readonly Dictionary<string, Action<object>> setMap =
            new Dictionary<string, Action<object>>();
        private Action? moveNext;
        private Action? GlobalChangeHandler = null;

        private HashSet<string> tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public TagEntryPanel(string[]? mappings = null)
        {
            InitializeUI();

            if (mappings != null)
            {
                MapLookupValues(mappings);
                MapSetValues(mappings);
            }
        }

        private void InitializeUI()
        {
            // Container for displaying tags using TableLayout
            tagsContainer = new TableLayout
            {
                Spacing = new Size(5, 5),
                BackgroundColor = ColorSettings.BackgroundColor,
                Width = ColorSettings.ControlWidth ?? 300,
                Height = 100
            };

            // Input for new tags
            newTagTextBox = new TextBox
            {
                PlaceholderText = TranslationHelper.Translate("Enter tag and press Enter or click Add"),
                BackgroundColor = ColorSettings.LesserBackgroundColor,
                TextColor = ColorSettings.LesserForegroundColor,
                Width = ColorSettings.ControlWidth ?? 200
            };

            // Button to add new tags
            addTagButton = new Button
            {
                Text = TranslationHelper.Translate("Add Tag"),
                BackgroundColor = ColorSettings.BackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
                Width = ColorSettings.ControlWidth ?? 100
            };

            // Add tag when Enter is pressed in the text box
            newTagTextBox.KeyDown += (sender, e) =>
            {
                if (e.Key == Keys.Enter)
                {
                    AddNewTag();
                    e.Handled = true;
                }
                else if (e.Key == Keys.Tab)
                {
                    // Move to next control when Tab is pressed
                    moveNext?.Invoke();
                    e.Handled = true;
                }
            };

            // Add tag when button is clicked
            addTagButton.Click += (sender, e) => AddNewTag();

            // Layout the controls
            Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Rows =
                {
                    new TableRow(new Label {
                        Text = TranslationHelper.Translate("Tags:"),
                        TextColor = ColorSettings.ForegroundColor
                    }),
                    new TableRow(tagsContainer) { ScaleHeight = true },
                    new TableRow(
                        new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 5,
                            Items = { newTagTextBox, addTagButton }
                        }
                    )
                }
            };
        }

        private void AddNewTag()
        {
            string tagText = newTagTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(tagText))
                return;

            if (tagText.Contains("|"))
            {
                MessageBox.Show(TranslationHelper.Translate("Tags cannot contain the pipe (|) character"),
                                TranslationHelper.Translate("Invalid Tag"),
                                MessageBoxType.Warning);
                return;
            }

            // Convert to lowercase for consistency
            string normalizedTag = tagText.ToLowerInvariant();

            // Check for uniqueness
            if (tags.Contains(normalizedTag))
            {
                MessageBox.Show(TranslationHelper.Translate("This tag already exists"),
                                TranslationHelper.Translate("Duplicate Tag"),
                                MessageBoxType.Information);
                return;
            }

            // Add the tag
            tags.Add(normalizedTag);
            newTagTextBox.Text = "";

            // Refresh the UI
            RefreshTagsDisplay();

            // Notify of change
            GlobalChangeHandler?.Invoke();
        }

        private void RefreshTagsDisplay()
        {
            // Clear existing tag controls
            tagsContainer.Rows.Clear();

            // Determine how many columns to use based on available width
            int columns = Math.Max(1, (int)((300 / 120))); // Approximate width per tag
            int currentColumn = 0;
            var currentRow = new TableRow();

            // Add a control for each tag
            foreach (var tag in tags)
            {
                var tagPanel = new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 3,
                    Padding = new Padding(3),
                    BackgroundColor = ColorSettings.LesserBackgroundColor
                };

                var tagLabel = new Label
                {
                    Text = tag,
                    TextColor = ColorSettings.LesserForegroundColor,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var removeButton = new Button
                {
                    Text = "×",
                    Width = 20,
                    Height = 20,
                    BackgroundColor = Colors.Red,
                    TextColor = Colors.White
                };

                // Remove tag when button is clicked
                removeButton.Click += (sender, e) =>
                {
                    tags.Remove(tag);
                    RefreshTagsDisplay();
                    GlobalChangeHandler?.Invoke();
                };

                tagPanel.Items.Add(tagLabel);
                tagPanel.Items.Add(removeButton);

                // Create a table cell for this tag
                var cell = new TableCell(tagPanel, false);

                // Add to current row
                currentRow.Cells.Add(cell);
                currentColumn++;

                // If we've filled the columns, add the row and start a new one
                if (currentColumn >= columns)
                {
                    tagsContainer.Rows.Add(currentRow);
                    currentRow = new TableRow();
                    currentColumn = 0;
                }
            }

            // Add the last row if it has any tags
            if (currentRow.Cells.Count > 0)
            {
                tagsContainer.Rows.Add(currentRow);
            }
        }

        public void MapLookupValues(string[] fieldNames)
        {
            // Map the serialized tag string
            actionsMap.Add(fieldNames[0], () => string.Join("|", tags));
        }

        public object LookupValue(string fieldName)
        {
            if (actionsMap.TryGetValue(fieldName, out var getter))
            {
                return getter();
            }
            return null!;
        }

        public void SetMoveNext(Action moveNext) => this.moveNext = moveNext;

        public List<Control> GetFocusableControls() =>
            new List<Control> { newTagTextBox, addTagButton };

        public (bool isValid, string errorDescription) Validate() => (true, string.Empty);

        public void FocusChild() => newTagTextBox.Focus();

        public void SetOriginalValues(object[] originalValues)
        {
            if (originalValues.Length > 0 && originalValues[0] is string tagString)
            {
                // Clear existing tags
                tags.Clear();

                // Parse the tag string
                if (!string.IsNullOrEmpty(tagString))
                {
                    string[] tagArray = tagString.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var tag in tagArray)
                    {
                        if (!string.IsNullOrWhiteSpace(tag))
                        {
                            tags.Add(tag.ToLowerInvariant());
                        }
                    }
                }

                // Refresh the UI
                RefreshTagsDisplay();
            }
        }

        public void SetOriginalValue(string key, object value)
        {
            if (setMap.ContainsKey(key))
            {
                setMap[key](value);
            }
        }

        public void MapSetValues(string[] fieldNames)
        {
            setMap.Add(fieldNames[0], (val) =>
            {
                if (val is string tagString)
                {
                    // Clear existing tags
                    tags.Clear();

                    // Parse the tag string
                    if (!string.IsNullOrEmpty(tagString))
                    {
                        string[] tagArray = tagString.Split('|', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var tag in tagArray)
                        {
                            if (!string.IsNullOrWhiteSpace(tag))
                            {
                                tags.Add(tag.ToLowerInvariant());
                            }
                        }
                    }

                    // Refresh the UI
                    RefreshTagsDisplay();
                }
            });
        }

        public void SetGlobalChangeWatcher(Action GlobalChangeHandler)
        {
            this.GlobalChangeHandler = GlobalChangeHandler;
        }

        public int RowSpan() => 3;
    }
}