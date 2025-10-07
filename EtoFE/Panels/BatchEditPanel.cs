using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommonUi;
using Eto.Forms;
using RV.InvNew;
using RV.InvNew.Common;

namespace EtoFE
{
    public class BatchEditPanel : Panel
    {
        private GenEtoUI _batchForm;
        private SearchPanelEto _itemSearchBox;
        private SearchPanelEto _batchSearchBox;
        private Panel _formContainer;
        private List<PosBatch> _batchItems = new();
        private List<PosCatalogue> _catalogueItems = new();
        private Inventory _currentBatch;
        private Catalogue _currentItem;

        public BatchEditPanel()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            var LocalColor = ColorSettings.GetPanelSettings(
                "Editor",
                (IReadOnlyDictionary<string, object>)Program.ConfigDict
            );
            LocalColor = ColorSettings.RotateAllToPanelSettings(0);
            BackgroundColor = LocalColor?.BackgroundColor ?? ColorSettings.BackgroundColor;

            // Get data
            PosRefresh PR;
            while (true)
            {
                var req = (
                    SendAuthenticatedRequest<string, PosRefresh>.Send(
                        "Refresh",
                        "/PosRefreshBearerAuth",
                        true
                    )
                );
                if (req.Error == false)
                {
                    PR = req.Out;
                    break;
                }
            }

            // Store catalogue and batch items for later use
            _catalogueItems = PR.Catalogue;
            _batchItems = PR.Batches;

            // Setup item search panel
            List<(string, TextAlignment, bool)> ItemHeaderEntries = new()
            {
                (
                    TranslationHelper.Translate("Itemcode", "Itemcode", Program.lang),
                    TextAlignment.Right,
                    true
                ),
                (
                    TranslationHelper.Translate("Name", "Name", Program.lang),
                    TextAlignment.Left,
                    false
                ),
            };

            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SearchCatalogue = PR
                .Catalogue.Select<PosCatalogue, (string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>
                (e => (e.ToStringArray(), null, null))
                .ToList();

            _itemSearchBox = new SearchPanelEto(SearchCatalogue, ItemHeaderEntries, false, LocalColor);
            _itemSearchBox.OnSelectionMade = OnItemSelected;

            // Setup batch search panel
            List<(string, TextAlignment, bool)> BatchHeaderEntries = new()
            {
                (
                    TranslationHelper.Translate("Itemcode", "Itemcode", Program.lang),
                    TextAlignment.Right,
                    true
                ),
                (
                    TranslationHelper.Translate("Batchcode", "Batchcode", Program.lang),
                    TextAlignment.Right,
                    true
                ),
                (
                    TranslationHelper.Translate("Units", "Units", Program.lang),
                    TextAlignment.Right,
                    false
                ),
            };

            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SearchBatches = PR
                .Batches.Select<PosBatch, (string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>
                (e => (new string[] { e.itemcode.ToString(), e.batchcode.ToString(), e.SIH.ToString() }, null, null))
                .ToList();

            _batchSearchBox = new SearchPanelEto(SearchBatches, BatchHeaderEntries, false, LocalColor);
            _batchSearchBox.OnSelectionMade = OnBatchSelected;

            // Setup batch form with reactive updates
            var defaultBatch = new Inventory()
            {
                Itemcode = 0,
                Batchcode = 0,
                BatchEnabled = true,
                MfgDate = DateTime.Now,
                ExpDate = DateTime.Now.AddYears(1),
                PackedSize = 1,
                Units = 0,
                MeasurementUnit = "pcs",
                MarkedPrice = 0,
                SellingPrice = 0,
                CostPrice = 0,
                VolumeDiscounts = true,
                Suppliercode = 0,
                UserDiscounts = true,
                LastCountedAt = DateTime.Now,
                Remarks = ""
            };

            var batchContent = SimpleJsonToUISerialization.ConvertToUISerialization(
                JsonSerializer.Serialize(defaultBatch)
            );

            _batchForm = new GenEtoUI(
                batchContent,
                (a) =>
                {
                    // Save handler
                    var batch = JsonSerializer.Deserialize<Inventory>(JsonSerializer.Serialize(a));
                    var result = SendAuthenticatedRequest<Inventory, long>.Send(batch, "BatchSave");
                    if (result.Error == false)
                    {
                        MessageBox.Show(
                            TranslationHelper.Translate("Batch saved successfully", "Batch saved successfully", Program.lang),
                            TranslationHelper.Translate("Success", "Success", Program.lang),
                            MessageBoxType.Information
                        );
                        // Refresh search results
                        RefreshBatchSearchResults();
                        return result.Out;
                    }
                    else
                    {
                        MessageBox.Show(
                            TranslationHelper.Translate("Failed to save batch", "Failed to save batch", Program.lang),
                            TranslationHelper.Translate("Error", "Error", Program.lang),
                            MessageBoxType.Error
                        );
                        return -1;
                    }
                },
                (e) =>
                {
                    // Update handler
                    var batch = JsonSerializer.Deserialize<Inventory>(JsonSerializer.Serialize(e));
                    var result = SendAuthenticatedRequest<Inventory, long>.Send(batch, "BatchUpdate");
                    if (result.Error == false)
                    {
                        MessageBox.Show(
                            TranslationHelper.Translate("Batch updated successfully", "Batch updated successfully", Program.lang),
                            TranslationHelper.Translate("Success", "Success", Program.lang),
                            MessageBoxType.Information
                        );
                        // Refresh search results
                        RefreshBatchSearchResults();
                        return result.Out;
                    }
                    else
                    {
                        MessageBox.Show(
                            TranslationHelper.Translate("Failed to update batch", "Failed to update batch", Program.lang),
                            TranslationHelper.Translate("Error", "Error", Program.lang),
                            MessageBoxType.Error
                        );
                        return -1;
                    }
                },
                new Dictionary<string, (CommonUi.ShowAndGetValue, CommonUi.LookupValue)>(),
                "Batchcode",
                true,
                PanelColours: LocalColor
            );

            // Setup reactive updates
            _batchForm.AnythingChanged = (string[] currentControlGroup) =>
            {
                // Prevent recursion by checking if the change is from a dependent field
                if (currentControlGroup.Contains("CostPrice") ||
                    currentControlGroup.Contains("MarkedPrice") ||
                    currentControlGroup.Contains("SellingPrice"))
                {
                    // These are price fields that affect other fields, not dependent fields
                    return;
                }

                // Update dependent fields based on the changed field
                try
                {
                    // Get current values
                    double costPrice = (double)_batchForm.Lookup("CostPrice");
                    double markedPrice = (double)_batchForm.Lookup("MarkedPrice");
                    double sellingPrice = (double)_batchForm.Lookup("SellingPrice");
                    bool volumeDiscounts = (bool)_batchForm.Lookup("VolumeDiscounts");
                    bool userDiscounts = (bool)_batchForm.Lookup("UserDiscounts");

                    // Update prices based on business logic
                    if (!currentControlGroup.Contains("SellingPrice"))
                    {
                        // Calculate selling price based on cost price and markup
                        // This is just an example - adjust according to your business logic
                        if (costPrice > 0 && sellingPrice <= costPrice)
                        {
                            _batchForm.SetValue("SellingPrice", costPrice * 1.2); // 20% markup
                        }
                    }

                    if (!currentControlGroup.Contains("MarkedPrice"))
                    {
                        // Calculate marked price based on selling price
                        // This is just an example - adjust according to your business logic
                        if (sellingPrice > 0 && markedPrice <= sellingPrice)
                        {
                            _batchForm.SetValue("MarkedPrice", sellingPrice * 1.1); // 10% higher than selling
                        }
                    }

                    // Update discount settings
                    if (!currentControlGroup.Contains("VolumeDiscounts") && !currentControlGroup.Contains("UserDiscounts"))
                    {
                        // Enable discounts based on price settings
                        // This is just an example - adjust according to your business logic
                        if (sellingPrice > costPrice * 1.5)
                        {
                            _batchForm.SetValue("VolumeDiscounts", true);
                            _batchForm.SetValue("UserDiscounts", true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in reactive update: {ex.Message}");
                }
            };

            _formContainer = new Panel()
            {
                Content = _batchForm,
                Height = Program.InnerEditorHeight ?? -1,
                Width = Program.InnerEditorWidth ?? -1,
            };

            Content = new StackLayout(
                new StackLayout(
                    new StackLayoutItem(_itemSearchBox, true),
                    new StackLayoutItem(_batchSearchBox, true)
                )
                {
                    Orientation = Orientation.Horizontal,
                },
                new StackLayoutItem(_formContainer, true)
            )
            {
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
        }

        private void OnItemSelected()
        {
            try
            {
                var selectedId = long.Parse(_itemSearchBox.Selected[0]);
                var catalogueResult = SendAuthenticatedRequest<long, Catalogue>.Send(
                    selectedId,
                    "CatalogueRead"
                );

                if (catalogueResult.Error == false && catalogueResult.Out != null)
                {
                    _currentItem = catalogueResult.Out;

                    // Update the batch search to show only batches for this item
                    var itemBatches = _batchItems.Where(b => b.itemcode == selectedId).ToList();
                    List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SearchBatches = itemBatches
                        .Select(b => (new string[] {
                            b.itemcode.ToString(),
                            b.batchcode.ToString(),
                            b.SIH.ToString()
                        }, (Eto.Drawing.Color?)null, (Eto.Drawing.Color?)null))
                        .ToList();

                    // Create a new batch search panel with filtered data
                    List<(string, TextAlignment, bool)> BatchHeaderEntries = new()
                    {
                        (
                            TranslationHelper.Translate("Itemcode", "Itemcode", Program.lang),
                            TextAlignment.Right,
                            true
                        ),
                        (
                            TranslationHelper.Translate("Batchcode", "Batchcode", Program.lang),
                            TextAlignment.Right,
                            true
                        ),
                        (
                            TranslationHelper.Translate("Units", "Units", Program.lang),
                            TextAlignment.Right,
                            false
                        ),
                    };

                    _batchSearchBox = new SearchPanelEto(SearchBatches, BatchHeaderEntries, false, ColorSettings.RotateAllToPanelSettings(0));
                    _batchSearchBox.OnSelectionMade = OnBatchSelected;

                    // Update the batch search panel in the layout
                    var layout = (StackLayout)Content;
                    var searchLayout = (StackLayout)layout.Items[0].Control;
                    searchLayout.Items[1] = new StackLayoutItem(_batchSearchBox, true);
                    searchLayout.Invalidate();
                }
                else
                {
                    MessageBox.Show(
                        TranslationHelper.Translate("Failed to load item", "Failed to load item", Program.lang),
                        TranslationHelper.Translate("Error", "Error", Program.lang),
                        MessageBoxType.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    TranslationHelper.Translate("Error loading item", "Error loading item", Program.lang) + ": " + ex.Message,
                    TranslationHelper.Translate("Error", "Error", Program.lang),
                    MessageBoxType.Error
                );
            }
        }

        private void OnBatchSelected()
        {
            try
            {
                var itemcode = long.Parse(_batchSearchBox.Selected[0]);
                var batchcode = long.Parse(_batchSearchBox.Selected[1]);

                var batchResult = SendAuthenticatedRequest<long, Inventory>.Send(
                    batchcode,
                    "BatchRead"
                );

                if (batchResult.Error == false && batchResult.Out != null)
                {
                    _currentBatch = batchResult.Out;

                    // Update the form
                    _formContainer.Content = new GenEtoUI(
                        SimpleJsonToUISerialization.ConvertToUISerialization(
                            JsonSerializer.Serialize(batchResult.Out)
                        ),
                        (a) =>
                        {
                            // Save handler
                            var batch = JsonSerializer.Deserialize<Inventory>(JsonSerializer.Serialize(a));
                            var result = SendAuthenticatedRequest<Inventory, long>.Send(batch, "BatchSave");
                            if (result.Error == false)
                            {
                                MessageBox.Show(
                                    TranslationHelper.Translate("Batch saved successfully", "Batch saved successfully", Program.lang),
                                    TranslationHelper.Translate("Success", "Success", Program.lang),
                                    MessageBoxType.Information
                                );
                                // Refresh search results
                                RefreshBatchSearchResults();
                                return result.Out;
                            }
                            else
                            {
                                MessageBox.Show(
                                    TranslationHelper.Translate("Failed to save batch", "Failed to save batch", Program.lang),
                                    TranslationHelper.Translate("Error", "Error", Program.lang),
                                    MessageBoxType.Error
                                );
                                return -1;
                            }
                        },
                        (e) =>
                        {
                            // Update handler
                            var batch = JsonSerializer.Deserialize<Inventory>(JsonSerializer.Serialize(e));
                            var result = SendAuthenticatedRequest<Inventory, long>.Send(batch, "BatchUpdate");
                            if (result.Error == false)
                            {
                                MessageBox.Show(
                                    TranslationHelper.Translate("Batch updated successfully", "Batch updated successfully", Program.lang),
                                    TranslationHelper.Translate("Success", "Success", Program.lang),
                                    MessageBoxType.Information
                                );
                                // Refresh search results
                                RefreshBatchSearchResults();
                                return result.Out;
                            }
                            else
                            {
                                MessageBox.Show(
                                    TranslationHelper.Translate("Failed to update batch", "Failed to update batch", Program.lang),
                                    TranslationHelper.Translate("Error", "Error", Program.lang),
                                    MessageBoxType.Error
                                );
                                return -1;
                            }
                        },
                        new Dictionary<string, (CommonUi.ShowAndGetValue, CommonUi.LookupValue)>(),
                        "Batchcode",
                        true,
                        PanelColours: ColorSettings.RotateAllToPanelSettings(0)
                    );

                    // Setup reactive updates for the new form
                    var newForm = (GenEtoUI)_formContainer.Content;
                    newForm.AnythingChanged = (string[] currentControlGroup) =>
                    {
                        // Prevent recursion by checking if the change is from a dependent field
                        if (currentControlGroup.Contains("CostPrice") ||
                            currentControlGroup.Contains("MarkedPrice") ||
                            currentControlGroup.Contains("SellingPrice"))
                        {
                            // These are price fields that affect other fields, not dependent fields
                            return;
                        }

                        // Update dependent fields based on the changed field
                        try
                        {
                            // Get current values
                            double costPrice = (double)newForm.Lookup("CostPrice");
                            double markedPrice = (double)newForm.Lookup("MarkedPrice");
                            double sellingPrice = (double)newForm.Lookup("SellingPrice");
                            bool volumeDiscounts = (bool)newForm.Lookup("VolumeDiscounts");
                            bool userDiscounts = (bool)newForm.Lookup("UserDiscounts");

                            // Update prices based on business logic
                            if (!currentControlGroup.Contains("SellingPrice"))
                            {
                                // Calculate selling price based on cost price and markup
                                // This is just an example - adjust according to your business logic
                                if (costPrice > 0 && sellingPrice <= costPrice)
                                {
                                    newForm.SetValue("SellingPrice", costPrice * 1.2); // 20% markup
                                }
                            }

                            if (!currentControlGroup.Contains("MarkedPrice"))
                            {
                                // Calculate marked price based on selling price
                                // This is just an example - adjust according to your business logic
                                if (sellingPrice > 0 && markedPrice <= sellingPrice)
                                {
                                    newForm.SetValue("MarkedPrice", sellingPrice * 1.1); // 10% higher than selling
                                }
                            }

                            // Update discount settings
                            if (!currentControlGroup.Contains("VolumeDiscounts") && !currentControlGroup.Contains("UserDiscounts"))
                            {
                                // Enable discounts based on price settings
                                // This is just an example - adjust according to your business logic
                                if (sellingPrice > costPrice * 1.5)
                                {
                                    newForm.SetValue("VolumeDiscounts", true);
                                    newForm.SetValue("UserDiscounts", true);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in reactive update: {ex.Message}");
                        }
                    };

                    _formContainer.BackgroundColor =
                        ColorSettings.RotateAllToPanelSettings(0)?.BackgroundColor ?? CommonUi.ColorSettings.BackgroundColor;
                    _formContainer.Invalidate();
                }
                else
                {
                    MessageBox.Show(
                        TranslationHelper.Translate("Failed to load batch", "Failed to load batch", Program.lang),
                        TranslationHelper.Translate("Error", "Error", Program.lang),
                        MessageBoxType.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    TranslationHelper.Translate("Error loading batch", "Error loading batch", Program.lang) + ": " + ex.Message,
                    TranslationHelper.Translate("Error", "Error", Program.lang),
                    MessageBoxType.Error
                );
            }
        }

        private void RefreshBatchSearchResults()
        {
            // Refresh the batch search results to reflect any changes
            PosRefresh PR;
            while (true)
            {
                var req = (
                    SendAuthenticatedRequest<string, PosRefresh>.Send(
                        "Refresh",
                        "/PosRefreshBearerAuth",
                        true
                    )
                );
                if (req.Error == false)
                {
                    PR = req.Out;
                    break;
                }
            }

            // Update the batch items list
            _batchItems = PR.Batches;

            // Create a new batch search panel with updated data
            List<(string, TextAlignment, bool)> BatchHeaderEntries = new()
            {
                (
                    TranslationHelper.Translate("Itemcode", "Itemcode", Program.lang),
                    TextAlignment.Right,
                    true
                ),
                (
                    TranslationHelper.Translate("Batchcode", "Batchcode", Program.lang),
                    TextAlignment.Right,
                    true
                ),
                (
                    TranslationHelper.Translate("Units", "Units", Program.lang),
                    TextAlignment.Right,
                    false
                ),
            };

            List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SearchBatches = PR
                .Batches.Select<PosBatch, (string[], Eto.Drawing.Color?, Eto.Drawing.Color?)>
                (e => (new string[] { e.itemcode.ToString(), e.batchcode.ToString(), e.SIH.ToString() }, null, null))
                .ToList();

            _batchSearchBox = new SearchPanelEto(SearchBatches, BatchHeaderEntries, false, ColorSettings.RotateAllToPanelSettings(0));
            _batchSearchBox.OnSelectionMade = OnBatchSelected;

            // Update the batch search panel in the layout
            var layout = (StackLayout)Content;
            var searchLayout = (StackLayout)layout.Items[0].Control;
            searchLayout.Items[1] = new StackLayoutItem(_batchSearchBox, true);
            searchLayout.Invalidate();
        }
    }
}