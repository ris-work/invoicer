using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using Eto.Drawing;
using Eto.Forms;
//using Microsoft.EntityFrameworkCore.Diagnostics;
using RV.InvNew.Common;
using CommonUi;

namespace CommonUi
{
    public static class Extensions
    {
        // Add these new methods at the class level (before the constructor)
        private static ConcurrentDictionary<string, string> _normalizationCache = new ConcurrentDictionary<string, string>();
        private static readonly object _searchLock = new object();
        public static string PadRightOrClamp(
            this string input,
            int totalWidth,
            char paddingChar = ' '
        )
        {
            if (input.Length > totalWidth)
            {
                return input.Substring(0, totalWidth); // Clamp the string
            }

            return input.PadRight(totalWidth, paddingChar); // Pad the string
        }

        public static string PadLeftOrClamp(
            this string input,
            int totalWidth,
            char paddingChar = ' '
        )
        {
            if (input.Length > totalWidth)
            {
                return input.Substring(0, totalWidth); // Clamp the string
            }

            return input.PadLeft(totalWidth, paddingChar); // Pad the string
        }

        public static IEnumerable<string> FilterWithOptions<T>(
            ref T Input,
            string s,
            bool CaseInsensitive = true,
            bool Contains = false
        )
            where T : IEnumerable<string>
        {
            IEnumerable<string> A = Input;
            if (CaseInsensitive == true)
            {
                A = A.Select(x => x.ToLowerInvariant());
            }
            if (Contains == true)
            {
                return A.Where(x => x.Contains(s));
            }
            else
                return A.Where(x => x.StartsWith(s));
        }

        // Improved normalization method that's more i18n-friendly
        public static string NormalizeSpelling(this string s)
        {
            // First check if we've already normalized this string
            if (_normalizationCache.TryGetValue(s, out var cached))
                return cached;

            // Use StringInfo to handle Unicode grapheme clusters properly
            var sb = new StringBuilder();
            var enumerator = StringInfo.GetTextElementEnumerator(s.ToLowerInvariant());

            while (enumerator.MoveNext())
            {
                var element = enumerator.GetTextElement();
                // Remove common vowels and similar-sounding characters across languages
                if (!"aeiouáéíóúàèìòùâêîôûäëïöüãõåæœ".Contains(element))
                {
                    // Replace similar-sounding consonants across languages
                    switch (element)
                    {
                        case "k":
                        case "c":
                        case "ç":
                        case "q":
                            sb.Append("c");
                            break;
                        case "i":
                        case "y":
                        case "j":
                            sb.Append("i");
                            break;
                        case "s":
                        case "ś":
                        case "š":
                        case "ş":
                        case "ß":
                        case "с":
                            sb.Append("s");
                            break;
                        case "ph":
                        case "f":
                        case "ф":
                            sb.Append("f");
                            break;
                        case "g":
                        case "ğ":
                        case "г":
                            sb.Append("g");
                            break;
                        default:
                            sb.Append(element);
                            break;
                    }
                }
            }

            var result = sb.ToString();
            _normalizationCache.TryAdd(s, result);
            return result;
        }

        public static bool FilterAccordingly(
            this string str,
            string s,
            bool CaseInsensitive = true,
            bool Contains = true,
            bool normalizeSpelling = true,
            bool AnythingAnywhere = false
        )
        {
            if (s.Length == 0)
                return true;
            string x = str;
            string cs = s;
            if (CaseInsensitive == true)
            {
                x = x.ToLowerInvariant();
                cs = cs.ToLowerInvariant();
            }
            if (normalizeSpelling == true)
            {
                x = x.NormalizeSpelling();
                cs = cs.NormalizeSpelling();
            }
            if (AnythingAnywhere == true)
            {
                return cs.Split(" ").All(seg => x.Contains(seg));
            }
            if (Contains == true)
            {
                return x.Contains(cs);
            }
            else
                return x.StartsWith(cs);
        }
    }

    public class SearchDialogEto : Dialog, RV.InvNew.CommonUi.IButtonChooserInput
    {
        private string[] _Selected = null;
        private List<string[]> _OutputList = new List<string[]>() { };
        public string[] Selected
        {
            get => _Selected;
        }
        public int SelectedOrder = -1;
        public bool ReverseSelection = false;
        public bool Cancelled = false;
        public List<string[]> OutputList
        {
            get => _OutputList;
        }
        public delegate void SendTextBoxAndSelectedCallback(string message, string[] selected);
        // REPORT BUTTON: Used like Facebook Report button, something that should be reported to the manager, or the law enforcement and NOT GENERAL SUCCESSFUL ENTRY.
        // For that, use the normal Selected and OutputList instead, they are guaranteed to be valid, and null in case nothing is selected.
        public SearchPanelEto.SendTextBoxAndSelectedCallback CallbackWhenReportButtonIsClicked = null;
        public string ReportSelectedButtonText = "Report Selected";
        private static readonly object _searchLock = new object();



        // Add a field to hold the SearchPanelEto instance
        private SearchPanelEto _searchPanel;

        // Add a public method to trigger search using the delegate
        public void TriggerSearch()
        {
            _searchPanel.SearchNow?.Invoke();
        }

        public SearchDialogEto(
            List<(
                string[] SearchItems,
                Eto.Drawing.Color? BackgroundColor,
                Eto.Drawing.Color? ForegroundColor
            )> SC,
            List<(string Title, TextAlignment Alignment, bool)> HeaderEntries,
            bool Debug = false
        )
        {
            // Set dialog properties
            Title = "Search...";
            Resizable = true;
            Maximizable = true;

            // Create the SearchPanelEto with appropriate settings
            _searchPanel = new SearchPanelEto(
                SC,
                HeaderEntries,
                Debug,
                null, // Use default colors
                600,  // GWW - same as in SearchDialogEto
                700,   // GWH - same as in SearchDialogEto
                        showExportOptions: true,
        showSearchLocationInString: true,
        showSearchLocation: true,
        showSearchNormalization: true,
        showSearchCaseSensitivity: true,
        showPrintOptions: true,
        showReportButton: true
            );

            // Ensure all features are visible to match SearchDialogEto behavior
            _searchPanel.ShowExportOptions = true;
            _searchPanel.ShowSearchLocationInString = true;
            _searchPanel.ShowSearchLocation = true;
            _searchPanel.ShowSearchNormalization = true;
            _searchPanel.ShowSearchCaseSensitivity = true;
            _searchPanel.ShowPrintOptions = true;

            // Set the report button text
            _searchPanel.ReportSelectedButtonText = ReportSelectedButtonText;

            // Set the callback to update the title
            _searchPanel.OnUpdateTitle = (count) => {
                Title = $"Found {count} ";
            };

            // Handle selection to close the dialog and set results
            _searchPanel.OnSelectionMade = () =>
            {
                _Selected = _searchPanel.Selected;
                _OutputList = _searchPanel.OutputList;
                SelectedOrder = _searchPanel.SelectedOrder;
                ReverseSelection = _searchPanel.ReverseSelection;

                // Close the dialog
                this.Close();
            };

            // Set the callback for the report button
            _searchPanel.CallbackWhenReportButtonIsClicked = CallbackWhenReportButtonIsClicked;

            // Handle key events to close on Escape
            KeyDown += (sender, e) =>
            {
                if (e.Key == Keys.Escape)
                {
                    Cancelled = true;
                    Close();
                }
            };

            // Set the content to be the search panel
            Content = _searchPanel;

            // Set initial size
            Size = new Size(800, 800);

            // Initial search
            TriggerSearch();
        }

        // Helper method for filtering strings
        private static bool FilterString(string source, string search, bool caseInsensitive,
            bool contains, bool anythingAnywhere)
        {
            if (caseInsensitive)
            {
                source = source.ToLowerInvariant();
                search = search.ToLowerInvariant();
            }

            if (anythingAnywhere)
            {
                return search.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .All(seg => source.Contains(seg));
            }

            return contains ? source.Contains(search) : source.StartsWith(search);
        }
    }
}
