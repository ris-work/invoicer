using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Eto.Drawing;
using RV.InvNew.Common;
using SharpDX.Direct2D1;

namespace EtoFE
{
    public class SearchDialog: Dialog
    {
        public SearchDialog(List<string[]> SC, List<(string, TextAlignment)> HeaderEntries)
        {
            TableLayout TL = new TableLayout();
            Label SL = new Label() { Text = "Search for: " };
            Label LabelResults = new Label() { Text = "Results: " };
            GridView Results = new GridView();
            RadioButtonList RBLSearchCriteria = new RadioButtonList();
            RadioButtonList RBLSearchCaseSensitivity = new RadioButtonList();
            RBLSearchCaseSensitivity.Items.Add("Case-insensitive");
            RBLSearchCaseSensitivity.Items.Add("Case-sensitive");
            

            GroupBox SearchCriteria = new() { Text = "Search in...", Content = RBLSearchCriteria };
            GroupBox SearchCaseSensitivity = new() { Text = "Case sensitivity setting", Content = RBLSearchCaseSensitivity };

            StackLayout SearchOptions = new StackLayout()
            {
                Items = {
                    SearchCriteria,
                    SearchCaseSensitivity
                },
                Orientation = Eto.Forms.Orientation.Vertical
            };

            int SelectedSearchIndex = 0;
            RBLSearchCriteria.SelectedIndexChanged += (e, a) => {
                SelectedSearchIndex = RBLSearchCriteria.SelectedIndex;
            };
            foreach (var Header in HeaderEntries)
            {
                Results.Columns.Add(new GridColumn { HeaderText = Header.Item1, DataCell = new TextBoxCell(0), HeaderTextAlignment = Header.Item2 });
                RBLSearchCriteria.Items.Add(Header.Item1);
            }
            Results.Enabled = true;
            Results.BackgroundColor = Eto.Drawing.Colors.Wheat;
            Results.Size = new Size(500, 500);
            Results.CellFormatting += (e, a) => {
                if (a.Row % 2 == 0)
                {
                    a.BackgroundColor = Eto.Drawing.Colors.Turquoise;
                    a.ForegroundColor = Eto.Drawing.Colors.Black;
                }
            };
            this.KeyDown += (e, a) => {
                switch (a.Key) {
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
                    default:
                        break;
                }
            };
            SearchOptions.Items.Add("Omnibox");
            TextBox SearchBox = new TextBox();
            MessageBox.Show($"PC: {SC.Count()}");
            bool searching = false;
            List<string[]> Filtered = new List<string[]>();
            List<string[]> FilteredTemp = new List<string[]>();
            MessageBox.Show($"Last: {SC.Last()[0]} Desc: {SC.Last()[1]}");
            TL.Padding = 10;
            TL.Spacing = new Eto.Drawing.Size(10, 10);
            int FilteredCount = 0;
            Results.GridLines = GridLines.Both;
            var UpdateView = () => {
                Filtered = FilteredTemp;
                List<GridItem> GI = new List<GridItem>();
                Filtered = FilteredTemp;
                GI.Add(new GridItem("Hello", "World"));
                foreach (var item in Filtered)
                {
                    GI.Add(new GridItem(item));
                }
                //MessageBox.Show(GI.Count().ToString());
                Results.DataStore = GI;
                Results.Invalidate(true);
                this.Invalidate();
                this.Title = $"Found {FilteredCount} ";
            };
            SearchBox.KeyDown += (e, a) =>
            {
                if (SearchBox.Text.Length > 0 && searching != true)
                {
                    var SelectedArrayIndex = SelectedSearchIndex;
                    var searchString = SearchBox.Text.ToLowerInvariant();
                    searching = true;
                    (new Thread(() =>
                    {
                        if (SelectedSearchIndex > SC[0].Length - 1)
                        {
                            var FilteredBeforeCounting = SC.AsParallel().Where((x) => x.Any((e) => e.ToLowerInvariant().Contains(searchString))).AsSequential();
                            FilteredTemp = FilteredBeforeCounting.Take(1000).ToList();
                            FilteredCount = FilteredBeforeCounting.Count();
                            searching = false;
                            Application.Instance.Invoke(UpdateView);
                            //MessageBox.Show(FilteredCount.ToString());
                        }
                        else
                        {
                            var FilteredBeforeCounting = SC.AsParallel().Where((x) => x[SelectedSearchIndex].ToLowerInvariant().Contains(searchString)).AsSequential();
                            FilteredTemp = FilteredBeforeCounting.Take(1000).ToList();
                            FilteredCount = FilteredBeforeCounting.Count();
                            searching = false;
                            Application.Instance.Invoke(UpdateView);
                        }
                    })).Start();
                    
                }
                
            };
            TL.Rows.Add(new TableRow(SearchBox));
            TL.Rows.Add(new TableRow(LabelResults));
            TL.Rows.Add(new TableRow(Results));
            TL.Rows.Add(new TableRow(SearchOptions));
            Content = TL;
        }
    }
}
