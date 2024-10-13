using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Eto.Drawing;
using RV.InvNew.Common;
using SharpDX.Direct2D1;
using System.Text.RegularExpressions;

namespace EtoFE
{
    public static class Extensions
    {
        public static IEnumerable<string> FilterWithOptions<T>(ref T Input, string s, bool CaseInsensitive = true, bool Contains = false) where T : IEnumerable<string>
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
            else return A.Where(x => x.StartsWith(s));
        }
        public static string NormalizeSpelling(this string s) {
            var o = Regex.Replace(s, "[aeiouh]", ""); //English has 10+ vowels, mgiht as well remove all of them.
            o = o.Replace("k", "c").Replace("i", "y"); //K is the C equivalent from Greek, Y is the I equivalent from Greek.
            //Hope these are the last totally redundant letters...
            return o;
        }
        public static bool FilterAccordingly(this string str, string s, bool CaseInsensitive = true, bool Contains = true, bool normalizeSpelling = true, bool AnythingAnywhere = false)
        {
            string x = str;
            string cs = s;
            if (CaseInsensitive == true)
            {
                x = x.ToLowerInvariant();
                cs = cs.ToLowerInvariant();
            }
            if (normalizeSpelling == true) {
                x = x.NormalizeSpelling();
                cs = cs.NormalizeSpelling();
            }
            if(AnythingAnywhere == true)
            {
                return cs.Split(" ").All(seg => x.Contains(seg));
            }
            if (Contains == true)
            {
                return x.Contains(cs);
            }
            else return x.StartsWith(cs);
        }
    }
    public class SearchDialog: Dialog
    {
        
        public SearchDialog(List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SC, List<(string, TextAlignment, bool)> HeaderEntries)
        {
            TableLayout TL = new TableLayout();
            Label SL = new Label() { Text = "Search for: " };
            Label LabelResults = new Label() { Text = "Results: " };
            GridView Results = new GridView();
            RadioButtonList RBLSearchCriteria = new RadioButtonList() { Orientation = Eto.Forms.Orientation.Vertical, Padding = 5};
            RadioButtonList RBLSearchCaseSensitivity = new RadioButtonList() {Orientation = Eto.Forms.Orientation.Vertical, Padding = 5 };
            RadioButtonList RBLSearchPosition = new RadioButtonList() { Orientation = Eto.Forms.Orientation.Vertical , Padding = 5 };
            RBLSearchCaseSensitivity.Items.Add("Case-insensitive [F1]");
            RBLSearchCaseSensitivity.Items.Add("Case-sensitive [F2]");
            RBLSearchPosition.Items.Add("Contains [F3]");
            RBLSearchPosition.Items.Add("StartsWith [F4]");
            bool SearchCaseSensitive = false;
            bool SearchContains = true;
            CheckBox CBNormalizeSpelling = new CheckBox() { Text = "Normalize spelling [END]" };
            CheckBox CBAnythingAnywhere = new CheckBox() { Text = "Anything Anywhere [BRK]" };
            bool NormalizeSpelling = false;
            bool AnythingAnywhere = false;

            CBNormalizeSpelling.CheckedChanged += (e, a) => {
                NormalizeSpelling = CBNormalizeSpelling.Checked ?? false;
            };
            CBAnythingAnywhere.CheckedChanged += (e, a) => {
                AnythingAnywhere = CBAnythingAnywhere.Checked ?? false;
            };


            GroupBox SearchCriteria = new() { Text = "Search in...", Content = RBLSearchCriteria };
            GroupBox SearchCaseSensitivity = new() { Text = "Case sensitivity setting", Content = RBLSearchCaseSensitivity };
            GroupBox SearchCasePosition = new() { Text = "Search Position", Content = RBLSearchPosition };
            GroupBox SearchSpellingNormalization = new() { Text = "Advanced...", Content = new StackLayout( CBNormalizeSpelling, CBAnythingAnywhere){ Orientation = Eto.Forms.Orientation.Vertical } };

            StackLayout SearchOptions = new StackLayout()
            {
                Items = {
                    SearchCaseSensitivity,
                    SearchCasePosition,
                    SearchCriteria,
                    SearchSpellingNormalization
                },
                Orientation = Eto.Forms.Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = 5,
            };

            int SelectedSearchIndex = SC[0].Item1.Length;
            RBLSearchCriteria.SelectedIndexChanged += (e, a) => {
                SelectedSearchIndex = RBLSearchCriteria.SelectedIndex;
            };
            RBLSearchCaseSensitivity.SelectedIndexChanged += (e, a) => {
                SearchCaseSensitive = RBLSearchCaseSensitivity.SelectedIndex == 1;
            };
            RBLSearchPosition.SelectedIndexChanged += (e, a) => {
                SearchContains = RBLSearchPosition.SelectedIndex == 0;
            };
            TextAlignment[] Alignments = new TextAlignment[HeaderEntries.Count];
            Alignments = HeaderEntries.Select((x) => x.Item2).ToArray();
            int ic = 0;
            int fnKey = 0;
            int SortBy = 0;
            foreach (var Header in HeaderEntries)
            {
                var HI = new GridColumn { HeaderText = Header.Item1, DataCell = new TextBoxCell(ic) { TextAlignment = Header.Item2 }, HeaderTextAlignment = Header.Item2, Sortable = true, MinWidth = 40 };
                
                Results.Columns.Add(HI);
                
                ic++;
                fnKey = 4 + ic;
                RBLSearchCriteria.Items.Add(Header.Item1 + $" [F{fnKey}]");
            }
            
            Results.Enabled = true;
            Results.BackgroundColor = Eto.Drawing.Colors.Wheat;
            Results.Size = new Size(600, 600);
            (Eto.Drawing.Color?, Eto.Drawing.Color?)[] ColorMat = Array.Empty<(Eto.Drawing.Color?, Eto.Drawing.Color?)>();
            Results.CellFormatting += (e, a) => {
                if (a.Row % 2 == 0)
                {
                    a.BackgroundColor = Eto.Drawing.Colors.Turquoise;
                    a.ForegroundColor = Eto.Drawing.Colors.Black;
                }
                if (ColorMat != null && Results.DataStore != null && Results.DataStore.Count() <= ColorMat.Length) {
                    if (ColorMat[a.Row].Item1 != null) a.BackgroundColor = (Eto.Drawing.Color)ColorMat[a.Row].Item1!;
                    if (ColorMat[a.Row].Item2 != null) a.ForegroundColor = (Eto.Drawing.Color)ColorMat[a.Row].Item2!;
                };
                
            };
            this.KeyDown += (e, a) => {
                switch (a.Key) {
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
                        if(RBLSearchCriteria.Items.Count >= 1)
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
                    default:
                        break;
                }
            };
            RBLSearchCriteria.Items.Add($"Omnibox [F{fnKey+1}]");
            TextBox SearchBox = new TextBox();
            MessageBox.Show($"PC: {SC.Count()}");
            bool searching = false;
            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> Filtered = new List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>();
            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> FilteredTemp = new List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>();
            
            MessageBox.Show($"Last: {SC.Last().Item1[0]} Desc: {SC.Last().Item1[1]}");
            TL.Padding = 10;
            TL.Spacing = new Eto.Drawing.Size(10, 10);
            int FilteredCount = 0;
            Results.GridLines = GridLines.Both;
            var UpdateView = () => {
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
                this.Title = $"Found {FilteredCount} ";
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
                    //MessageBox.Show($"{SelectedArrayIndex}, {SC[0].Length}");
                    searching = true;
                    (new Thread(() =>
                    {
                        if (SelectedArrayIndex > SC[0].Item1.Length - 1)
                        {
                            var FilteredBeforeCounting = SC.AsParallel().Where((x) => x.Item1.Any((e) => e.FilterAccordingly(searchString, !SearchCaseSensitiveSetting, SearchContainsSetting, SearchNormalizeSpelling, SearchAnythingAnywhere))).AsSequential().OrderBy(x => x.Item1[SearchSortBy]);
                            FilteredTemp = FilteredBeforeCounting.Take(1000).ToList();
                            FilteredCount = FilteredBeforeCounting.Count();
                            searching = false;
                            Application.Instance.Invoke(UpdateView);
                            //MessageBox.Show(FilteredCount.ToString());
                        }
                        else
                        {
                            var FilteredBeforeCounting = SC.AsParallel().Where((x) => x.Item1[SelectedSearchIndex].FilterAccordingly(searchString, !SearchCaseSensitiveSetting, SearchContainsSetting, SearchNormalizeSpelling, SearchAnythingAnywhere)).AsSequential().OrderBy(x => x.Item1[SearchSortBy]);
                            FilteredTemp = FilteredBeforeCounting.Take(1000).ToList();
                            FilteredCount = FilteredBeforeCounting.Count();
                            searching = false;
                            Application.Instance.Invoke(UpdateView);
                        }
                    })).Start();
                    
                }
                
            };
            var ResultsContainer = new StackLayout() { Items = {Results}, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
            TL.Rows.Add(new TableRow(SearchBox));
            TL.Rows.Add(new TableRow(LabelResults));
            TL.Rows.Add(new TableRow(new TableCell(ResultsContainer) { ScaleWidth = true }, SearchOptions) { ScaleHeight = true });
            Content = TL;
            RBLSearchCriteria.SelectedIndex = RBLSearchCriteria.Items.Count - 1;
            RBLSearchCaseSensitivity.SelectedIndex = 0;
            RBLSearchPosition.SelectedIndex = 0;
            Title = "Search...";
            Resizable = true;
            Maximizable = true;
            Results.ColumnHeaderClick += (e, a) => {
                //MessageBox.Show(a.Column.DisplayIndex.ToString(), "Header was clicked!");
                SortBy = a.Column.DisplayIndex;
                Search();
            };
            SearchBox.KeyUp += (_, _) => Search();

        }
    }
}
