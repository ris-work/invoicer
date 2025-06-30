using System;
using System.Collections.Generic;
using System.Text.Json;
using Eto.Drawing;
using Eto.Forms;

namespace CommonUi
{
    //---------------------------------------------------------------------
    // AoT‑Friendly Search Panel Utility (No key prefix in cell values)
    //---------------------------------------------------------------------
    public static class SearchPanelUtility
    {
        public static string[] GenerateSearchDialog<T>(
                        List<T> items,
                        Control owner,
            bool debug = true,
            PanelSettings? localColors = null
            )
        {
            var generated = new Dialog<string[]>();

            var searchPanel = GenerateSearchPanel(items);
            generated.Content = searchPanel;
            searchPanel.OnSelectionMade += () => { generated.Close(searchPanel.Selected); };
            return generated.ShowModal(owner);
        }
        /// <summary>
        /// Generates a SearchPanelEto for a list of objects of type T.
        /// Each object is serialized to JSON and deserialized into a dictionary.
        /// Cell values are produced by converting each JSON value to a simple string without the key.
        /// Numeric fields output their raw numeric string (enabling numeric sort),
        /// while non‑numeric fields output their string value.
        /// An extra (typically hidden) column contains the full JSON for comprehensive search.
        /// </summary>
        /// <typeparam name="T">The type of objects to display.</typeparam>
        /// <param name="items">List of objects.</param>
        /// <param name="debug">Debug flag passed to SearchPanelEto.</param>
        /// <param name="localColors">Optional panel color settings.</param>
        /// <returns>A SearchPanelEto instance ready for integration in your Eto.Forms UI.</returns>
        public static SearchPanelEto GenerateSearchPanel<T>(
            List<T> items,
            bool debug = true,
            PanelSettings? localColors = null
        )
        {
            if (items == null || items.Count == 0)
            {
                return new SearchPanelEto(
                    new List<(string[] Cells, Color? ForeColor, Color? BackColor)>(),
                    new List<(string Header, TextAlignment Alignment, bool Visible)>(),
                    debug,
                    localColors
                );
            }

            // Serialize the first object to build the column template.
            string firstJson = JsonSerializer.Serialize(items[0]);
            var firstDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(firstJson);
            List<string> keys = new List<string>(firstDict!.Keys);

            // Build header entries: if the sample value is numeric, right-align; otherwise, left-align.
            var headerEntries = new List<(string Header, TextAlignment Alignment, bool Visible)>();
            foreach (var key in keys)
            {
                JsonElement element = firstDict[key];
                TextAlignment alignment =
                    (element.ValueKind == JsonValueKind.Number)
                        ? TextAlignment.Right
                        : TextAlignment.Left;
                headerEntries.Add((key, alignment, true));
            }
            // Append an extra header for the full JSON (typically hidden).
            headerEntries.Add(("JSON", TextAlignment.Left, false));

            // Build the search catalogue.
            var searchCatalogue = new List<(string[] Cells, Color? ForeColor, Color? BackColor)>();
            foreach (T item in items)
            {
                string jsonText = JsonSerializer.Serialize(item);
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonText);
                var cells = new List<string>();

                // For each key (using the same order as the header), output ONLY the cell's value.
                foreach (var key in keys)
                {
                    if (dict!.TryGetValue(key, out JsonElement element))
                    {
                        // If the element is numeric, output the raw numeric string.
                        // Otherwise, output the value as a string.
                        string cell = ConvertJsonElementToString(element);
                        cells.Add(cell);
                    }
                    else
                    {
                        cells.Add("null");
                    }
                }
                // Append the full JSON representation in the final (hidden) column.
                cells.Add(jsonText);
                searchCatalogue.Add((cells.ToArray(), null, null));
            }

            return new SearchPanelEto(searchCatalogue, headerEntries, debug, localColors);
        }

        /// <summary>
        /// Converts a JsonElement to its string representation.
        /// For numeric types, it returns the raw numeric string.
        /// For string types and booleans, it returns the value.
        /// For objects, arrays, or other types, it returns the raw JSON.
        /// </summary>
        private static string ConvertJsonElementToString(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString() ?? "null";
                case JsonValueKind.Number:
                    return element.GetRawText();
                case JsonValueKind.True:
                    return "True";
                case JsonValueKind.False:
                    return "False";
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    return element.GetRawText();
                default:
                    return "null";
            }
        }
    }
}
