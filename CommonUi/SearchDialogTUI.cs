//using Eto.Forms;
//using Eto.Forms;
//using Eto.Forms;
//using Eto.Forms;
//using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Microsoft.EntityFrameworkCore;
using System.Data;
using EtoFE;
using System.Drawing.Imaging;
//using Eto.Forms;

namespace CommonUi
{
    public class SearchDialogTUI: Terminal.Gui.Toplevel {
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
            TextField SearchBox = new TextField() { Width = 30, Y = Pos.Bottom(LabelResults) };
            TableView Results = new TableView() { FullRowSelect = true, Width = Dim.Fill(), Height = Dim.Fill(), Y = Pos.Bottom(SearchBox)+1  };
            RadioGroup RBLSearchCriteria = new RadioGroup()
            {
                Orientation = Orientation.Vertical,
                Width = Dim.Auto(),
                Height = Dim.Auto()
            };
            RadioGroup RBLSearchCaseSensitivity = new RadioGroup()
            {
                Orientation = Orientation.Vertical,
                Width = Dim.Auto(),
                Height = Dim.Auto()
            };
            RadioGroup RBLSearchPosition = new RadioGroup()
            {
                Orientation = Orientation.Vertical,
                Width = Dim.Auto(),
                Height = Dim.Auto()
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
            CheckBox CBAnythingAnywhere = new CheckBox() { Text = "Anything Anywhere [BRK]", Y = Pos.Bottom(CBNormalizeSpelling) };
            bool NormalizeSpelling = false;
            bool AnythingAnywhere = false;
            bool ReverseSort = false;

            CBNormalizeSpelling.CheckedStateChanging += (e, a) =>
            {
                NormalizeSpelling = CBNormalizeSpelling.CheckedState == CheckState.Checked ? false : true;
            };
            CBAnythingAnywhere.CheckedStateChanging += (e, a) =>
            {
                AnythingAnywhere = CBAnythingAnywhere.CheckedState == CheckState.Checked ? false : true;
            };

            FrameView SearchCriteria = new() { Text = "Search in..."};

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
                ResultsDT.Columns.Add(new DataColumn() {  ColumnName = Header.Item1});

                ic++;
                fnKey = 4 + ic;
                RBLSearchCriteriaList.Add(Header.Item1 + $" [F{fnKey}]");
            }
            RBLSearchCriteriaList.Add($"Omnibox [F{fnKey + 1}]");
            Results.Table = new DataTableSource(ResultsDT);
            RBLSearchCriteria.RadioLabels = RBLSearchCriteriaList.ToArray();

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

            FrameView SearchCaseSensitivity = new()
            {
                Text = "Case sensitivity setting",
            };
            SearchCaseSensitivity.Add(RBLSearchCaseSensitivity);
            SearchCaseSensitivity.Width = Dim.Auto();
            FrameView SearchCasePosition = new()
            {
                Text = "Search Position",
            };
            SearchCasePosition.Add(RBLSearchPosition);
            SearchCasePosition.Width = Dim.Auto();
            FrameView SearchSpellingNormalization = new()
            {
                Text = "Advanced...",

            };
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
                    ResultsDTU.Rows.Add(item.Item1);
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
            };
            SearchCasePosition.Y = Pos.Bottom(SearchCaseSensitivity)+1;
            SearchCriteria.Y = Pos.Bottom(SearchCasePosition)+1;
            SearchSpellingNormalization.Y = Pos.Bottom(SearchCriteria)+1;
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
                    SearchSpellingNormalization);

            SearchOptions.Y = 0;
            SearchOptions.X = Pos.AnchorEnd();
            SearchOptions.Width = Dim.Auto();
            SearchOptions.Height = Dim.Auto();
            SearchCasePosition.Y = Pos.Bottom(SearchCaseSensitivity) + 1;
            SearchCriteria.Y = Pos.Bottom(SearchCasePosition) + 1;
            SearchSpellingNormalization.Y = Pos.Bottom(SearchCriteria) + 1;
            

            Button ExportAllResultsAsCsv = new Button() { Text = "Export Results..." };
            Button ExportAllAsCsv = new Button() { Text = "Export Everything...", Y = Pos.Bottom(ExportAllResultsAsCsv) };
            Button ExportShownAsCsv = new Button() { Text = "Export Displayed...", Y = Pos.Bottom(ExportAllAsCsv) };
            Button ReportSelectedAndSearch = new Button() { Text = ReportSelectedButtonText, Y = Pos.Bottom(ExportShownAsCsv)+1 };

            View ExportOptions = new View()
            {
                Height = Dim.Auto(),
                Width = Dim.Auto(),
                Y = Pos.Bottom(SearchSpellingNormalization)+1,
            };
            ExportOptions.Add(ExportAllResultsAsCsv, ExportAllAsCsv, ExportShownAsCsv, ReportSelectedAndSearch);
            SearchOptions.Add(ExportOptions);

            

            Add(new Label() { Text = "Hello, world!", Width = Dim.Auto(), Height = Dim.Auto() });
            Add(LabelResults, SearchBox, Results);
            Add(SearchOptions);
            Width = Dim.Fill();
            Height = Dim.Fill();

        }
        }
}
