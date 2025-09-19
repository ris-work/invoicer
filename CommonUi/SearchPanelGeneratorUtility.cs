using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using Eto.Drawing;
using Eto.Forms;

namespace CommonUi
{
    public static class StringArrayExtensions
    {
        /// <summary>
        /// Returns a new array where the specified frontKeys appear first (in that order),
        /// followed by the rest of the source items (in their original order).
        /// </summary>
        public static string[] BringToFront(this string[] source, params string[] frontKeys)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (frontKeys == null || frontKeys.Length == 0)
                return source.ToArray();

            var seen = new HashSet<string>(frontKeys);
            var result = new List<string>(source.Length);

            // 1) Add any frontKeys that actually exist in source, in the order given
            foreach (var key in frontKeys)
            {
                if (Array.IndexOf(source, key) >= 0)
                    result.Add(key);
            }

            // 2) Append all items from source that weren't in frontKeys
            foreach (var item in source)
            {
                if (!seen.Contains(item))
                    result.Add(item);
            }

            return result.ToArray();
        }
    }

    public static class DictionaryExtensions
    {
        /// <summary>
        /// Returns an OrderedDictionary whose entries are:
        ///  1) the keys in frontKeys (in that order, if they exist in the source)
        ///  2) then all remaining entries in the original dictionary’s enumeration order
        /// </summary>
        public static OrderedDictionary OrderByKeys<K, V>(
            this IDictionary<K, V> source,
            IEnumerable<K> frontKeys
        )
        {
            var result = new OrderedDictionary();
            var seen = new HashSet<K>(frontKeys);

            // 1) add front‐keys in the requested order
            foreach (var key in frontKeys)
            {
                if (source.TryGetValue(key, out var value))
                    result.Add(key, value);
            }

            // 2) add the rest in whatever order the source gives them
            foreach (var kvp in source)
            {
                if (!seen.Contains(kvp.Key))
                    result.Add(kvp.Key, kvp.Value);
            }

            return result;
        }
    }

    //---------------------------------------------------------------------
    // AoT‑Friendly Search Panel Utility (No key prefix in cell values)
    //---------------------------------------------------------------------
    public static class SearchPanelUtility
    {
        public static string[]? GenerateSearchDialog<T>(
            List<T> items,
            Control owner,
            bool debug = false,
            PanelSettings? localColors = null,
            string[]? order = null
        )
        {
            var generated = new Dialog<string[]?>();

            var searchPanel = GenerateSearchPanel(items, debug, localColors, order);
            generated.Content = searchPanel;
            searchPanel.OnSelectionMade += () =>
            {
                generated.Close(searchPanel.Selected);
            };
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
            PanelSettings? localColors = null,
            string[]? order = null
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
            string[] keys = new List<string>(firstDict!.Keys).ToArray();
            if (order != null)
                keys = keys.BringToFront(order);
            else
                keys = keys.BringToFront([]);

            // Build header entries: if the sample value is numeric, right-align; otherwise, left-align.
            var headerEntries = new List<(string Header, TextAlignment Alignment, bool Visible)>();
            foreach (var key in keys)
            {
                JsonElement element = firstDict[key];
                TextAlignment alignment =
                    (element.ValueKind == JsonValueKind.Number)
                        ? TextAlignment.Right
                        : TextAlignment.Left;
                bool IsNumeric = (firstDict[key]).ValueKind is JsonValueKind.Number;
                headerEntries.Add((key, alignment, IsNumeric));
            }
            // Append an extra header for the full JSON (typically hidden).
            headerEntries.Add(("JSON", TextAlignment.Left, false));

            // Build the search catalogue.
            var searchCatalogue = new List<(string[] Cells, Color? ForeColor, Color? BackColor)>();
            foreach (T item in items)
            {
                string jsonText = JsonSerializer.Serialize(item);
                IDictionary<string, JsonElement> dict = JsonSerializer.Deserialize<
                    Dictionary<string, JsonElement>
                >(jsonText);

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
