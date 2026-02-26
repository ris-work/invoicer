using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using InvoicerBackend;
using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;

namespace RV.InvNew.Common
{
    public static class Images
    {
        public static WebApplication AddBatchDefaultImageEndpoints(this WebApplication app)
        {
            // 3. Batch Default Image Get
            app.AddAsyncEndpointWithBearerAuth<long, object>(
                "BatchDefaultImageGet",
                async (ItemCodeIn, LoginInfo) =>
                {
                    long itemCode = ((long)ItemCodeIn);

                    using (var ctx = new NewinvContext())
                    {
                        // Find Inventory Item
                        var inv = await ctx.Inventories
                            .FirstOrDefaultAsync(i => i.Itemcode == itemCode);

                        if (inv == null || inv.RefDocId == null || inv.RefDocId == 0)
                        {
                            return null; // No item or no image linked
                        }

                        // Find RefDoc
                        var doc = await ctx.RefDocs
                            .FirstOrDefaultAsync(d => d.RefId == inv.RefDocId);

                        if (doc == null)
                        {
                            return null;
                        }

                        return new { RefId = doc.RefId, ImageBase64 = doc.RefImage };
                    }
                },
                "Refresh"
            );

            // 4. Batch Default Image Set
            app.AddAsyncEndpointWithBearerAuth<InventoryImageRequest, object>(
                "BatchDefaultImageSet",
                async (DataIn, LoginInfo) =>
                {
                    var req = (InventoryImageRequest)DataIn;

                    // Validate Size (10MB)
                    if (!string.IsNullOrEmpty(req.ImageBase64))
                    {
                        if (req.ImageBase64.Length > 13981012)
                        {
                            throw new ArgumentException("Image size exceeds 10MiB limit.");
                        }
                    }

                    using (var ctx = new NewinvContext())
                    {
                        // Find Inventory Item
                        var inv = await ctx.Inventories
                            .FirstOrDefaultAsync(i => i.Itemcode == req.Itemcode);

                        if (inv == null)
                        {
                            throw new ArgumentException("Inventory item not found.");
                        }

                        // Check for existing RefDoc
                        RefDoc docToSave;
                        var existingDoc = await ctx.RefDocs
                            .FirstOrDefaultAsync(d => d.RefId == inv.RefDocId);

                        if (existingDoc == null)
                        {
                            // Create new RefDoc
                            docToSave = new RefDoc
                            {
                                RefText = inv.Itemcode.ToString(),
                                RefImage = req.ImageBase64,
                                CreatedAt = DateTime.UtcNow,
                                AuthoredBy = (long)LoginInfo.UserId
                            };
                            ctx.RefDocs.Add(docToSave);
                        }
                        else
                        {
                            // Update existing RefDoc
                            existingDoc.RefImage = req.ImageBase64;
                            existingDoc.AuthoredBy = (long)LoginInfo.UserId;
                            docToSave = existingDoc;
                        }

                        // Link Inventory to RefDoc
                        inv.RefDocId = docToSave.RefId;

                        await ctx.SaveChangesAsync();

                        // Trigger Transcription
                        try
                        {
                            await TranscriptionService.AITranscribe(docToSave.RefId, "z-ai/glm-4.6v", ctx);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Transcription failed for RefDoc {docToSave.RefId}: {ex.Message}");
                        }

                        return new { RefId = docToSave.RefId, ImageBase64 = req.ImageBase64 };
                    }
                },
                "Refresh"
            );

            return app;
        }
    }

    // DTO for Set Request
    public class InventoryImageRequest
    {
        public long Itemcode { get; set; }
        public string ImageBase64 { get; set; }
    }
}