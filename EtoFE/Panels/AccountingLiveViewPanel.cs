using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonUi;
using Eto.Forms;
using Eto.Drawing;
using RV.InvNew.Common;

namespace EtoFE.Panels
{
    public class AccountingLiveView : Panel
    {
        private readonly int refreshIntervalSeconds = 3; // Configurable refresh interval
        private Timer refreshTimer;
        private Control gridView;
        private StackLayout mainLayout;

        public AccountingLiveView()
        {
            InitializeUI();
            SetupRefreshTimer();

            // Initial data load
            RefreshData();
        }

        private void InitializeUI()
        {
            var LocalColor = ColorSettings.GetPanelSettings(
                "Editor",
                (IReadOnlyDictionary<string, object>)Program.ConfigDict
            );
            LocalColor = ColorSettings.RotateAllToPanelSettings(0);
            BackgroundColor = LocalColor?.BackgroundColor ?? ColorSettings.BackgroundColor;

            mainLayout = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Padding = 10,
                Spacing = 5
            };

            // Add a header label
            var headerLabel = new Label
            {
                Text = TranslationHelper.Translate("Live Journal Entries View"),
                Font = new Font(ColorSettings.UIFont ?? FontFamilies.Monospace, 12, FontStyle.Bold),
                TextColor = ColorSettings.ForegroundColor
            };

            // Add a status label
            var statusLabel = new Label
            {
                Text = TranslationHelper.Translate("Loading data..."),
                Font = new Font(ColorSettings.UIFont ?? FontFamilies.Monospace, 10),
                TextColor = ColorSettings.LesserForegroundColor
            };

            mainLayout.Items.Add(new StackLayoutItem(headerLabel, HorizontalAlignment.Left));
            mainLayout.Items.Add(new StackLayoutItem(statusLabel, HorizontalAlignment.Left));

            Content = mainLayout;
        }

        private void SetupRefreshTimer()
        {
            refreshTimer = new Timer(TimerCallback, null,
                TimeSpan.FromSeconds(refreshIntervalSeconds),
                TimeSpan.FromSeconds(refreshIntervalSeconds));
        }

        private void TimerCallback(object state)
        {
            // This runs on a background thread
            RefreshData();
        }

        private void RefreshData()
        {
            // Start data retrieval in a background thread
            Task.Run(() =>
            {
                try
                {
                    Log($"Fetching journal entries...");
                    var req = (
                        SendAuthenticatedRequest<string, List<AccountsJournalEntry>>.Send(
                            "Refresh",
                            "/GetNJournalEntries",
                            true
                        )
                    );

                    if (req.Error == false)
                    {
                        var JEs = req.Out;
                        Log($"Successfully retrieved {JEs.Count} journal entries");

                        // Update UI on the main thread
                        Application.Instance.Invoke(() =>
                        {
                            UpdateGrid(JEs);

                            // Update status
                            if (mainLayout.Items.Count > 1 && mainLayout.Items[1].Control is Label statusLabel)
                            {
                                statusLabel.Text = TranslationHelper.Translate($"Last updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                            }
                        });
                    }
                    else
                    {
                        Log($"Error retrieving journal entries");

                        // Update UI on the main thread
                        Application.Instance.Invoke(() =>
                        {
                            if (mainLayout.Items.Count > 1 && mainLayout.Items[1].Control is Label statusLabel)
                            {
                                statusLabel.Text = TranslationHelper.Translate("Error retrieving data");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log($"Exception in RefreshData: {ex.Message}");

                    // Update UI on the main thread
                    Application.Instance.Invoke(() =>
                    {
                        if (mainLayout.Items.Count > 1 && mainLayout.Items[1].Control is Label statusLabel)
                        {
                            statusLabel.Text = TranslationHelper.Translate($"Exception: {ex.Message}");
                        }
                    });
                }
            });
        }

        private void UpdateGrid(List<AccountsJournalEntry> journalEntries)
        {
            // Create or update the grid view with new data
            var newGrid = SearchPanelUtility.GenerateSearchPanel(
                journalEntries,
                false,
                null,
                [
                    "TimeAsEntered",
                    "DebitAccountName",
                    "CreditAccountName",
                    "Amount",
                    "JournalNo",
                    "PrincipalName",
                ]
            );

            // Replace old grid if it exists, or add it for the first time
            if (gridView != null)
            {
                // Remove old grid (assuming it's the third item in the stack layout)
                if (mainLayout.Items.Count > 2)
                {
                    mainLayout.Items.RemoveAt(2);
                }
            }

            // Add new grid
            gridView = newGrid;
            mainLayout.Items.Add(new StackLayoutItem(newGrid, HorizontalAlignment.Stretch, true));

            Log($"Journal entries grid updated with {journalEntries.Count} entries");
        }

        private void Log(string message)
        {
            Console.WriteLine($"[AccountingLiveView] {message}");
        }

        // Use Dispose method instead of OnDispose
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean up timer
                refreshTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}