//using Eto.Forms;
//using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
//using Eto.Forms;

namespace CommonUi
{
    public class SearchDialogTUI {
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
            TableView Results = new TableView();
            RadioGroup RBLSearchCriteria = new RadioGroup()
            {
                Orientation = Orientation.Vertical
            };
            RadioGroup RBLSearchCaseSensitivity = new RadioGroup()
            {
                Orientation = Orientation.Vertical
            };
            RadioGroup RBLSearchPosition = new RadioGroup()
            {
                Orientation = Orientation.Vertical
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
            CheckBox CBAnythingAnywhere = new CheckBox() { Text = "Anything Anywhere [BRK]" };
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

        }
        }
}
