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
    public class CatalogueEditPanel : Panel
    {
        private GenEtoUI _catalogueForm;
        private SearchPanelEto _searchBox;
        private InventoryImageEditorPanel _imageEditPanel;
        private Panel _imageEditPanelContainer;
        private Panel _scrollableLower;
        private List<PosCatalogue> _catalogueItems = new();

        public CatalogueEditPanel()
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

            // Get catalogue data
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

            // Store catalogue items for later use
            _catalogueItems = PR.Catalogue;

            // Setup search panel
            List<(string, TextAlignment, bool)> HeaderEntries = new()
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

            _searchBox = new SearchPanelEto(SearchCatalogue, HeaderEntries, false, LocalColor);
            _searchBox.OnSelectionMade = OnCatalogueSelected;

            // Setup image editor panel
            _imageEditPanel = new InventoryImageEditorPanel(
                null,
                (itemcode, imageid) =>
                {
                    if (itemcode != null)
                    {
                        var IA = SendAuthenticatedRequest<long, InventoryImage>.Send(
                            (long)itemcode,
                            "CatalogueDefaultImageGet"
                        );
                        return IA.Out?.ImageBase64;
                    }
                    else
                    {
                        return null;
                    }
                },
                (itemcode, imageid, imageData) =>
                {
                    var IA = SendAuthenticatedRequest<InventoryImage, long?>.Send(
                        new InventoryImage()
                        {
                            Itemcode = (long)itemcode,
                            Imageid = imageid,
                            ImageBase64 = imageData,
                        },
                        "CatalogueDefaultImageSet"
                    );
                    return IA.Out;
                }
            );

            _imageEditPanelContainer = new Panel()
            {
                Content = _imageEditPanel,
                Size = new Eto.Drawing.Size(200, 500),
            };

            // Setup catalogue form with reactive updates
            var defaultCatalogue = new Catalogue()
            {
                Active = true,
                ActiveWeb = true,
                CategoriesBitmask = 0b1,
                CreatedOn = DateTime.Now,
                DefaultVatCategory = 0,
                Description = "",
                DescriptionPos = "",
                DescriptionsOtherLanguages = 0,
                DescriptionWeb = "",
                EnforceAboveCost = true,
                ExpiryTrackingEnabled = true,
                Itemcode = 0,
                PermissionsCategory = 0,
                PriceManual = true,
                VatCategoryAdjustable = true,
                VatDependsOnUser = true,
            };

            var catalogueContent = SimpleJsonToUISerialization.ConvertToUISerialization(
                JsonSerializer.Serialize(defaultCatalogue)
            );

            _catalogueForm = new GenEtoUI(
                catalogueContent,
                (a) =>
                {
                    // Save handler
                    var catalogue = JsonSerializer.Deserialize<Catalogue>(JsonSerializer.Serialize(a));
                    var result = SendAuthenticatedRequest<Catalogue, long>.Send(catalogue, "CatalogueSave");
                    if (result.Error == false)
                    {
                        MessageBox.Show(
                            TranslationHelper.Translate("Catalogue saved successfully", "Catalogue saved successfully", Program.lang),
                            TranslationHelper.Translate("Success", "Success", Program.lang),
                            MessageBoxType.Information
                        );
                        // Refresh search results
                        RefreshSearchResults();
                        return result.Out;
                    }
                    else
                    {
                        MessageBox.Show(
                            TranslationHelper.Translate("Failed to save catalogue", "Failed to save catalogue", Program.lang),
                            TranslationHelper.Translate("Error", "Error", Program.lang),
                            MessageBoxType.Error
                        );
                        return -1;
                    }
                },
                (e) =>
                {
                    // Update handler
                    var catalogue = JsonSerializer.Deserialize<Catalogue>(JsonSerializer.Serialize(e));
                    var result = SendAuthenticatedRequest<Catalogue, long>.Send(catalogue, "CatalogueUpdate");
                    if (result.Error == false)
                    {
                        MessageBox.Show(
                            TranslationHelper.Translate("Catalogue updated successfully", "Catalogue updated successfully", Program.lang),
                            TranslationHelper.Translate("Success", "Success", Program.lang),
                            MessageBoxType.Information
                        );
                        // Refresh search results
                        RefreshSearchResults();
                        return result.Out;
                    }
                    else
                    {
                        MessageBox.Show(
                            TranslationHelper.Translate("Failed to update catalogue", "Failed to update catalogue", Program.lang),
                            TranslationHelper.Translate("Error", "Error", Program.lang),
                            MessageBoxType.Error
                        );
                        return -1;
                    }
                },
                new Dictionary<string, (CommonUi.ShowAndGetValue, CommonUi.LookupValue)>(),
                "Itemcode",
                true,
                PanelColours: LocalColor
            );

            // Setup reactive updates
            _catalogueForm.AnythingChanged = (string[] currentControlGroup) =>
            {
                // Prevent recursion by checking if the change is from a dependent field
                if (currentControlGroup.Contains("PriceManual") ||
                    currentControlGroup.Contains("EnforceAboveCost") ||
                    currentControlGroup.Contains("VatDependsOnUser") ||
                    currentControlGroup.Contains("VatCategoryAdjustable"))
                {
                    // These are control fields that affect other fields, not dependent fields
                    return;
                }

                // Update dependent fields based on the changed field
                try
                {
                    // Get current values
                    bool priceManual = (bool)_catalogueForm.Lookup("PriceManual");
                    bool enforceAboveCost = (bool)_catalogueForm.Lookup("EnforceAboveCost");
                    bool vatDependsOnUser = (bool)_catalogueForm.Lookup("VatDependsOnUser");
                    bool vatCategoryAdjustable = (bool)_catalogueForm.Lookup("VatCategoryAdjustable");

                    // Update fields based on the control fields
                    if (priceManual && !currentControlGroup.Contains("PriceManual"))
                    {
                        // When price is manual, certain fields should be enabled/disabled
                        // This is just an example - adjust according to your business logic
                    }

                    if (enforceAboveCost && !currentControlGroup.Contains("EnforceAboveCost"))
                    {
                        // When enforcing above cost, certain fields should be validated
                    }

                    if (vatDependsOnUser && !currentControlGroup.Contains("VatDependsOnUser"))
                    {
                        // When VAT depends on user, certain fields should be updated
                    }

                    if (vatCategoryAdjustable && !currentControlGroup.Contains("VatCategoryAdjustable"))
                    {
                        // When VAT category is adjustable, certain fields should be updated
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in reactive update: {ex.Message}");
                }
            };

            _scrollableLower = new Panel()
            {
                Content = _catalogueForm,
                Height = Program.InnerEditorHeight ?? -1,
                Width = Program.InnerEditorWidth ?? -1,
            };

            Content = new StackLayout(
                new StackLayout(new StackLayoutItem(_searchBox, true))
                {
                    Orientation = Orientation.Horizontal,
                },
                new StackLayoutItem(
                    new StackLayout(
                        new StackLayoutItem(_scrollableLower, true),
                        _imageEditPanelContainer
                    )
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalContentAlignment = VerticalAlignment.Stretch,
                    },
                    true
                )
            )
            {
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
        }

        private void OnCatalogueSelected()
        {
            try
            {
                var selectedId = long.Parse(_searchBox.Selected[0]);
                var catalogueResult = SendAuthenticatedRequest<long, Catalogue>.Send(
                    selectedId,
                    "CatalogueRead"
                );

                if (catalogueResult.Error == false && catalogueResult.Out != null)
                {
                    // Update image editor
                    _imageEditPanel = new InventoryImageEditorPanel(
                        selectedId,
                        (itemcode, imageid) =>
                        {
                            if (itemcode != null)
                            {
                                var IA = SendAuthenticatedRequest<long, InventoryImage>.Send(
                                    (long)itemcode,
                                    "CatalogueDefaultImageGet"
                                );
                                return IA.Out?.ImageBase64;
                            }
                            else
                            {
                                return null;
                            }
                        },
                        (itemcode, imageid, imageData) =>
                        {
                            var IA = SendAuthenticatedRequest<InventoryImage, long?>.Send(
                                new InventoryImage()
                                {
                                    Itemcode = (long)itemcode,
                                    Imageid = imageid,
                                    ImageBase64 = imageData,
                                },
                                "CatalogueDefaultImageSet"
                            );
                            return IA.Out;
                        }
                    );
                    _imageEditPanelContainer.Content = _imageEditPanel;

                    // Update form
                    _scrollableLower.Content = new GenEtoUI(
                        SimpleJsonToUISerialization.ConvertToUISerialization(
                            JsonSerializer.Serialize(catalogueResult.Out)
                        ),
                        (a) =>
                        {
                            // Save handler
                            var catalogue = JsonSerializer.Deserialize<Catalogue>(JsonSerializer.Serialize(a));
                            var result = SendAuthenticatedRequest<Catalogue, long>.Send(catalogue, "CatalogueSave");
                            if (result.Error == false)
                            {
                                MessageBox.Show(
                                    TranslationHelper.Translate("Catalogue saved successfully", "Catalogue saved successfully", Program.lang),
                                    TranslationHelper.Translate("Success", "Success", Program.lang),
                                    MessageBoxType.Information
                                );
                                // Refresh search results
                                RefreshSearchResults();
                                return result.Out;
                            }
                            else
                            {
                                MessageBox.Show(
                                    TranslationHelper.Translate("Failed to save catalogue", "Failed to save catalogue", Program.lang),
                                    TranslationHelper.Translate("Error", "Error", Program.lang),
                                    MessageBoxType.Error
                                );
                                return -1;
                            }
                        },
                        (e) =>
                        {
                            // Update handler
                            var catalogue = JsonSerializer.Deserialize<Catalogue>(JsonSerializer.Serialize(e));
                            var result = SendAuthenticatedRequest<Catalogue, long>.Send(catalogue, "CatalogueUpdate");
                            if (result.Error == false)
                            {
                                MessageBox.Show(
                                    TranslationHelper.Translate("Catalogue updated successfully", "Catalogue updated successfully", Program.lang),
                                    TranslationHelper.Translate("Success", "Success", Program.lang),
                                    MessageBoxType.Information
                                );
                                // Refresh search results
                                RefreshSearchResults();
                                return result.Out;
                            }
                            else
                            {
                                MessageBox.Show(
                                    TranslationHelper.Translate("Failed to update catalogue", "Failed to update catalogue", Program.lang),
                                    TranslationHelper.Translate("Error", "Error", Program.lang),
                                    MessageBoxType.Error
                                );
                                return -1;
                            }
                        },
                        new Dictionary<string, (CommonUi.ShowAndGetValue, CommonUi.LookupValue)>(),
                        "Itemcode",
                        true,
                        PanelColours: ColorSettings.RotateAllToPanelSettings(0)
                    );

                    // Setup reactive updates for the new form
                    var newForm = (GenEtoUI)_scrollableLower.Content;
                    newForm.AnythingChanged = (string[] currentControlGroup) =>
                    {
                        // Prevent recursion by checking if the change is from a dependent field
                        if (currentControlGroup.Contains("PriceManual") ||
                            currentControlGroup.Contains("EnforceAboveCost") ||
                            currentControlGroup.Contains("VatDependsOnUser") ||
                            currentControlGroup.Contains("VatCategoryAdjustable"))
                        {
                            // These are control fields that affect other fields, not dependent fields
                            return;
                        }

                        // Update dependent fields based on the changed field
                        try
                        {
                            // Get current values
                            bool priceManual = (bool)newForm.Lookup("PriceManual");
                            bool enforceAboveCost = (bool)newForm.Lookup("EnforceAboveCost");
                            bool vatDependsOnUser = (bool)newForm.Lookup("VatDependsOnUser");
                            bool vatCategoryAdjustable = (bool)newForm.Lookup("VatCategoryAdjustable");

                            // Update fields based on the control fields
                            if (priceManual && !currentControlGroup.Contains("PriceManual"))
                            {
                                // When price is manual, certain fields should be enabled/disabled
                                // This is just an example - adjust according to your business logic
                            }

                            if (enforceAboveCost && !currentControlGroup.Contains("EnforceAboveCost"))
                            {
                                // When enforcing above cost, certain fields should be validated
                            }

                            if (vatDependsOnUser && !currentControlGroup.Contains("VatDependsOnUser"))
                            {
                                // When VAT depends on user, certain fields should be updated
                            }

                            if (vatCategoryAdjustable && !currentControlGroup.Contains("VatCategoryAdjustable"))
                            {
                                // When VAT category is adjustable, certain fields should be updated
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in reactive update: {ex.Message}");
                        }
                    };

                    _scrollableLower.BackgroundColor =
                        ColorSettings.RotateAllToPanelSettings(0)?.BackgroundColor ?? CommonUi.ColorSettings.BackgroundColor;
                    _scrollableLower.Invalidate();
                }
                else
                {
                    MessageBox.Show(
                        TranslationHelper.Translate("Failed to load catalogue", "Failed to load catalogue", Program.lang),
                        TranslationHelper.Translate("Error", "Error", Program.lang),
                        MessageBoxType.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    TranslationHelper.Translate("Error loading catalogue", "Error loading catalogue", Program.lang) + ": " + ex.Message,
                    TranslationHelper.Translate("Error", "Error", Program.lang),
                    MessageBoxType.Error
                );
            }
        }

        private void RefreshSearchResults()
        {
            // Refresh the search results to reflect any changes
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

            // Update the catalogue items list
            _catalogueItems = PR.Catalogue;

            // Create a new search panel with updated data
            List<(string, TextAlignment, bool)> HeaderEntries = new()
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

            _searchBox = new SearchPanelEto(SearchCatalogue, HeaderEntries, false, ColorSettings.RotateAllToPanelSettings(0));
            _searchBox.OnSelectionMade = OnCatalogueSelected;

            // Update the search panel in the layout
            var layout = (StackLayout)Content;
            var searchLayout = (StackLayout)layout.Items[0].Control;
            searchLayout.Items[0] = new StackLayoutItem(_searchBox, true);
            searchLayout.Invalidate();
        }
    }
}