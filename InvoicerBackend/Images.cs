using InvoicerBackend;
using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace InvoicerBackend
{
    public static class Images
    {
        public static WebApplication AddCatalogueImageEndpoints(this WebApplication app)
        {
            // 1. Catalogue Default Image Get
            app.AddAsyncEndpointWithBearerAuth<CatalogueGetRequest>(
                "CatalogueDefaultImageGet",
                async (ItemCodeIn, LoginInfo) =>
                {
                    CatalogueGetRequest itemCode = ((CatalogueGetRequest)ItemCodeIn);

                    using (var ctx = new NewinvContext())
                    {
                        // 1. Get Catalogue Item
                        var cat = await ctx.Catalogues
                            .FirstOrDefaultAsync(c => c.Itemcode == itemCode.ItemCode);

                        if (cat == null || cat.RefDocId == null || cat.RefDocId == 0)
                        {
                            return null; // No item or no image linked
                        }

                        // 2. Get RefDoc
                        var doc = await ctx.RefDocs
                            .FirstOrDefaultAsync(d => d.RefId == cat.RefDocId);

                        if (doc == null)
                        {
                            return null; // Inconsistent state
                        }

                        return new { RefId = doc.RefId, ImageBase64 = doc.RefImage };
                    }
                },
                "Refresh"
            );

            // 2. Catalogue Default Image Set
            // 2. Catalogue Default Image Set
            app.AddAsyncEndpointWithBearerAuth<CatalogueImageRequest>(
                "CatalogueDefaultImageSet",
                async (DataIn, LoginInfo) =>
                {
                    var req = (CatalogueImageRequest)DataIn;

                    // Validate Size (10MB)
                    if (!string.IsNullOrEmpty(req.ImageBase64))
                    {
                        if (req.ImageBase64.Length > 13981012) // Approx 10MiB Base64 limit
                        {
                            throw new ArgumentException("Image size exceeds 10MiB limit.");
                        }
                    }

                    using (var ctx = new NewinvContext())
                    {
                        // Check if Catalogue item exists
                        var cat = await ctx.Catalogues.FirstOrDefaultAsync(c => c.Itemcode == req.ItemCode);
                        if (cat == null)
                        {
                            throw new ArgumentException("Catalogue item not found.");
                        }

                        // Check for existing RefDoc
                        RefDoc docToSave;
                        var existingDoc = await ctx.RefDocs
                            .FirstOrDefaultAsync(d => d.RefId == cat.RefDocId);

                        if (existingDoc == null)
                        {
                            // Create new RefDoc
                            docToSave = new RefDoc
                            {
                                RefText = cat.Itemcode.ToString(),
                                RefImage = req.ImageBase64,
                                CreatedAt = DateTime.UtcNow,
                                AuthoredBy = (long)LoginInfo.UserId
                            };
                            ctx.RefDocs.Add(docToSave);

                            // 1. Save to generate RefDoc ID
                            await ctx.SaveChangesAsync();

                            // 2. Update Catalogue with the new RefDoc ID
                            cat.RefDocId = docToSave.RefId;
                            ctx.Entry(cat).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            await ctx.SaveChangesAsync();
                        }
                        else
                        {
                            // Update existing RefDoc
                            existingDoc.RefImage = req.ImageBase64;
                            existingDoc.AuthoredBy = (long)LoginInfo.UserId;
                            docToSave = existingDoc;

                            // cat.RefDocId is already correct (matches existingDoc.RefId)
                            // Just save the image update
                            await ctx.SaveChangesAsync();
                        }

                        // Trigger Transcription (Async)
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

        // DTO for Set Request
        public class CatalogueImageRequest
        {
            public long ItemCode { get; set; }
            public string ImageBase64 { get; set; }
        }
    }
}