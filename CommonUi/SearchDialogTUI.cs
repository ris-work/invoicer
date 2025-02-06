//using Eto.Forms;
//using Eto.Forms;
//using Eto.Forms;
//using Eto.Forms;
//using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Eto.Forms;
using EtoFE;
using Microsoft.EntityFrameworkCore;
using Terminal.Gui;

//using Eto.Forms;

namespace CommonUi
{
    public class SearchDialogTUI : Terminal.Gui.Toplevel
    {
        private string[] _Selected = null;
        private List<string[]> _OutputList = new List<string[]>() { };
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
        public string ReportSelectedButtonText = "Report Selected";

        public SearchDialogTUI(
            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SC,
            List<(string, Eto.Forms.TextAlignment, bool)> HeaderEntries
        )
        {
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
            View TL = new View() { Width = 80, Height = 24 };
            Label SL = new Label() { Text = "Search for: " };
            Label LabelResults = new Label() { Text = "Results: " };
            Terminal.Gui.Attribute TextFieldColors = new Terminal.Gui.Attribute(
                Terminal.Gui.Color.BrightGreen,
                Terminal.Gui.Color.Black
            );
            Terminal.Gui.Attribute TextFieldSelected = new Terminal.Gui.Attribute(
                Terminal.Gui.Color.Black,
                Terminal.Gui.Color.BrightGreen
            );
            TextField SearchBox = new TextField()
            {
                Width = 30,
                Y = Pos.Bottom(LabelResults),
                ColorScheme = new ColorScheme(
                    TextFieldColors,
                    TextFieldSelected,
                    TextFieldColors,
                    TextFieldColors,
                    TextFieldSelected
                ),
            };
            Terminal.Gui.Attribute TableColors = new Terminal.Gui.Attribute(
                Terminal.Gui.Color.BrightBlue,
                Terminal.Gui.Color.Black
            );
            Terminal.Gui.Attribute TableColorSelected = new Terminal.Gui.Attribute(
                Terminal.Gui.Color.Black,
                Terminal.Gui.Color.BrightGreen
            );
            Terminal.Gui.Attribute SearchOptionsColors = new Terminal.Gui.Attribute(
                Terminal.Gui.Color.BrightMagenta,
                Terminal.Gui.Color.Black
            );
            Terminal.Gui.Attribute SearchOptionSelected = new Terminal.Gui.Attribute(
                Terminal.Gui.Color.Black,
                Terminal.Gui.Color.BrightYellow
            );
            TableView Results = new TableView()
            {
                FullRowSelect = true,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                Y = Pos.Bottom(SearchBox) + 1,
                ColorScheme = new ColorScheme(
                    TableColors,
                    TableColorSelected,
                    TableColors,
                    TableColors,
                    TableColorSelected
                ),
                TextAlignment = Alignment.End,
            };
            RadioGroup RBLSearchCriteria = new RadioGroup()
            {
                Orientation = Orientation.Vertical,
                Width = Dim.Auto(),
                Height = Dim.Auto(),
            };
            RadioGroup RBLSearchCaseSensitivity = new RadioGroup()
            {
                Orientation = Orientation.Vertical,
                Width = Dim.Auto(),
                Height = Dim.Auto(),
            };
            RadioGroup RBLSearchPosition = new RadioGroup()
            {
                Orientation = Orientation.Vertical,
                Width = Dim.Auto(),
                Height = Dim.Auto(),
            };
            var RBLSearchCaseSensitivityLabels = new List<string>();
            RBLSearchCaseSensitivityLabels.Add("Case-insensitive [F1]");
            RBLSearchCaseSensitivityLabels.Add("Case-sensitive [F2]");
            RBLSearchCaseSensitivity.RadioLabels = RBLSearchCaseSensitivityLabels.ToArray();
            var RBLSearchPositionLabels = new List<string>();
            RBLSearchPositionLabels.Add("Contains [F3]");
            RBLSearchPositionLabels.Add("StartsWith [F4]");
            RBLSearchPosition.RadioLabels = RBLSearchPositionLabels.ToArray();
            bool SearchCaseSensitive = false;
            bool SearchContains = true;
            CheckBox CBNormalizeSpelling = new CheckBox() { Text = "Normalize spelling [END]" };
            CheckBox CBAnythingAnywhere = new CheckBox()
            {
                Text = "Anything Anywhere [BRK]",
                Y = Pos.Bottom(CBNormalizeSpelling),
            };
            bool NormalizeSpelling = false;
            bool AnythingAnywhere = false;
            bool ReverseSort = false;

            CBNormalizeSpelling.CheckedStateChanging += (e, a) =>
            {
                NormalizeSpelling =
                    CBNormalizeSpelling.CheckedState == CheckState.Checked ? false : true;
            };
            CBAnythingAnywhere.CheckedStateChanging += (e, a) =>
            {
                AnythingAnywhere =
                    CBAnythingAnywhere.CheckedState == CheckState.Checked ? false : true;
            };

            FrameView SearchCriteria = new() { Text = "Search in..." };

            var ResultsDT = new DataTable();

            Eto.Forms.TextAlignment[] Alignments = new Eto.Forms.TextAlignment[HeaderEntries.Count];
            List<string> RBLSearchCriteriaList = new List<string>();
            Alignments = HeaderEntries.Select((x) => x.Item2).ToArray();
            int ic = 0;
            int fnKey = 0;
            int SortBy = 0;
            foreach (var Header in HeaderEntries)
            {
                /*var HI = new GridColumn
                {
                    HeaderText = Header.Item1,
                    DataCell = new TextBoxCell(ic) { TextAlignment = Header.Item2 },
                    HeaderTextAlignment = Header.Item2,
                    Sortable = true,
                    MinWidth = 40,
                };*/

                //Results.Columns.Add(HI);
                ResultsDT.Columns.Add(new DataColumn() { ColumnName = Header.Item1 });

                ic++;
                fnKey = 4 + ic;
                RBLSearchCriteriaList.Add(Header.Item1 + $" [F{fnKey}]");
            }
            RBLSearchCriteriaList.Add($"Omnibox [F{fnKey + 1}]");
            Results.Table = new DataTableSource(ResultsDT);
            RBLSearchCriteria.RadioLabels = RBLSearchCriteriaList.ToArray();
            RBLSearchCriteria.Width = Dim.Auto();
            RBLSearchCriteria.Height = Dim.Auto();

            int SelectedSearchIndex = SC[0].Item1.Length;
            RBLSearchCriteria.SelectedItemChanged += (e, a) =>
            {
                SelectedSearchIndex = RBLSearchCriteria.SelectedItem;
                //MessageBox.Show(SelectedSearchIndex.ToString(), "SelectedSearchIndex");
            };
            RBLSearchCaseSensitivity.SelectedItemChanged += (e, a) =>
            {
                SearchCaseSensitive = RBLSearchCaseSensitivity.SelectedItem == 1;
            };
            RBLSearchPosition.SelectedItemChanged += (e, a) =>
            {
                SearchContains = RBLSearchPosition.SelectedItem == 0;
            };
            SearchCriteria.Add(RBLSearchCriteria);
            FrameView SearchCaseSensitivity = new() { Text = "Case sensitivity setting" };
            SearchCaseSensitivity.Add(RBLSearchCaseSensitivity);
            SearchCaseSensitivity.Width = Dim.Auto();
            FrameView SearchCasePosition = new() { Text = "Search Position" };
            SearchCasePosition.Add(RBLSearchPosition);
            SearchCasePosition.Width = Dim.Auto();
            FrameView SearchSpellingNormalization = new() { Text = "Advanced..." };
            SearchSpellingNormalization.Add(CBNormalizeSpelling);
            SearchSpellingNormalization.Add(CBAnythingAnywhere);
            SearchSpellingNormalization.Width = Dim.Auto();
            SearchSpellingNormalization.Height = Dim.Auto();

            bool searching = false;
            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> Filtered =
                new List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>();
            IEnumerable<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> FilteredUnlim =
                new List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>();
            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> FilteredTemp =
                new List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>();
            int FilteredCount = 0;

            var UpdateView = () =>
            {
                var ResultsDTU = new DataTable();
                foreach (var Header in HeaderEntries)
                {
                    /*var HI = new GridColumn
                    {
                        HeaderText = Header.Item1,
                        DataCell = new TextBoxCell(ic) { TextAlignment = Header.Item2 },
                        HeaderTextAlignment = Header.Item2,
                        Sortable = true,
                        MinWidth = 40,
                    };*/

                    //Results.Columns.Add(HI);
                    ResultsDTU.Columns.Add(new DataColumn() { ColumnName = Header.Item1 });
                }
                Filtered = FilteredTemp;
                var ColorMatTemp = new List<(Eto.Drawing.Color?, Eto.Drawing.Color?)>();
                //List<GridItem> GI = new List<GridItem>();
                Filtered = FilteredTemp;
                foreach (var item in Filtered)
                {
                    ResultsDTU.Rows.Add(item.Item1.Take(ResultsDTU.Columns.Count).ToArray());
                    ColorMatTemp.Add((item.Item2, item.Item3));
                }
                //ColorMat = ColorMatTemp.ToArray();
                //MessageBox.Show(GI.Count().ToString());
                //Results.DataStore = GI;
                //Results.Invalidate(true);
                //Results.UpdateLayout();

                //this.Invalidate();
                this.Title = $"Found {FilteredCount} ";
                Results.Table = new DataTableSource(ResultsDTU);
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
                            UpdateView();
                        })
                    ).Start();
                }
            };
            SearchBox.TextChanged += (_, _) => Search();

            FrameView SearchOptions = new FrameView()
            {
                ColorScheme = new ColorScheme(
                    SearchOptionsColors,
                    SearchOptionSelected,
                    SearchOptionsColors,
                    SearchOptionsColors,
                    SearchOptionSelected
                ),
            };
            SearchCasePosition.Y = Pos.Bottom(SearchCaseSensitivity) + 1;
            SearchCriteria.Y = Pos.Bottom(SearchCasePosition) + 1;
            SearchSpellingNormalization.Y = Pos.Bottom(SearchCriteria) + 1;
            SearchCaseSensitivity.Width = Dim.Auto();
            SearchCaseSensitivity.Height = Dim.Auto();
            SearchCasePosition.Width = Dim.Auto();
            SearchCasePosition.Height = Dim.Auto();
            SearchCriteria.Width = Dim.Auto();
            SearchCriteria.Height = Dim.Auto();
            SearchSpellingNormalization.Width = Dim.Auto();
            SearchSpellingNormalization.Height = Dim.Auto();
            SearchOptions.Add(
                SearchCaseSensitivity,
                SearchCasePosition,
                SearchCriteria,
                SearchSpellingNormalization
            );

            SearchOptions.Y = Pos.AnchorEnd() - 1;
            SearchOptions.X = Pos.AnchorEnd() - 1;
            SearchOptions.Width = Dim.Auto();
            SearchOptions.Height = Dim.Auto();
            SearchCasePosition.Y = Pos.Bottom(SearchCaseSensitivity) + 1;
            SearchCriteria.Y = Pos.Bottom(SearchCasePosition) + 1;
            SearchSpellingNormalization.Y = Pos.Bottom(SearchCriteria) + 1;

            Button ExportAllResultsAsCsv = new Button() { Text = "Export Results..." };
            Button ExportAllAsCsv = new Button()
            {
                Text = "Export Everything...",
                Y = Pos.Bottom(ExportAllResultsAsCsv),
            };
            Button ExportShownAsCsv = new Button()
            {
                Text = "Export Displayed...",
                Y = Pos.Bottom(ExportAllAsCsv),
            };
            Button ReportSelectedAndSearch = new Button()
            {
                Text = ReportSelectedButtonText,
                Y = Pos.Bottom(ExportShownAsCsv) + 1,
            };

            View ExportOptions = new View()
            {
                Height = Dim.Auto(),
                Width = Dim.Auto(),
                Y = Pos.Bottom(SearchSpellingNormalization) + 1,
            };
            ExportOptions.Add(
                ExportAllResultsAsCsv,
                ExportAllAsCsv,
                ExportShownAsCsv,
                ReportSelectedAndSearch
            );
            SearchOptions.Add(ExportOptions);

            Add(
                new Label()
                {
                    Text = "Hello, world!",
                    Width = Dim.Auto(),
                    Height = Dim.Auto(),
                }
            );
            Add(LabelResults, SearchBox, Results);
            Add(SearchOptions);
            Width = Dim.Fill();
            Height = Dim.Fill();

            KeyDown += (e, a) =>
            {
                switch (a.KeyCode)
                {
                    case KeyCode.F1:
                        RBLSearchCaseSensitivity.SelectedItem = 0;
                        break;
                    case KeyCode.F2:
                        RBLSearchCaseSensitivity.SelectedItem = 1;
                        break;
                    case KeyCode.F3:
                        RBLSearchPosition.SelectedItem = 0;
                        break;
                    case KeyCode.F4:
                        RBLSearchPosition.SelectedItem = 1;
                        break;
                    case KeyCode.F5:
                        if (RBLSearchCriteria.RadioLabels.Count() >= 1)
                            RBLSearchCriteria.SelectedItem = 0;
                        break;
                    case KeyCode.F6:
                        if (RBLSearchCriteria.RadioLabels.Count() >= 2)
                            RBLSearchCriteria.SelectedItem = 1;
                        break;
                    case KeyCode.F7:
                        if (RBLSearchCriteria.RadioLabels.Count() >= 3)
                            RBLSearchCriteria.SelectedItem = 2;
                        break;
                    case KeyCode.F8:
                        if (RBLSearchCriteria.RadioLabels.Count() >= 4)
                            RBLSearchCriteria.SelectedItem = 3;
                        break;
                    case KeyCode.F9:
                        if (RBLSearchCriteria.RadioLabels.Count() >= 5)
                            RBLSearchCriteria.SelectedItem = 4;
                        break;
                    case KeyCode.F10:
                        if (RBLSearchCriteria.RadioLabels.Count() >= 6)
                            RBLSearchCriteria.SelectedItem = 5;
                        break;
                    case KeyCode.F11:
                        if (RBLSearchCriteria.RadioLabels.Count() >= 7)
                            RBLSearchCriteria.SelectedItem = 6;
                        break;
                    case KeyCode.End:
                        CBNormalizeSpelling.CheckedState =
                            CBNormalizeSpelling.CheckedState == CheckState.UnChecked
                                ? CheckState.Checked
                                : CheckState.UnChecked;
                        break;
                    case KeyCode.Insert:
                        CBAnythingAnywhere.CheckedState =
                            CBAnythingAnywhere.CheckedState == CheckState.UnChecked
                                ? CheckState.Checked
                                : CheckState.UnChecked;
                        break;
                    case KeyCode.Enter:
                        if (!searching)
                        {
                            if (Results.SelectedRow != null)
                            {
                                this._OutputList = Filtered.Select(a => a.Item1).ToList();
                                this._Selected = (string[])(
                                    (ResultsDT.Rows[Results.SelectedRow])[0]
                                );
                                //this.Close();
                            }
                            else if (ResultsDT.Rows != null && ResultsDT.Rows.Count != 0)
                            {
                                this._OutputList = Filtered.Select(a => a.Item1).ToList();
                                this._Selected = (string[])(ResultsDT.Rows[0])[0];
                                //this.Close();
                            }
                            else
                            {
                                MessageBox.Query(
                                    "Nothing displayed, nothing selected; [Esc] to exit the search dialog",
                                    "Error",
                                    "Ok"
                                );
                            }
                        }

                        break;
                    //case Keys.Escape:
                    //this.Close();
                    //break;
                    default:
                        break;
                }
                ;
            };
        }
    }
}
