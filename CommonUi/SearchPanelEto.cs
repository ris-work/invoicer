using System;
using System.Collections.Generic;
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

namespace CommonUi
{
    /*public static class Extensions
    {
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

        public static string NormalizeSpelling(this string s)
        {
            var o = Regex.Replace(s, "[aeiouh]", ""); //English has 10+ vowels, mgiht as well remove all of them.
            o = o.Replace("k", "c").Replace("i", "y"); //K is the C equivalent from Greek, Y is the I equivalent from Greek.
            //Hope these are the last totally redundant letters...
            return o;
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
    }*/

    public class SearchPanelEto : Panel, RV.InvNew.CommonUi.IButtonChooserInput
    {
        public Action OnSelectionMade;
        private string[] _Selected = null;
        private List<string[]> _OutputList = new List<string[]>() { };
        public bool ShowExportOptions = false;
        public bool ShowSearchLocationInString = true;
        public bool ShowSearchLocation = true;
        public bool ShowSearchNormalization = false;
        public bool ShowSearchCaseSensitivity = false;
        public bool ShowPrintOptions = false;
        public string[] Selected
        {
            get => _Selected;
        }
        public int SelectedOrder = -1;
        public bool ReverseSelection = false;
        public List<string[]> OutputList
        {
            get => _OutputList;
        }
        public delegate void SendTextBoxAndSelectedCallback(string message, string[] selected);
        public SendTextBoxAndSelectedCallback CallbackWhenReportButtonIsClicked = null;
        public string ReportSelectedButtonText = TranslationHelper.Translate(
            "ReportSelected",
            "Report Selected",
            TranslationHelper.Lang
        );

        public SearchPanelEto(
            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SC,
            List<(string, TextAlignment, bool)> HeaderEntries,
            bool Debug = true,
            PanelSettings? LocalColors = null
        )
        {
            var Colors = new PanelSettings()
            {
                AlternatingColor1 =
                    LocalColors?.AlternatingColor1 ?? ColorSettings.AlternatingColor1,
                AlternatingColor2 =
                    LocalColors?.AlternatingColor2 ?? ColorSettings.AlternatingColor2,
                SelectedColumnColor =
                    LocalColors?.SelectedColumnColor ?? ColorSettings.SelectedColumnColor,
                LesserForegroundColor =
                    LocalColors?.LesserForegroundColor ?? ColorSettings.LesserForegroundColor,
                LesserBackgroundColor =
                    LocalColors?.LesserBackgroundColor ?? ColorSettings.LesserBackgroundColor,
                ForegroundColor = LocalColors?.ForegroundColor ?? ColorSettings.ForegroundColor,
                BackgroundColor = LocalColors?.BackgroundColor ?? ColorSettings.BackgroundColor,
            };
            IEnumerable<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> OptimizedCatalogue;
            OptimizedCatalogue = SC.Select(e =>
                    (
                        e.Item1.ToList()
                            .Concat(new List<string> { String.Join(",", e.Item1) })
                            .ToArray(),
                        e.Item2,
                        e.Item3
                    )
                )
                .ToArray();
            TableLayout TL = new TableLayout();
            Label SL = new Label()
            {
                Text = TranslationHelper.Translate(
                    "SearchFor",
                    "Search for: ",
                    TranslationHelper.Lang
                ),
            };
            Label LabelResults = new Label()
            {
                Text = TranslationHelper.Translate("Results", "Results: ", TranslationHelper.Lang),
            };
            GridView Results = new GridView();
            //Results.ApplyDarkThemeScrollBars();
            Results.DisableLines();
            Results.ApplyDarkGridHeaders();
            Results.ConfigureForPlatform();
            //this.ApplyDark
            RadioButtonList RBLSearchCriteria = new RadioButtonList()
            {
                Orientation = Eto.Forms.Orientation.Vertical,
                Padding = 5,
                BackgroundColor = Colors.BackgroundColor,
                TextColor = Colors.ForegroundColor,
            };
            RadioButtonList RBLSearchCaseSensitivity = new RadioButtonList()
            {
                Orientation = Eto.Forms.Orientation.Vertical,
                Padding = 5,
                BackgroundColor = Colors.BackgroundColor,
                TextColor = Colors.ForegroundColor,
            };
            RadioButtonList RBLSearchPosition = new RadioButtonList()
            {
                Orientation = Eto.Forms.Orientation.Vertical,
                Padding = 5,
                BackgroundColor = Colors.BackgroundColor,
                TextColor = Colors.ForegroundColor,
            };
            RBLSearchCaseSensitivity.Items.Add(
                TranslationHelper.Translate(
                    "CaseInsensitiveF1",
                    "Case-insensitive [F1]",
                    TranslationHelper.Lang
                )
            );
            RBLSearchCaseSensitivity.Items.Add(
                TranslationHelper.Translate(
                    "CaseSensitiveF2",
                    "Case-sensitive [F2]",
                    TranslationHelper.Lang
                )
            );
            RBLSearchPosition.Items.Add(
                TranslationHelper.Translate("ContainsF3", "Contains [F3]", TranslationHelper.Lang)
            );
            RBLSearchPosition.Items.Add(
                TranslationHelper.Translate(
                    "StartsWithF4",
                    "StartsWith [F4]",
                    TranslationHelper.Lang
                )
            );
            bool SearchCaseSensitive = false;
            bool SearchContains = true;
            CheckBox CBNormalizeSpelling = new CheckBox()
            {
                Text = TranslationHelper.Translate(
                    "NormalizeSpellingEND",
                    "Normalize spelling [END]",
                    TranslationHelper.Lang
                ),
                BackgroundColor = Colors.BackgroundColor,
                TextColor = Colors.ForegroundColor,
            };
            CheckBox CBAnythingAnywhere = new CheckBox()
            {
                Text = TranslationHelper.Translate(
                    "AnythingAnywhereBRK",
                    "Anything Anywhere [BRK]",
                    TranslationHelper.Lang
                ),
                BackgroundColor = Colors.BackgroundColor,
                TextColor = Colors.ForegroundColor,
            };
            bool NormalizeSpelling = false;
            bool AnythingAnywhere = false;
            bool ReverseSort = false;

            CBNormalizeSpelling.CheckedChanged += (e, a) =>
            {
                NormalizeSpelling = CBNormalizeSpelling.Checked ?? false;
            };
            CBAnythingAnywhere.CheckedChanged += (e, a) =>
            {
                AnythingAnywhere = CBAnythingAnywhere.Checked ?? false;
            };

            GroupBox SearchCriteria = new()
            {
                Text = TranslationHelper.Translate(
                    "SearchIn",
                    "Search in...",
                    TranslationHelper.Lang
                ),
                Content = RBLSearchCriteria,
            };
            GroupBox SearchCaseSensitivity = new()
            {
                Text = TranslationHelper.Translate(
                    "CaseSensitivitySetting",
                    "Case sensitivity setting",
                    TranslationHelper.Lang
                ),
                Content = RBLSearchCaseSensitivity,
                BackgroundColor = Colors.BackgroundColor,
                TextColor = Colors.ForegroundColor,
            };
            GroupBox SearchCasePosition = new()
            {
                Text = TranslationHelper.Translate(
                    "SearchPosition",
                    "Search Position",
                    TranslationHelper.Lang
                ),
                Content = RBLSearchPosition,
                BackgroundColor = Colors.BackgroundColor,
                TextColor = Colors.ForegroundColor,
            };
            GroupBox SearchSpellingNormalization = new()
            {
                Text = TranslationHelper.Translate(
                    "Advanced",
                    "Advanced...",
                    TranslationHelper.Lang
                ),
                Content = new StackLayout(CBNormalizeSpelling, CBAnythingAnywhere)
                {
                    Orientation = Eto.Forms.Orientation.Vertical,
                },
                BackgroundColor = Colors.BackgroundColor,
                TextColor = Colors.ForegroundColor,
            };

            StackLayout SearchOptions = new StackLayout()
            {
                Items =
                {
                    ShowSearchCaseSensitivity ? SearchCaseSensitivity : null,
                    ShowSearchLocationInString ? SearchCasePosition : null,
                    ShowSearchLocation ? SearchCriteria : null,
                    ShowSearchNormalization ? SearchSpellingNormalization : null,
                },
                Orientation = Eto.Forms.Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = 5,
            };

            Button ExportAllResultsAsCsv = new Button()
            {
                Text = TranslationHelper.Translate(
                    "ExportResults",
                    "Export Results...",
                    TranslationHelper.Lang
                ),
            };
            Button ExportAllAsCsv = new Button()
            {
                Text = TranslationHelper.Translate(
                    "ExportEverything",
                    "Export Everything...",
                    TranslationHelper.Lang
                ),
            };
            Button ExportShownAsCsv = new Button()
            {
                Text = TranslationHelper.Translate(
                    "ExportDisplayed",
                    "Export Displayed...",
                    TranslationHelper.Lang
                ),
            };
            Button ReportSelectedAndSearch = new Button() { Text = ReportSelectedButtonText };

            StackLayout ExportOptions = new StackLayout()
            {
                Items =
                {
                    ExportAllAsCsv,
                    ExportAllResultsAsCsv,
                    ExportShownAsCsv,
                    ReportSelectedAndSearch,
                },
                Orientation = Eto.Forms.Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = 5,
            };

            GroupBox GBExportOptions = new GroupBox()
            {
                Text = TranslationHelper.Translate(
                    "ExportOptions",
                    "Export options...",
                    TranslationHelper.Lang
                ),
                Content = ExportOptions,
            };

            Button PrintAllDisplayed = new Button()
            {
                Text = TranslationHelper.Translate(
                    "PrintDisplayed",
                    "Print Displayed Results...",
                    TranslationHelper.Lang
                ),
            };

            StackLayout PrintOptions = new StackLayout()
            {
                Items = { PrintAllDisplayed },
                Orientation = Eto.Forms.Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = 5,
            };

            GroupBox GBPrintOptions = new GroupBox()
            {
                Text = TranslationHelper.Translate(
                    "PrintingOptions",
                    "Printing options...",
                    TranslationHelper.Lang
                ),
                Content = PrintOptions,
            };

            int SelectedSearchIndex = SC[0].Item1.Length;
            RBLSearchCriteria.SelectedIndexChanged += (e, a) =>
            {
                SelectedSearchIndex = RBLSearchCriteria.SelectedIndex;
                //MessageBox.Show(SelectedSearchIndex.ToString(), "SelectedSearchIndex");
            };
            RBLSearchCaseSensitivity.SelectedIndexChanged += (e, a) =>
            {
                SearchCaseSensitive = RBLSearchCaseSensitivity.SelectedIndex == 1;
            };
            RBLSearchPosition.SelectedIndexChanged += (e, a) =>
            {
                SearchContains = RBLSearchPosition.SelectedIndex == 0;
            };
            TextAlignment[] Alignments = new TextAlignment[HeaderEntries.Count];
            Alignments = HeaderEntries.Select((x) => x.Item2).ToArray();
            int ic = 0;
            int fnKey = 0;
            int SortBy = 0;
            foreach (var Header in HeaderEntries)
            {
                var HI = new GridColumn
                {
                    HeaderText = Header.Item1,
                    DataCell = new TextBoxCell(ic) { TextAlignment = Header.Item2 },
                    HeaderTextAlignment = Header.Item2,
                    Sortable = true,
                    MinWidth = 40,
                };

                Results.Columns.Add(HI);

                ic++;
                fnKey = 4 + ic;
                RBLSearchCriteria.Items.Add(Header.Item1 + $" [F{fnKey}]");
            }

            Results.Enabled = true;
            Results.ApplyDarkGridHeaders();
            //Results.ApplyDarkThemeScrollBars2();
            //Results.ForceDarkThemeScrollBarArrows();
            //Results.ForceDarkThemeScrollBarThumbs();
            //ApplyScrollBarLineButtonStyle();
            //ApplyScrollBarThumbStyle();
            Results.BackgroundColor = Colors.LesserBackgroundColor;
            Results.Size = new Size(600, 300);
            (Eto.Drawing.Color?, Eto.Drawing.Color?)[] ColorMat = Array.Empty<(
                Eto.Drawing.Color?,
                Eto.Drawing.Color?
            )>();
            Results.CellFormatting += (e, a) =>
            {
                a.Font = Eto.Drawing.Fonts.Monospace(11);
                a.BackgroundColor = Colors.BackgroundColor;
                a.ForegroundColor = Colors.ForegroundColor;
                //Colour the column first
                if (a.Column.DisplayIndex % 2 == 1)
                {
                    a.BackgroundColor = Colors.AlternatingColor2;
                    a.ForegroundColor = Colors.ForegroundColor;
                }
                //Override with row colours
                if (a.Row % 2 == 0)
                {
                    a.BackgroundColor = Colors.AlternatingColor1;
                    a.ForegroundColor = Colors.ForegroundColor;
                }
                //Use color matrix now!
                if (
                    ColorMat != null
                    && Results.DataStore != null
                    && Results.DataStore.Count() <= ColorMat.Length
                )
                {
                    if (ColorMat[a.Row].Item1 != null)
                        a.BackgroundColor = (Eto.Drawing.Color)ColorMat[a.Row].Item1!;
                    if (ColorMat[a.Row].Item2 != null)
                        a.ForegroundColor = (Eto.Drawing.Color)ColorMat[a.Row].Item2!;
                }
                ;
                //Override everything with the currently sorted column
                if (a.Column.DisplayIndex == SortBy)
                {
                    a.Column.AutoSize = true;
                    a.BackgroundColor = ReverseSort
                        ? Colors.ForegroundColor
                        : Colors.SelectedColumnColor;
                    a.ForegroundColor = ReverseSort
                        ? Colors.SelectedColumnColor
                        : Colors.ForegroundColor;
                    a.Font = Eto.Drawing.Fonts.Monospace(11, FontStyle.Bold);
                }
                if (a.Row == Results.SelectedRow)
                {
                    a.Column.AutoSize = true;
                    a.BackgroundColor = Colors.SelectedColumnColor;
                    a.ForegroundColor = Colors.ForegroundColor;
                    a.Font = Eto.Drawing.Fonts.Monospace(11, FontStyle.Bold);
                }
            };
            Results.KeyUp += (e, a) =>
            {
                Results.Invalidate(true);
            };
            Results.MouseUp += (e, a) =>
            {
                Results.Invalidate(true);
            };
            Results.RowFormatting += (e, a) =>
            {
                a.BackgroundColor = Colors.LesserBackgroundColor;
            };
            Results.Border = BorderType.None;
            Results.GridLines = GridLines.None;

            RBLSearchCriteria.Items.Add($"Omnibox [F{fnKey + 1}]");
            TextBox SearchBox = new TextBox()
            {
                BackgroundColor = Colors.BackgroundColor,
                TextColor = Colors.ForegroundColor,
            };
            if (Debug)
                MessageBox.Show($"PC: {SC.Count()}");
            bool searching = false;
            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> Filtered =
                new List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>();
            IEnumerable<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> FilteredUnlim =
                new List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>();
            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> FilteredTemp =
                new List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>();

            if (Debug)
                MessageBox.Show($"Last: {SC.Last().Item1[0]} Desc: {SC.Last().Item1[1]}");
            TL.Padding = 10;
            TL.Spacing = new Eto.Drawing.Size(10, 10);
            int FilteredCount = 0;
            Results.Border = BorderType.None;
            Results.GridLines = GridLines.Both;
            var UpdateView = () =>
            {
                Filtered = FilteredTemp;
                var ColorMatTemp = new List<(Eto.Drawing.Color?, Eto.Drawing.Color?)>();
                List<GridItem> GI = new List<GridItem>();
                Filtered = FilteredTemp;
                foreach (var item in Filtered)
                {
                    GI.Add(new GridItem(item.Item1));
                    ColorMatTemp.Add((item.Item2, item.Item3));
                }
                ColorMat = ColorMatTemp.ToArray();
                //MessageBox.Show(GI.Count().ToString());
                Results.DataStore = GI;
                Results.Invalidate(true);
                Results.UpdateLayout();

                this.Invalidate();
                //this.Title = $"Found {FilteredCount} ";
            };
            EventHandler<MouseEventArgs> SendSelected = (e, a) =>
            {
                ReverseSelection = ReverseSort;
                SelectedOrder = SortBy;
                _OutputList = Filtered.Select(a => a.Item1).ToList();
                if (Results.SelectedItem != null)
                {
                    this._Selected = (string[])((GridItem)Results.SelectedItem).Values;
                    //this.Close();
                    OnSelectionMade();
                }
                else if (Results.DataStore != null && Results.DataStore.Count() != 0)
                {
                    this._Selected = (string[])((GridItem)Results.DataStore.First()).Values;
                    //this.Close();
                    OnSelectionMade();
                }
                else
                {
                    MessageBox.Show(
                        "Nothing displayed, nothing selected; [Esc] to exit the search dialog",
                        "Error",
                        MessageBoxType.Warning
                    );
                }
            };
            EventHandler<KeyEventArgs> SendSelectedOnEnter = (e, a) =>
            {
                ReverseSelection = ReverseSort;
                SelectedOrder = SortBy;
                _OutputList = Filtered.Select(a => a.Item1).ToList();
                if (Results.SelectedItem != null)
                {
                    this._Selected = (string[])((GridItem)Results.SelectedItem).Values;
                    //this.Close();
                    OnSelectionMade();
                }
                else if (Results.DataStore != null && Results.DataStore.Count() != 0)
                {
                    this._Selected = (string[])((GridItem)Results.DataStore.First()).Values;
                    //this.Close();
                    OnSelectionMade();
                }
                else
                {
                    MessageBox.Show(
                        "Nothing displayed, nothing selected; [Esc] to exit the search dialog",
                        "Error",
                        MessageBoxType.Warning
                    );
                }
            };
            var SendSelectedWithoutDefaults = () =>
            {
                ReverseSelection = ReverseSort;
                SelectedOrder = SortBy;
                _OutputList = Filtered.Select(a => a.Item1).ToList();
                if (Results.SelectedItem != null)
                {
                    this._Selected = (string[])((GridItem)Results.SelectedItem).Values;
                    //this.Close();
                    OnSelectionMade();
                }
                else if (Results.DataStore != null && Results.DataStore.Count() != 0)
                {
                    this._Selected = (string[])((GridItem)Results.DataStore.First()).Values;
                    //this.Close();
                    OnSelectionMade();
                }
                else
                {
                    MessageBox.Show(
                        "Nothing displayed, nothing selected; [Esc] to exit the search dialog",
                        "Error",
                        MessageBoxType.Warning
                    );
                }

                return;
            };
            Results.MouseDoubleClick += SendSelected;
            Results.KeyUp += (e, a) =>
            {
                if (a.Key == Keys.Enter)
                    SendSelectedOnEnter(e, a);
            };

            var Search = () =>
            {
                if (SearchBox.Text.Length > 0 && searching != true)
                {
                    var SelectedArrayIndex = SelectedSearchIndex;
                    var searchString = SearchBox.Text.ToLowerInvariant();
                    var SearchCaseSensitiveSetting = SearchCaseSensitive;
                    var SearchContainsSetting = SearchContains;
                    var SearchNormalizeSpelling = NormalizeSpelling;
                    int SearchSortBy = SortBy;
                    bool SearchAnythingAnywhere = AnythingAnywhere;
                    bool SortingIsNumeric = HeaderEntries[SearchSortBy].Item3;
                    //MessageBox.Show($"{SelectedArrayIndex}, {SC[0].Item1.Length}, SAMPLE: {String.Join(",", SC[0].Item1)}");
                    searching = true;
                    (
                        new Thread(() =>
                        {
                            if (SelectedArrayIndex >= SC[0].Item1.Length - 1)
                            {
                                //MessageBox.Show(SelectedArrayIndex.ToString(), "SelectedArrayIndex", MessageBoxType.Information);
                                var FilteredBeforeCountingAndSorting = OptimizedCatalogue
                                    .AsParallel()
                                    .Where(
                                        (x) =>
                                            x
                                                .Item1.Last()
                                                .FilterAccordingly(
                                                    searchString,
                                                    !SearchCaseSensitiveSetting,
                                                    SearchContainsSetting,
                                                    SearchNormalizeSpelling,
                                                    SearchAnythingAnywhere
                                                )
                                    )
                                    .AsSequential();
                                var FilteredBeforeCounting = ReverseSort
                                    ? (
                                        SortingIsNumeric
                                            ? FilteredBeforeCountingAndSorting.OrderByDescending(
                                                x => long.Parse(x.Item1[SearchSortBy])
                                            )
                                            : FilteredBeforeCountingAndSorting.OrderByDescending(
                                                x => x.Item1[SearchSortBy]
                                            )
                                    )
                                    : (
                                        SortingIsNumeric
                                            ? FilteredBeforeCountingAndSorting.OrderBy(x =>
                                                long.Parse(x.Item1[SearchSortBy])
                                            )
                                            : FilteredBeforeCountingAndSorting.OrderBy(x =>
                                                x.Item1[SearchSortBy]
                                            )
                                    );
                                FilteredUnlim = FilteredBeforeCountingAndSorting;
                                FilteredTemp = FilteredBeforeCounting.Take(500).ToList();
                                FilteredCount = FilteredBeforeCounting.Count();
                            }
                            else
                            {
                                var FilteredBeforeCountingAndSorting = SC.AsParallel()
                                    .Where(
                                        (x) =>
                                            x.Item1[SelectedSearchIndex]
                                                .FilterAccordingly(
                                                    searchString,
                                                    !SearchCaseSensitiveSetting,
                                                    SearchContainsSetting,
                                                    SearchNormalizeSpelling,
                                                    SearchAnythingAnywhere
                                                )
                                    )
                                    .AsSequential();
                                var FilteredBeforeCounting = ReverseSort
                                    ? (
                                        SortingIsNumeric
                                            ? FilteredBeforeCountingAndSorting.OrderByDescending(
                                                x => long.Parse(x.Item1[SearchSortBy])
                                            )
                                            : FilteredBeforeCountingAndSorting.OrderByDescending(
                                                x => x.Item1[SearchSortBy]
                                            )
                                    )
                                    : (
                                        SortingIsNumeric
                                            ? FilteredBeforeCountingAndSorting.OrderBy(x =>
                                                long.Parse(x.Item1[SearchSortBy])
                                            )
                                            : FilteredBeforeCountingAndSorting.OrderBy(x =>
                                                x.Item1[SearchSortBy]
                                            )
                                    );
                                FilteredUnlim = FilteredBeforeCountingAndSorting;
                                FilteredTemp = FilteredBeforeCounting.Take(500).ToList();
                                FilteredCount = FilteredBeforeCounting.Count();
                            }

                            searching = false;
                            Application.Instance.Invoke(UpdateView);
                        })
                    ).Start();
                }
            };
            var ResultsContainer = new StackLayout()
            {
                Items = { Results },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
            var OthersContainer = new StackLayout()
            {
                Items =
                {
                    SearchOptions,
                    ShowExportOptions ? GBExportOptions : null,
                    ShowPrintOptions ? GBPrintOptions : null,
                },
                Orientation = Eto.Forms.Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
            TL.Rows.Add(new TableRow(SearchBox));
            //TL.Rows.Add(new TableRow(LabelResults));
            TL.Rows.Add(
                new TableRow(new TableCell(ResultsContainer) { ScaleWidth = true }, OthersContainer)
                {
                    ScaleHeight = true,
                }
            );
            Content = TL;
            RBLSearchCriteria.SelectedIndex = RBLSearchCriteria.Items.Count - 1;
            RBLSearchCaseSensitivity.SelectedIndex = 0;
            RBLSearchPosition.SelectedIndex = 0;
            //Title = "Search...";
            //Resizable = true;
            //Maximizable = true;
            Results.ColumnHeaderClick += (e, a) =>
            {
                //MessageBox.Show(a.Column.DisplayIndex.ToString(), "Header was clicked!");
                if (SortBy == a.Column.DisplayIndex)
                    ReverseSort = !ReverseSort;
                SortBy = a.Column.DisplayIndex;
                Search();
            };
            SearchBox.TextChanged += (_, _) => Search();
            SearchBox.KeyDown += (_, e) =>
            {
                if (e.Key == Keys.Down)
                {
                    Results.Focus();
                }
            };
            SearchBox.DisableTextBoxDownArrow(() =>
            {
                if (Results.DataStore.Count() > Results.SelectedRow + 1)
                    Results.SelectedRow++;
            });

            this.SizeChanged += (_, _) =>
            {
                if (this.Height > 200)
                {
                    Results.Height = (int)Math.Floor(this.Height * 0.85);
                }
            };
            var WriteCsv = (
                List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> Entries,
                List<(string, TextAlignment, bool)> Headers,
                string FileName
            ) =>
            {
                using (StreamWriter SW = new StreamWriter(FileName))
                {
                    using (
                        CsvWriter CSW = new CsvWriter(
                            SW,
                            System.Globalization.CultureInfo.InvariantCulture
                        )
                    )
                    {
                        var HeaderLabels = (HeaderEntries.Select(x => x.Item1).ToArray());
                        foreach (var HeaderLabel in HeaderLabels)
                        {
                            CSW.WriteField(HeaderLabel);
                        }
                        CSW.NextRecord();
                        var ArrOut = Entries.Select(x => x.Item1).ToArray();
                        foreach (var entry in ArrOut)
                        {
                            foreach (var EntryCell in entry)
                            {
                                CSW.WriteField(entry);
                            }
                            CSW.NextRecord();
                        }
                    }
                }
            };
            ExportAllAsCsv.Click += (_, _) =>
            {
                var SFD = new SaveFileDialog() { Title = "Save as..." };
                SFD.Filters.Add(
                    new FileFilter() { Extensions = ["*.csv"], Name = "Comma-Separated Values" }
                );
                SFD.ShowDialog(Application.Instance.MainForm);
                if (SFD.FileName != null)
                {
                    WriteCsv(SC, HeaderEntries, SFD.FileName);
                }
            };
            ExportAllResultsAsCsv.Click += (_, _) =>
            {
                var SFD = new SaveFileDialog() { Title = "Save as..." };
                SFD.Filters.Add(
                    new FileFilter() { Extensions = ["*.csv"], Name = "Comma-Separated Values" }
                );
                SFD.ShowDialog(Application.Instance.MainForm);
                if (SFD.FileName != null)
                {
                    WriteCsv(FilteredUnlim.ToList(), HeaderEntries, SFD.FileName);
                }
            };

            ExportShownAsCsv.Click += (_, _) =>
            {
                var SFD = new SaveFileDialog() { Title = "Save as..." };
                SFD.Filters.Add(
                    new FileFilter() { Extensions = ["*.csv"], Name = "Comma-Separated Values" }
                );
                SFD.ShowDialog(Application.Instance.MainForm);
                if (SFD.FileName != null)
                {
                    WriteCsv(FilteredTemp, HeaderEntries, SFD.FileName);
                }
            };

            PrintAllDisplayed.Click += (_, _) =>
            {
                var WhetherToPrint = MessageBox.Show(
                    $"{FilteredTemp.Count} lines, would you still like to print?",
                    "List might be too long",
                    MessageBoxButtons.YesNo,
                    MessageBoxType.Question
                );

                if (WhetherToPrint == DialogResult.Yes)
                {
                    var RP = new ReceiptPrinter(
                        FilteredTemp.Select(e => e.Item1).Reverse().Skip(2).Reverse().ToList(),
                        (IReadOnlyDictionary<string, object>)(new Dictionary<string, object>())
                    );
                    RP.PrintReceipt();
                }
            };

            if (ReportSelectedAndSearch != null)
            {
                ReportSelectedAndSearch.Click += (_, _) =>
                {
                    string[] SelectedList = new string[0];
                    if (Results.SelectedItem != null)
                    {
                        SelectedList = (string[])((GridItem)Results.SelectedItem).Values;
                    }
                    string SearchBoxText = SearchBox.Text;
                    if (CallbackWhenReportButtonIsClicked != null)
                        CallbackWhenReportButtonIsClicked(SearchBoxText, SelectedList);
                };
            }
            Results.DisableGridViewEnterKey(SendSelectedWithoutDefaults);
            EventHandler<Eto.Forms.KeyEventArgs> ProcessKeyDown = (_, ea) =>
            {
                ReverseSelection = ReverseSort;
                SelectedOrder = SortBy;
                KeyEventArgs a = (KeyEventArgs)ea;
                switch (a.Key)
                {
                    case Keys.F1:
                        RBLSearchCaseSensitivity.SelectedIndex = 0;
                        break;
                    case Keys.F2:
                        RBLSearchCaseSensitivity.SelectedIndex = 1;
                        break;
                    case Keys.F3:
                        RBLSearchPosition.SelectedIndex = 0;
                        break;
                    case Keys.F4:
                        RBLSearchPosition.SelectedIndex = 1;
                        break;
                    case Keys.F5:
                        if (RBLSearchCriteria.Items.Count >= 1)
                            RBLSearchCriteria.SelectedIndex = 0;
                        break;
                    case Keys.F6:
                        if (RBLSearchCriteria.Items.Count >= 2)
                            RBLSearchCriteria.SelectedIndex = 1;
                        break;
                    case Keys.F7:
                        if (RBLSearchCriteria.Items.Count >= 3)
                            RBLSearchCriteria.SelectedIndex = 2;
                        break;
                    case Keys.F8:
                        if (RBLSearchCriteria.Items.Count >= 4)
                            RBLSearchCriteria.SelectedIndex = 3;
                        break;
                    case Keys.F9:
                        if (RBLSearchCriteria.Items.Count >= 5)
                            RBLSearchCriteria.SelectedIndex = 4;
                        break;
                    case Keys.F10:
                        if (RBLSearchCriteria.Items.Count >= 6)
                            RBLSearchCriteria.SelectedIndex = 5;
                        break;
                    case Keys.F11:
                        if (RBLSearchCriteria.Items.Count >= 7)
                            RBLSearchCriteria.SelectedIndex = 6;
                        break;
                    case Keys.End:
                        CBNormalizeSpelling.Checked = !(CBNormalizeSpelling.Checked ?? false);
                        break;
                    case Keys.Pause:
                        CBAnythingAnywhere.Checked = !(CBAnythingAnywhere.Checked ?? false);
                        break;
                    case Keys.Enter:
                        if (!searching)
                        {
                            if (Results.SelectedItem != null)
                            {
                                this._OutputList = Filtered.Select(a => a.Item1).ToList();
                                this._Selected = (string[])((GridItem)Results.SelectedItem).Values;
                                //this.Close();
                                OnSelectionMade();
                            }
                            else if (Results.DataStore != null && Results.DataStore.Count() != 0)
                            {
                                this._OutputList = Filtered.Select(a => a.Item1).ToList();
                                this._Selected = (string[])
                                    ((GridItem)Results.DataStore.First()).Values;
                                //this.Close();
                                OnSelectionMade();
                            }
                            else
                            {
                                MessageBox.Show(
                                    "Nothing displayed, nothing selected; [Esc] to exit the search dialog",
                                    "Error",
                                    MessageBoxType.Warning
                                );
                            }
                        }

                        break;
                    case Keys.Escape:
                        //this.Close();
                        OnSelectionMade();
                        break;
                    default:
                        break;
                }
                //MessageBox.Show($"{RBLSearchCriteria.SelectedIndex.ToString()}", "SelectedIndex");
            };

            this.KeyDown += ProcessKeyDown;
        }
    }
}
