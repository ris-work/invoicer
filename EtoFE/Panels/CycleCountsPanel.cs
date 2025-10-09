using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CommonUi;
using Eto.Forms;
using Eto.Drawing;
using RV.InvNew.Common;
using static CommonUi.ColorSettings;

namespace EtoFE.Panels
{
    public class CycleCountPanel : Panel
    {
        private TabControl tabControl;
        private TabPage addTab;
        private TabPage viewTab;
        private TabPage searchTab;
        private TabPage timeRangeTab;
        private TabPage discrepancyTab;

        // Add Cycle Count controls
        private TextBox txtItemcode;
        private Label lblItemcodeDisplay;
        private TextBox txtActualQty;
        private TextBox txtLocation;
        private TextBox txtNotes;
        private DateTimePicker dpCountDate;
        private Button btnSave;
        private Button btnReset;
        private Button btnNew;

        // View Cycle Count controls
        private TextBox txtViewItemcode;
        private Label lblViewItemcodeDisplay;
        private GridView gvCycleHistory;
        private Button btnViewHistory;

        // Search controls
        private TextBox txtSearchTerm;
        private GridView gvSearchResults;
        private Button btnSearch;

        // Time Range controls
        private DateTimePicker dpStartDate;
        private DateTimePicker dpEndDate;
        private GridView gvTimeRangeResults;
        private Button btnTimeRangeSearch;

        // Discrepancy controls
        private GridView gvDiscrepancyResults;
        private Button btnRefreshDiscrepancies;

        public CycleCountPanel()
        {
            InitializeComponents();
            SetupLayout();
            SetupEventHandlers();
            ApplyStyling();
        }

        private void InitializeComponents()
        {
            // Create tab control
            tabControl = new TabControl();

            // Initialize Add Cycle Count tab
            addTab = new TabPage { Text = TranslationHelper.Translate("Add Cycle Count") };
            txtItemcode = new TextBox { PlaceholderText = TranslationHelper.Translate("Item Code") };
            lblItemcodeDisplay = new Label();
            txtActualQty = new TextBox { PlaceholderText = TranslationHelper.Translate("Actual Quantity") };
            txtLocation = new TextBox { PlaceholderText = TranslationHelper.Translate("Location") };
            txtNotes = new TextBox { PlaceholderText = TranslationHelper.Translate("Notes") };
            dpCountDate = new DateTimePicker { Value = DateTime.Now };
            btnSave = new Button { Text = TranslationHelper.Translate("Save (F9-F12)") };
            btnReset = new Button { Text = TranslationHelper.Translate("Reset (F7-F8)") };
            btnNew = new Button { Text = TranslationHelper.Translate("New") };

            // Initialize View Cycle Count tab
            viewTab = new TabPage { Text = TranslationHelper.Translate("View Cycle Count") };
            txtViewItemcode = new TextBox { PlaceholderText = TranslationHelper.Translate("Item Code") };
            lblViewItemcodeDisplay = new Label();
            gvCycleHistory = new GridView();
            btnViewHistory = new Button { Text = TranslationHelper.Translate("View History (F1-F4)") };

            // Initialize Search tab
            searchTab = new TabPage { Text = TranslationHelper.Translate("Search Cycle Counts") };
            txtSearchTerm = new TextBox { PlaceholderText = TranslationHelper.Translate("Search Term") };
            gvSearchResults = new GridView();
            btnSearch = new Button { Text = TranslationHelper.Translate("Search (F1-F4)") };

            // Initialize Time Range tab
            timeRangeTab = new TabPage { Text = TranslationHelper.Translate("Cycle Counts by Date Range") };
            dpStartDate = new DateTimePicker { Value = DateTime.Now.AddDays(-180) };
            dpEndDate = new DateTimePicker { Value = DateTime.Now };
            gvTimeRangeResults = new GridView();
            btnTimeRangeSearch = new Button { Text = TranslationHelper.Translate("Search (F1-F4)") };

            // Initialize Discrepancy tab
            discrepancyTab = new TabPage { Text = TranslationHelper.Translate("Cycle Counts with Discrepancies") };
            gvDiscrepancyResults = new GridView();
            btnRefreshDiscrepancies = new Button { Text = TranslationHelper.Translate("Refresh (F1-F4)") };

            // Setup grid views
            SetupGridViews();
        }

        private void SetupGridViews()
        {
            // Cycle History Grid View
            gvCycleHistory.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, long>(r => r.Itemcode).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Item Code")
            });
            gvCycleHistory.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, long>(r => r.SeqNo).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Seq No")
            });
            gvCycleHistory.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, double>(r => r.RecordedQty).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Recorded Qty")
            });
            gvCycleHistory.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, double>(r => r.ActualQty).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Actual Qty")
            });
            gvCycleHistory.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, DateTime>(r => r.CountDate).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Count Date")
            });
            gvCycleHistory.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, string>(r => r.PrincipalName) },
                HeaderText = TranslationHelper.Translate("Counted By")
            });
            gvCycleHistory.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, string>(r => r.Location) },
                HeaderText = TranslationHelper.Translate("Location")
            });

            // Search Results Grid View
            gvSearchResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, long>(r => r.Itemcode).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Item Code")
            });
            gvSearchResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, long>(r => r.SeqNo).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Seq No")
            });
            gvSearchResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, double>(r => r.RecordedQty).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Recorded Qty")
            });
            gvSearchResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, double>(r => r.ActualQty).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Actual Qty")
            });
            gvSearchResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, DateTime>(r => r.CountDate).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Count Date")
            });
            gvSearchResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, string>(r => r.PrincipalName) },
                HeaderText = TranslationHelper.Translate("Counted By")
            });
            gvSearchResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, string>(r => r.Location) },
                HeaderText = TranslationHelper.Translate("Location")
            });

            // Time Range Results Grid View
            gvTimeRangeResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, long>(r => r.Itemcode).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Item Code")
            });
            gvTimeRangeResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, long>(r => r.SeqNo).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Seq No")
            });
            gvTimeRangeResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, double>(r => r.RecordedQty).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Recorded Qty")
            });
            gvTimeRangeResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, double>(r => r.ActualQty).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Actual Qty")
            });
            gvTimeRangeResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, DateTime>(r => r.CountDate).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Count Date")
            });
            gvTimeRangeResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, string>(r => r.PrincipalName) },
                HeaderText = TranslationHelper.Translate("Counted By")
            });
            gvTimeRangeResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, string>(r => r.Location) },
                HeaderText = TranslationHelper.Translate("Location")
            });

            // Discrepancy Results Grid View
            gvDiscrepancyResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, long>(r => r.Itemcode).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Item Code")
            });
            gvDiscrepancyResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, long>(r => r.SeqNo).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Seq No")
            });
            gvDiscrepancyResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, double>(r => r.RecordedQty).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Recorded Qty")
            });
            gvDiscrepancyResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, double>(r => r.ActualQty).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Actual Qty")
            });
            gvDiscrepancyResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, double>(r => Math.Abs(r.ActualQty - r.RecordedQty)).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Difference")
            });
            gvDiscrepancyResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, DateTime>(r => r.CountDate).Convert(r => r.ToString()) },
                HeaderText = TranslationHelper.Translate("Count Date")
            });
            gvDiscrepancyResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, string>(r => r.PrincipalName) },
                HeaderText = TranslationHelper.Translate("Counted By")
            });
            gvDiscrepancyResults.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<CycleCount, string>(r => r.Location) },
                HeaderText = TranslationHelper.Translate("Location")
            });
        }

        private void SetupLayout()
        {
            // Add Cycle Count tab layout
            var addLayout = new DynamicTableLayout(2);
            addLayout.AddRow(TranslationHelper.Translate("Item Code (F1-F4)"), txtItemcode, lblItemcodeDisplay);
            addLayout.AddRow(TranslationHelper.Translate("Actual Quantity"), txtActualQty);
            addLayout.AddRow(TranslationHelper.Translate("Location"), txtLocation);
            addLayout.AddRow(TranslationHelper.Translate("Notes"), txtNotes);
            addLayout.AddRow(TranslationHelper.Translate("Count Date"), dpCountDate);

            var buttonLayout = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Items =
                {
                    new StackLayoutItem(btnNew),
                    new StackLayoutItem(btnSave),
                    new StackLayoutItem(btnReset)
                },
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            addTab.Content = new StackLayout
            {
                Items =
                {
                    addLayout,
                    buttonLayout
                }
            };

            // View Cycle Count tab layout
            var viewLayout = new DynamicTableLayout(2);
            viewLayout.AddRow(TranslationHelper.Translate("Item Code (F1-F4)"), txtViewItemcode, lblViewItemcodeDisplay);

            var viewButtonLayout = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Items = { new StackLayoutItem(btnViewHistory) },
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            viewTab.Content = new StackLayout
            {
                Items =
                {
                    viewLayout,
                    viewButtonLayout,
                    gvCycleHistory
                }
            };

            // Search tab layout
            var searchLayout = new DynamicTableLayout(2);
            searchLayout.AddRow(TranslationHelper.Translate("Search Term"), txtSearchTerm);

            var searchButtonLayout = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Items = { new StackLayoutItem(btnSearch) },
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            searchTab.Content = new StackLayout
            {
                Items =
                {
                    searchLayout,
                    searchButtonLayout,
                    gvSearchResults
                }
            };

            // Time Range tab layout
            var timeRangeLayout = new DynamicTableLayout(2);
            timeRangeLayout.AddRow(TranslationHelper.Translate("Start Date"), dpStartDate);
            timeRangeLayout.AddRow(TranslationHelper.Translate("End Date"), dpEndDate);

            var timeRangeButtonLayout = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Items = { new StackLayoutItem(btnTimeRangeSearch) },
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            timeRangeTab.Content = new StackLayout
            {
                Items =
                {
                    timeRangeLayout,
                    timeRangeButtonLayout,
                    gvTimeRangeResults
                }
            };

            // Discrepancy tab layout
            var discrepancyButtonLayout = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Items = { new StackLayoutItem(btnRefreshDiscrepancies) },
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            discrepancyTab.Content = new StackLayout
            {
                Items =
                {
                    discrepancyButtonLayout,
                    gvDiscrepancyResults
                }
            };

            // Add all tabs to the tab control
            tabControl.Pages.Add(addTab);
            tabControl.Pages.Add(viewTab);
            tabControl.Pages.Add(searchTab);
            tabControl.Pages.Add(timeRangeTab);
            tabControl.Pages.Add(discrepancyTab);

            Content = tabControl;
        }

        private void SetupEventHandlers()
        {
            // Add Cycle Count event handlers
            btnSave.Click += (_, _) => SaveCycleCount();
            btnReset.Click += (_, _) => ResetAddForm();
            btnNew.Click += (_, _) =>
            {
                var result = MessageBox.Show(
                    TranslationHelper.Translate("Create new cycle count? Unsaved changes will be lost."),
                    TranslationHelper.Translate("Confirm"), MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    ResetAddForm();
                }
            };

            // View Cycle Count event handlers
            btnViewHistory.Click += (_, _) => ViewCycleHistory();

            // Search event handlers
            btnSearch.Click += (_, _) => SearchCycleCounts();

            // Time Range event handlers
            btnTimeRangeSearch.Click += (_, _) => SearchByTimeRange();

            // Discrepancy event handlers
            btnRefreshDiscrepancies.Click += (_, _) => RefreshDiscrepancies();

            // Keyboard shortcuts for Itemcode search
            txtItemcode.KeyUp += (sender, e) => HandleItemcodeSearch(sender, e, txtItemcode, lblItemcodeDisplay);
            txtViewItemcode.KeyUp += (sender, e) => HandleItemcodeSearch(sender, e, txtViewItemcode, lblViewItemcodeDisplay);

            // Global keyboard shortcuts
            KeyUp += (sender, e) =>
            {
                switch (e.Key)
                {
                    case Keys.F1:
                    case Keys.F2:
                    case Keys.F3:
                    case Keys.F4:
                        if (tabControl.SelectedPage == addTab && IsFocused(txtItemcode))
                            HandleItemcodeSearch(txtItemcode, null, txtItemcode, lblItemcodeDisplay);
                        else if (tabControl.SelectedPage == viewTab && IsFocused(txtViewItemcode))
                            HandleItemcodeSearch(txtViewItemcode, null, txtViewItemcode, lblViewItemcodeDisplay);
                        else if (tabControl.SelectedPage == searchTab)
                            SearchCycleCounts();
                        else if (tabControl.SelectedPage == timeRangeTab)
                            SearchByTimeRange();
                        else if (tabControl.SelectedPage == discrepancyTab)
                            RefreshDiscrepancies();
                        break;
                    case Keys.F5:
                    case Keys.F6:
                        // Edit functionality (to be implemented)
                        break;
                    case Keys.F7:
                    case Keys.F8:
                        if (tabControl.SelectedPage == addTab)
                            ResetAddForm();
                        break;
                    case Keys.F9:
                    case Keys.F10:
                    case Keys.F11:
                    case Keys.F12:
                        if (tabControl.SelectedPage == addTab)
                            SaveCycleCount();
                        break;
                }
            };
        }

        private void ApplyStyling()
        {
            BackgroundColor = ColorSettings.BackgroundColor;

            // Apply styling to all controls
            var allControls = new List<Control>
            {
                txtItemcode, lblItemcodeDisplay, txtActualQty, txtLocation, txtNotes, dpCountDate,
                txtViewItemcode, lblViewItemcodeDisplay, gvCycleHistory, gvSearchResults, gvTimeRangeResults, gvDiscrepancyResults,
                txtSearchTerm, dpStartDate, dpEndDate
            };

            foreach (var control in allControls)
            {
                if (control is TextBox textBox)
                {
                    textBox.BackgroundColor = ColorSettings.LesserBackgroundColor;
                    textBox.TextColor = ColorSettings.LesserForegroundColor;
                }
                else if (control is Label label)
                {
                    label.TextColor = ColorSettings.ForegroundColor;
                }
                else if (control is GridView gridView)
                {
                    gridView.BackgroundColor = ColorSettings.LesserBackgroundColor;
                    gridView.GridLines = GridLines.Horizontal;
                }
                else if (control is DateTimePicker dateTimePicker)
                {
                    dateTimePicker.BackgroundColor = ColorSettings.LesserBackgroundColor;
                    dateTimePicker.TextColor = ColorSettings.LesserForegroundColor;
                }
            }

            // Style buttons
            var buttons = new List<Button> { btnSave, btnReset, btnNew, btnViewHistory, btnSearch, btnTimeRangeSearch, btnRefreshDiscrepancies };
            foreach (var button in buttons)
            {
                button.BackgroundColor = ColorSettings.BackgroundColor;
                button.TextColor = ColorSettings.ForegroundColor;
            }
        }

        private bool IsFocused(Control control)
        {
            return control.HasFocus;
        }

        private void HandleItemcodeSearch(object sender, KeyEventArgs e, TextBox textBox, Label displayLabel)
        {
            if (e != null && (e.Key != Keys.F1 && e.Key != Keys.F2 && e.Key != Keys.F3 && e.Key != Keys.F4))
                return;

            if (string.IsNullOrEmpty(textBox.Text))
            {
                // Search for inventory items
                var sel = SearchPanelUtility.GenerateSearchDialog(
                    new List<object>(), // Replace with actual inventory items
                    this,
                    debug: false
                );

                if (sel?.Length > 0 && long.TryParse(sel[0], out var itemcode))
                {
                    textBox.Text = itemcode.ToString();
                    displayLabel.Text = LookupHumanFriendlyItemcode(itemcode);
                }
            }
        }

        private string LookupHumanFriendlyItemcode(long itemcode)
        {
            // Implement lookup logic
            return $"Item {itemcode}"; // Placeholder
        }

        private (bool IsValid, string ConsolidatedErrorList) ValidateInputs()
        {
            var errors = new List<string>();

            if (tabControl.SelectedPage == addTab)
            {
                if (string.IsNullOrEmpty(txtItemcode.Text) || !long.TryParse(txtItemcode.Text, out _))
                    errors.Add(TranslationHelper.Translate("Item Code must be a valid number"));

                if (string.IsNullOrEmpty(txtActualQty.Text) || !double.TryParse(txtActualQty.Text, out _))
                    errors.Add(TranslationHelper.Translate("Actual Quantity must be a valid number"));
            }

            return (errors.Count == 0, string.Join("\n", errors));
        }

        private void SaveCycleCount()
        {
            var (isValid, errorList) = ValidateInputs();
            if (!isValid)
            {
                MessageBox.Show(errorList, TranslationHelper.Translate("Validation Error"), MessageBoxButtons.OK);
                return;
            }

            try
            {
                var cycleCount = new CycleCount
                {
                    Itemcode = long.Parse(txtItemcode.Text),
                    ActualQty = double.Parse(txtActualQty.Text),
                    Location = txtLocation.Text,
                    Notes = txtNotes.Text,
                    CountDate = dpCountDate?.Value ?? DateTime.Now
                };

                var json = JsonSerializer.Serialize(cycleCount);
                Log($"Saving cycle count: {json}");

                MessageBox.Show(json, TranslationHelper.Translate("Cycle Count Data"), MessageBoxButtons.OK);

                // Send to backend (actual implementation would go here)
                var req = (
                    SendAuthenticatedRequest<CycleCount, long>.Send(
                        cycleCount,
                        "/AddCycleCount",
                        true
                    )
                );

                if (!req.Error)
                {
                    var resp = req.Out;
                    Log($"Cycle count saved with ID: {resp}");
                    ResetAddForm();
                }
                else
                {
                    Log($"Error saving cycle count");
                    MessageBox.Show(
                        TranslationHelper.Translate("Error saving cycle count"),
                        TranslationHelper.Translate("Error"),
                        MessageBoxButtons.OK
                    );
                }
            }
            catch (Exception ex)
            {
                Log($"Exception saving cycle count: {ex.Message}");
                MessageBox.Show(ex.Message, TranslationHelper.Translate("Error"), MessageBoxButtons.OK);
            }
        }

        private void ResetAddForm()
        {
            txtItemcode.Text = "";
            lblItemcodeDisplay.Text = "";
            txtActualQty.Text = "";
            txtLocation.Text = "";
            txtNotes.Text = "";
            dpCountDate.Value = DateTime.Now;
        }

        private void ViewCycleHistory()
        {
            if (string.IsNullOrEmpty(txtViewItemcode.Text) || !long.TryParse(txtViewItemcode.Text, out var itemcode))
            {
                MessageBox.Show(TranslationHelper.Translate("Please enter a valid Item Code"), TranslationHelper.Translate("Error"), MessageBoxButtons.OK);
                return;
            }

            try
            {
                var req = (
                    SendAuthenticatedRequest<long, List<CycleCount>>.Send(
                        itemcode,
                        "/GetCycleCountHistory",
                        true
                    )
                );

                if (!req.Error)
                {
                    gvCycleHistory.DataStore = req.Out;
                    Log($"Retrieved {req.Out.Count} cycle count records for item {itemcode}");
                }
                else
                {
                    Log($"Error retrieving cycle count history");
                    MessageBox.Show(
                        TranslationHelper.Translate("Error retrieving cycle count history"),
                        TranslationHelper.Translate("Error"),
                        MessageBoxButtons.OK
                    );
                }
            }
            catch (Exception ex)
            {
                Log($"Exception retrieving cycle count history: {ex.Message}");
                MessageBox.Show(ex.Message, TranslationHelper.Translate("Error"), MessageBoxButtons.OK);
            }
        }

        private void SearchCycleCounts()
        {
            if (string.IsNullOrEmpty(txtSearchTerm.Text))
            {
                MessageBox.Show(TranslationHelper.Translate("Please enter a search term"), TranslationHelper.Translate("Error"), MessageBoxButtons.OK);
                return;
            }

            try
            {
                var req = (
                    SendAuthenticatedRequest<string, List<CycleCount>>.Send(
                        txtSearchTerm.Text,
                        "/SearchCycleCounts",
                        true
                    )
                );

                if (!req.Error)
                {
                    gvSearchResults.DataStore = req.Out;
                    Log($"Found {req.Out.Count} cycle count records matching '{txtSearchTerm.Text}'");
                }
                else
                {
                    Log($"Error searching cycle counts");
                    MessageBox.Show(
                        TranslationHelper.Translate("Error searching cycle counts"),
                        TranslationHelper.Translate("Error"),
                        MessageBoxButtons.OK
                    );
                }
            }
            catch (Exception ex)
            {
                Log($"Exception searching cycle counts: {ex.Message}");
                MessageBox.Show(ex.Message, TranslationHelper.Translate("Error"), MessageBoxButtons.OK);
            }
        }

        private void SearchByTimeRange()
        {
            try
            {
                var timePeriod = new TimePeriod
                {
                    From = dpStartDate.Value,
                    To = dpEndDate.Value
                };

                var req = (
                    SendAuthenticatedRequest<TimePeriod, List<CycleCount>>.Send(
                        timePeriod,
                        "/GetCycleCountsWithinTimePeriod",
                        true
                    )
                );

                if (!req.Error)
                {
                    gvTimeRangeResults.DataStore = req.Out;
                    Log($"Found {req.Out.Count} cycle count records between {dpStartDate.Value} and {dpEndDate.Value}");
                }
                else
                {
                    Log($"Error searching cycle counts by time range");
                    MessageBox.Show(
                        TranslationHelper.Translate("Error searching cycle counts by time range"),
                        TranslationHelper.Translate("Error"),
                        MessageBoxButtons.OK
                    );
                }
            }
            catch (Exception ex)
            {
                Log($"Exception searching cycle counts by time range: {ex.Message}");
                MessageBox.Show(ex.Message, TranslationHelper.Translate("Error"), MessageBoxButtons.OK);
            }
        }

        private void RefreshDiscrepancies()
        {
            try
            {
                var req = (
                    SendAuthenticatedRequest<string, List<CycleCount>>.Send(
                        "",
                        "/GetCycleCountsWithDiscrepancies",
                        true
                    )
                );

                if (!req.Error)
                {
                    gvDiscrepancyResults.DataStore = req.Out;
                    Log($"Found {req.Out.Count} cycle count records with discrepancies");
                }
                else
                {
                    Log($"Error retrieving cycle counts with discrepancies");
                    MessageBox.Show(
                        TranslationHelper.Translate("Error retrieving cycle counts with discrepancies"),
                        TranslationHelper.Translate("Error"),
                        MessageBoxButtons.OK
                    );
                }
            }
            catch (Exception ex)
            {
                Log($"Exception retrieving cycle counts with discrepancies: {ex.Message}");
                MessageBox.Show(ex.Message, TranslationHelper.Translate("Error"), MessageBoxButtons.OK);
            }
        }

        private void Log(string message)
        {
            Console.WriteLine($"[CycleCountPanel] {DateTime.Now}: {message}");
        }
    }

    // Helper class for dynamic table layout
    public class DynamicTableLayout : TableLayout
    {
        private int columnCount;
        private int currentRow = 0;
        private List<Control> currentRowControls = new List<Control>();

        public DynamicTableLayout(int columnCount)
        {
            this.columnCount = columnCount;
            Padding = 5;
            Spacing = new Eto.Drawing.Size(5, 5);
        }

        public void AddRow(params Control[] controls)
        {
            var rowControls = new List<TableCell>();

            foreach (var control in controls)
            {
                rowControls.Add(new TableCell(control));

                if (rowControls.Count >= columnCount)
                {
                    Rows.Add(new TableRow(rowControls.ToArray()));
                    rowControls.Clear();
                }
            }

            // Add any remaining controls in the row
            if (rowControls.Count > 0)
            {
                // Fill the rest of the row with empty cells if needed
                while (rowControls.Count < columnCount)
                {
                    rowControls.Add(new TableCell(new Panel()));
                }

                Rows.Add(new TableRow(rowControls.ToArray()));
            }
        }

        public void AddRow(string labelText, Control control, Control additionalControl = null)
        {
            var label = new Label { Text = labelText, VerticalAlignment = VerticalAlignment.Center };
            label.Width = ColorSettings.InnerLabelWidth ?? 150;

            if (additionalControl != null)
            {
                control.Width = (ColorSettings.InnerControlWidth ?? 200) - 50;
                additionalControl.Width = 50;
                AddRow(label, control, additionalControl);
            }
            else
            {
                control.Width = ColorSettings.InnerControlWidth ?? 200;
                AddRow(label, control);
            }
        }
    }


}