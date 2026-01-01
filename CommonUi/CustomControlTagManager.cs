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
    /// </summary>
    public class TagEntryPanel : Panel, ILookupSupportedChildPanel
    {
        private TableLayout tagsContainer;
        private TextBox newTagTextBox;
        private Label suggestionLabel;
        private Button addTagButton;

        private readonly Dictionary<string, Func<object>> actionsMap =
            new Dictionary<string, Func<object>>();
        private readonly Dictionary<string, Action<object>> setMap =
            new Dictionary<string, Action<object>>();
        private Action? moveNext;
        private Action? GlobalChangeHandler = null;

        private HashSet<string> tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Autocomplete suggestions
        public static string[] Autocomplete = new[]
        {
            "Amoxicillin", "Clavulanate", "Penicillin", "Ibuprofen", "Paracetamol",
            "Aspirin", "Diphenhydramine", "Loratadine", "Omeprazole", "Metformin",
            "Amlodipine", "Simvastatin", "Levothyroxine", "Metoprolol", "Albuterol",
            "Azithromycin", "Sertraline", "Gabapentin", "Hydrochlorothiazide", "Losartan",
            "Alprazolam", "Zolpidem", "Furosemide", "Tramadol", "Trazodone",
            "Warfarin", "Prednisone", "Hydrocodone", "Atorvastatin", "Fluoxetine",
            "Citalopram", "Cephalexin", "Metronidazole", "Glyburide", "Glipizide",
            "Acetaminophen", "Ciprofloxacin", "Lisinopril", "Ondansetron", "Pantoprazole",
            "Amoxicillin-clavulanate", "Cephalexin", "Ciprofloxacin", "Clindamycin",
            "Doxycycline", "Metronidazole", "Nitrofurantoin", "Sulfamethoxazole-trimethoprim"
        };

        public TagEntryPanel(string[]? mappings = null)
        {
            InitializeUI();

            if (mappings != null)
            {
                MapLookupValues(mappings);
                MapSetValues(mappings);
            }

            // Initial refresh of tags display
            RefreshTagsDisplay();
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

            // Suggestion label for autocomplete
            suggestionLabel = new Label
            {
                Text = "",
                TextColor = ColorSettings.LesserForegroundColor,
                BackgroundColor = ColorSettings.BackgroundColor,
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

            // Handle text changes in the textbox
            newTagTextBox.TextChanged += (sender, e) => UpdateSuggestion();

            // Handle key events in the textbox
            newTagTextBox.KeyDown += (sender, e) =>
            {
                if (e.Key == Keys.Enter)
                {
                    // If there's a suggestion, use it
                    if (!string.IsNullOrEmpty(suggestionLabel.Text))
                    {
                        newTagTextBox.Text = suggestionLabel.Text;
                        suggestionLabel.Text = "";
                        e.Handled = true;
                    }
                    else
                    {
                        // Otherwise, add the tag
                        AddNewTag();
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

            // Handle click on suggestion label
            suggestionLabel.MouseUp += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(suggestionLabel.Text))
                {
                    newTagTextBox.Text = suggestionLabel.Text;
                    suggestionLabel.Text = "";
                    newTagTextBox.Focus();
                }
            };

            // Add tag when button is clicked
            addTagButton.Click += (sender, e) =>
            {
                // If there's a suggestion, use it
                if (!string.IsNullOrEmpty(suggestionLabel.Text))
                {
                    newTagTextBox.Text = suggestionLabel.Text;
                    suggestionLabel.Text = "";
                }
                // Add the tag
                AddNewTag();
            };

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
                    new TableRow(newTagTextBox),
                    new TableRow(suggestionLabel),
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
            suggestionLabel.Text = "";

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
            int columns = Math.Max(1, (int)(( 300) / 120)); // Approximate width per tag

            // Create a list of tags to display
            var tagsList = tags.ToList();

            // Create rows with tags
            for (int i = 0; i < tagsList.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;

                // Ensure we have enough rows
                while (tagsContainer.Rows.Count <= row)
                {
                    tagsContainer.Rows.Add(new TableRow());
                }

                // Create tag panel for this specific tag
                string currentTag = tagsList[i]; // Capture the tag in a local variable
                var tagPanel = CreateTagPanel(currentTag);

                // Create a table cell for this tag
                var cell = new TableCell(tagPanel, false);

                // Add to the appropriate row
                tagsContainer.Rows[row].Cells.Add(cell);
            }
        }

        private StackLayout CreateTagPanel(string tag)
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
            // Use a local variable to ensure the correct tag is captured in the closure
            string tagToRemove = tag;
            removeButton.Click += (sender, e) =>
            {
                tags.Remove(tagToRemove);
                RefreshTagsDisplay();
                GlobalChangeHandler?.Invoke();
            };

            tagPanel.Items.Add(tagLabel);
            tagPanel.Items.Add(removeButton);

            return tagPanel;
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

        public int RowSpan() => 4; // Increased to accommodate the suggestion label
    }
}