using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using RV.InvNew.Common;

namespace CommonUi
{
    /// <summary>
    /// A panel for adding and removing multiple tags with autocomplete support.
    /// Tags are serialized as a string with pipe (|) delimiter.
    /// Example: "amoxicillin|clavulanate|antibiotic"
    /// 
    /// Usage:
    /// - Enter: Add the suggested tag (if available) or the typed tag
    /// - Shift+Enter: Add the typed tag as-is, ignoring suggestions
    /// - Tab: Move to next control
    /// </summary>
    public class TagEntryPanel : Panel, ILookupSupportedChildPanel
    {
        private TextBox newTagTextBox;
        private Label suggestionLabel;
        private Label instructionsLabel;
        private Button addTagButton;

        private readonly Dictionary<string, Func<object>> actionsMap =
            new Dictionary<string, Func<object>>();
        private readonly Dictionary<string, Action<object>> setMap =
            new Dictionary<string, Action<object>>();
        private Action? moveNext;
        private Action? GlobalChangeHandler = null;

        private List<string> tags = new List<string>();

        // Autocomplete suggestions
        public static string[] Autocomplete = new[]
        {
            "Amoxicillin", "Clavulanate", "Penicillin", "Ibuprofen", "Paracetamol",
            "Aspirin", "Diphenhydramine", "Loratadine", "Omeprazole", "Metformin",
            "Amlodipine", "Simvastatin", "Levothyroxine", "Metoprolol", "Albuterol",
            "Azithromycin", "Sertraline", "Gabapentin", "Hydrochlorothiazide", "Losartan",
            "Alprazolam", "Zolpidem", "Furosemide", "Tramadol", "Trazodone",
            "Warfarin", "Prednisone", "Hydrocodone", "Atorvastatin", "Fluoxetine",
            "Citalopram", "Cephalexin", "Ciprofloxacin", "Clindamycin",
            "Doxycycline", "Metronidazole", "Nitrofurantoin", "Sulfamethoxazole-trimethoprim"
        };

        public TagEntryPanel(string[]? mappings = null)
        {
            if (mappings != null)
            {
                MapLookupValues(mappings);
                MapSetValues(mappings);
            }

            Console.WriteLine($"[TagEntryPanel] Constructor: Initial tags count: {tags.Count}");

            // Initial refresh of tags display
            Changed();
        }

        private void UpdateSuggestion()
        {
            string input = newTagTextBox.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(input))
            {
                suggestionLabel.Text = "";
                return;
            }

            // Find the closest match that starts with the input
            var matches = Autocomplete
                .Where(s => s.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Length) // Prefer shorter matches
                .ToList();

            if (matches.Count > 0)
            {
                suggestionLabel.Text = matches[0];
            }
            else
            {
                // If no exact prefix match, find the closest match using Levenshtein distance
                var closestMatch = Autocomplete
                    .OrderBy(s => LevenshteinDistance(input.ToLowerInvariant(), s.ToLowerInvariant()))
                    .FirstOrDefault();

                if (closestMatch != null && LevenshteinDistance(input.ToLowerInvariant(), closestMatch.ToLowerInvariant()) < 3)
                {
                    suggestionLabel.Text = closestMatch;
                }
                else
                {
                    suggestionLabel.Text = "";
                }
            }
        }

        // Simple implementation of Levenshtein distance
        private int LevenshteinDistance(string s1, string s2)
        {
            int[,] matrix = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                matrix[i, 0] = i;

            for (int j = 0; j <= s2.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[s1.Length, s2.Length];
        }

        private void AddNewTag(bool useSuggestion = true)
        {
            string tagText = newTagTextBox.Text?.Trim();

            Console.WriteLine($"[TagEntryPanel] AddNewTag called with input: '{tagText}', useSuggestion: {useSuggestion}");

            if (string.IsNullOrEmpty(tagText))
            {
                Console.WriteLine("[TagEntryPanel] Empty tag, returning");
                return;
            }

            if (tagText.Contains("|"))
            {
                MessageBox.Show(TranslationHelper.Translate("Tags cannot contain pipe (|) character"),
                                TranslationHelper.Translate("Invalid Tag"),
                                MessageBoxType.Warning);
                return;
            }

            // Use suggestion if available and requested
            if (useSuggestion && !string.IsNullOrEmpty(suggestionLabel.Text))
            {
                tagText = suggestionLabel.Text;
                Console.WriteLine($"[TagEntryPanel] Using suggestion: '{tagText}'");
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

            // Add to tags collection
            tags.Add(normalizedTag);
            Console.WriteLine($"[TagEntryPanel] Added tag '{normalizedTag}'");

            // Clear input and suggestion
            newTagTextBox.Text = "";
            suggestionLabel.Text = "";

            // Update UI
            Changed();
        }

        private void RemoveTag(string tag)
        {
            Console.WriteLine($"[TagEntryPanel] RemoveTag called for '{tag}'");

            // Remove from tags collection
            bool removed = tags.Remove(tag);
            Console.WriteLine($"[TagEntryPanel] Tag '{tag}' removed: {removed}");

            // Update UI
            Changed();
        }

        private void Changed()
        {
            Console.WriteLine($"[TagEntryPanel] Changed() called with {tags.Count} tags");
            Console.WriteLine($"[TagEntryPanel] Tags in collection: {string.Join(", ", tags)}");

            // Save current textbox text and suggestion
            string currentText = newTagTextBox?.Text ?? "";
            string currentSuggestion = suggestionLabel?.Text ?? "";

            // Create a new TableLayout for the tags container
            var tagsContainer = new TableLayout()
            {
                Spacing = new Size(5, 5),
                BackgroundColor = ColorSettings.BackgroundColor,
                Width = ColorSettings.ControlWidth ?? 300,
                Height = 4*(ColorSettings.ControlHeight??30),
            };

            // Determine how many columns to use based on available width
            int columns = Math.Max(1, (int)((300 / 120))); // Approximate width per tag
            Console.WriteLine($"[TagEntryPanel] Using {columns} columns for tag display");

            // Create rows with tags
            for (int i = 0; i < tags.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;

                // Ensure we have enough rows
                while (tagsContainer.Rows.Count <= row)
                {
                    tagsContainer.Rows.Add(new TableRow() { ScaleHeight = false });
                }

                // Create tag panel for this specific tag
                string currentTag = tags[i]; // Capture tag in a local variable
                var tagPanel = CreateTagPanel(currentTag);

                // Create a table cell for this tag
                var cell = new TableCell(tagPanel, false);

                // Add to the appropriate row
                tagsContainer.Rows[row].Cells.Add(cell);
                Console.WriteLine($"[TagEntryPanel] Added tag '{currentTag}' at row {row}, column {col}");
            }

            Console.WriteLine($"[TagEntryPanel] Final tag container row count: {tagsContainer.Rows.Count}");

            // Create new controls for input
            newTagTextBox = new TextBox
            {
                Text = currentText,
                PlaceholderText = TranslationHelper.Translate("Enter tag and press Enter or Shift+Enter"),
                BackgroundColor = ColorSettings.LesserBackgroundColor,
                TextColor = ColorSettings.LesserForegroundColor,
                Width = ColorSettings.ControlWidth ?? 200
            };

            // Add tooltip to the textbox
            newTagTextBox.ToolTip = TranslationHelper.Translate("Enter: Add suggested tag\nShift+Enter: Add tag as-is\nTab: Move to next field");

            suggestionLabel = new Label
            {
                Text = currentSuggestion,
                TextColor = Colors.DarkGreen,
                BackgroundColor = ColorSettings.BackgroundColor,
                Width = ColorSettings.ControlWidth ?? 200
            };

            // Add tooltip to the suggestion label
            suggestionLabel.ToolTip = TranslationHelper.Translate("Click to use this suggestion");

            addTagButton = new Button
            {
                Text = TranslationHelper.Translate("Add Tag"),
                BackgroundColor = ColorSettings.BackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
                Width = ColorSettings.ControlWidth ?? 100
            };

            // Add tooltip to the button
            addTagButton.ToolTip = TranslationHelper.Translate("Click to add the suggested tag (if available) or the typed tag");

            // Create instruction label
            instructionsLabel = new Label
            {
                Text = TranslationHelper.Translate("Enter: Add suggested | Shift+Enter: Add as-is | Click suggestion to use it"),
                TextColor = ColorSettings.LesserForegroundColor,
                BackgroundColor = ColorSettings.BackgroundColor,
                Font = new Font(ColorSettings.UIFont ?? FontFamilies.Sans, 9)
            };

            // Wire up event handlers
            newTagTextBox.TextChanged += (sender, e) => UpdateSuggestion();

            newTagTextBox.KeyDown += (sender, e) =>
            {
                if (e.Key == Keys.Enter)
                {
                    if (e.Shift)
                    {
                        // Shift+Enter: Add tag as-is, ignoring suggestions
                        AddNewTag(false);
                        e.Handled = true;
                    }
                    else
                    {
                        // Regular Enter: Add suggested tag if available, otherwise add typed tag
                        AddNewTag(true);
                        e.Handled = true;
                    }
                }
                else if (e.Key == Keys.Tab)
                {
                    // Move to next control when Tab is pressed
                    moveNext?.Invoke();
                    e.Handled = true;
                }
            };

            suggestionLabel.MouseUp += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(suggestionLabel.Text))
                {
                    newTagTextBox.Text = suggestionLabel.Text;
                    suggestionLabel.Text = "";
                    newTagTextBox.Focus();
                }
            };

            addTagButton.Click += (sender, e) =>
            {
                // Add to tag using suggestion if available
                AddNewTag(true);
            };

            // Replace the entire content
            Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Rows =
                {
                    new TableRow(new Label {
                        Text = TranslationHelper.Translate("Tags:"),
                        TextColor = ColorSettings.ForegroundColor
                    }),
                    new TableRow(tagsContainer) { ScaleHeight = false },
                    new TableRow(newTagTextBox),
                    new TableRow(suggestionLabel),
                    new TableRow(instructionsLabel),
                    new TableRow(
                        new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 5,
                            Items = { addTagButton }
                        }
                    )
                }
            };

            // Notify of change
            GlobalChangeHandler?.Invoke();
        }

        private StackLayout CreateTagPanel(string tag)
        {
            Console.WriteLine($"[TagEntryPanel] CreateTagPanel called for '{tag}'");

            var tagPanel = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
                Padding = new Padding(3),
                BackgroundColor = ColorSettings.BackgroundColor
            };

            var tagLabel = new Label
            {
                Text = tag,
                TextColor = ColorSettings.ForegroundColor,
                //BackgroundColor = ColorSettings.BackgroundColor,
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

            // Add tooltip to the remove button
            removeButton.ToolTip = TranslationHelper.Translate("Remove this tag");

            // Remove tag when button is clicked
            // Use a local variable to ensure the correct tag is captured in the closure
            string tagToRemove = tag;
            removeButton.Click += (sender, e) =>
            {
                Console.WriteLine($"[TagEntryPanel] Remove button clicked for '{tagToRemove}'");
                RemoveTag(tagToRemove);
            };

            tagPanel.Items.Add(tagLabel);
            tagPanel.Items.Add(null);
            tagPanel.Items.Add(removeButton);

            return tagPanel;
        }

        public void MapLookupValues(string[] fieldNames)
        {
            // Map to serialized tag string
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
                Console.WriteLine($"[TagEntryPanel] SetOriginalValues called with '{tagString}'");

                // Clear existing tags
                tags.Clear();

                // Parse tag string
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

                // Update UI
                Changed();
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
                    Console.WriteLine($"[TagEntryPanel] MapSetValues called with '{tagString}'");

                    // Clear existing tags
                    tags.Clear();

                    // Parse tag string
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

                    // Update UI
                    Changed();
                }
            });
        }

        public void SetGlobalChangeWatcher(Action GlobalChangeHandler)
        {
            this.GlobalChangeHandler = GlobalChangeHandler;
        }

        public int RowSpan() => 4; // Increased to accommodate instruction label
    }
}