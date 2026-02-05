using InvoicerBackend;
using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;
using System;
using System.Linq;

namespace InvoicerBackend
{
    public static class RefDocsEndpoints
    {
        public static WebApplication AddRefDocsEndpoints(this WebApplication app)
        {
            // Save RefDoc
            app.AddAsyncEndpointWithBearerAuth<RefDoc>(
                "SaveRefDoc",
                async (DataIn, LoginInfo) =>
                {
                    var doc = (RefDoc)DataIn;

                    // Force CLR default to trigger ValueGeneratedOnAdd (Auto-increment)
                    doc.RefId = 0;

                    // Constraint: Content must not exceed 8MiB
                    if (!string.IsNullOrEmpty(doc.RefImage))
                    {
                        if (doc.RefImage.Length > 11184810) // Approx 8MiB Base64 limit
                        {
                            throw new ArgumentException("Document image size exceeds the 8MiB limit.");
                        }
                    }

                    doc.AuthoredBy = (long)LoginInfo.UserId;
                    doc.CreatedAt = DateTime.UtcNow;

                    using (var ctx = new NewinvContext())
                    {
                        ctx.RefDocs.Add(doc);
                        await ctx.SaveChangesAsync();
                    }

                    return doc;
                },
                "Refresh"
            );

            // ReTranscribe RefDoc
            app.AddAsyncEndpointWithBearerAuth<RefDocsTranscription>(
                "ReTranscribeRefDoc",
                async (DataIn, LoginInfo) =>
                {
                    var req = (RefDocsTranscription)DataIn;

                    using (var ctx = new NewinvContext())
                    {
                        // Use the common service
                        var result = await TranscriptionService.AITranscribe(req.RefDoc, req.TranscriberLlmName, ctx);
                        return result;
                    }
                },
                "Refresh"
            );

            // GetDocuments (Last 100)
            app.AddEndpointWithBearerAuth<string>(
                "GetDocuments",
                (DataIn, LoginInfo) =>
                {
                    using (var ctx = new NewinvContext())
                    {
                        return ctx.RefDocs
                            .OrderByDescending(d => d.CreatedAt)
                            .Take(100)
                            .ToList();
                    }
                },
                "Refresh"
            );

            // SearchDocuments
            app.AddAsyncEndpointWithBearerAuth<DocumentSearchRequest>(
                "SearchDocuments",
                async (DataIn, LoginInfo) =>
                {
                    var req = (DocumentSearchRequest)DataIn;

                    // Calculate Time Range (Ensure UTC)
                    var (from, to) = CalculateTimeRange(req.From, req.To);

                    using (var ctx = new NewinvContext())
                    {
                        // Method Syntax: Join RefDocs with RefDocsTranscriptions manually
                        var query = ctx.RefDocs
                            .Join(ctx.RefDocsTranscriptions,
                                  doc => doc.RefId,
                                  trans => trans.RefDoc,
                                  (doc, trans) => new { doc, trans })
                            .Where(x => x.doc.CreatedAt >= from && x.doc.CreatedAt <= to);

                        if (!string.IsNullOrWhiteSpace(req.Query))
                        {
                            var lowerQuery = req.Query.ToLower();
                            query = query.Where(x =>
                                (x.trans.RefDocTitle != null && x.trans.RefDocTitle.ToLower().Contains(lowerQuery)) ||
                                (x.trans.TranscribedContent != null && x.trans.TranscribedContent.ToLower().Contains(lowerQuery)) ||
                                (x.trans.TranscriptionStructured != null && x.trans.TranscriptionStructured.ToLower().Contains(lowerQuery))
                            );
                        }

                        // Select distinct documents to avoid duplicates if multiple transcriptions match
                        return await query
                            .Select(x => x.doc)
                            .Distinct()
                            .OrderByDescending(d => d.CreatedAt)
                            .ToListAsync();
                    }
                },
                "Refresh"
            );

            // SuggestDocuments (Top 10)
            app.AddAsyncEndpointWithBearerAuth<DocumentSearchRequest>(
                "SuggestDocuments",
                async (DataIn, LoginInfo) =>
                {
                    var req = (DocumentSearchRequest)DataIn;

                    // Calculate Time Range
                    var (from, to) = CalculateTimeRange(req.From, req.To);

                    using (var ctx = new NewinvContext())
                    {
                        // Method Syntax: Join RefDocs with RefDocsTranscriptions manually
                        var query = ctx.RefDocs
                            .Join(ctx.RefDocsTranscriptions,
                                  doc => doc.RefId,
                                  trans => trans.RefDoc,
                                  (doc, trans) => new { doc, trans })
                            .Where(x => x.doc.CreatedAt >= from && x.doc.CreatedAt <= to);

                        if (!string.IsNullOrWhiteSpace(req.Query))
                        {
                            // Split query by space for OR logic
                            var terms = req.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                            // Search for ANY term in the transcription fields
                            query = query.Where(x => x.trans != null && terms.Any(term =>
                                (x.trans.RefDocTitle != null && x.trans.RefDocTitle.ToLower().Contains(term.ToLower())) ||
                                (x.trans.TranscribedContent != null && x.trans.TranscribedContent.ToLower().Contains(term.ToLower())) ||
                                (x.trans.TranscriptionStructured != null && x.trans.TranscriptionStructured.ToLower().Contains(term.ToLower()))
                            ));
                        }

                        return await query
                            .Select(x => x.doc)
                            .Distinct()
                            .OrderByDescending(d => d.CreatedAt)
                            .Take(10)
                            .ToListAsync();
                    }
                },
                "Refresh"
            );

            // Get Transcriptions for a specific RefDoc
            app.AddEndpointWithBearerAuth<long>(
                "GetTranscriptions",
                (RefIdIn, LoginInfo) =>
                {
                    var refId = (long)RefIdIn;
                    using (var ctx = new NewinvContext())
                    {
                        return ctx.RefDocsTranscriptions
                            .Where(t => t.RefDoc == refId)
                            .OrderByDescending(t => t.TranscribedAt)
                            .ToList();
                    }
                },
                "Refresh"
            );

            return app;
        }

        // DTO for Search Requests
        public class DocumentSearchRequest
        {
            public string Query { get; set; }
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
        }

        // Helper for Time Range Calculation
        private static (DateTime From, DateTime To) CalculateTimeRange(DateTime? from, DateTime? to)
        {
            DateTime now = DateTime.UtcNow;
            DateTime rangeFrom, rangeTo;

            if (from.HasValue && to.HasValue)
            {
                // Both specified
                rangeFrom = from.Value.Kind == DateTimeKind.Utc ? from.Value : from.Value.ToUniversalTime();
                rangeTo = to.Value.Kind == DateTimeKind.Utc ? to.Value : to.Value.ToUniversalTime();
            }
            else if (to.HasValue)
            {
                // Only To specified: From = To - 6 months
                rangeTo = to.Value.Kind == DateTimeKind.Utc ? to.Value : to.Value.ToUniversalTime();
                rangeFrom = rangeTo.AddMonths(-6);
            }
            else if (from.HasValue)
            {
                // Only From specified: To = From + 6 months
                rangeFrom = from.Value.Kind == DateTimeKind.Utc ? from.Value : from.Value.ToUniversalTime();
                rangeTo = rangeFrom.AddMonths(6);
            }
            else
            {
                // None specified: From = Now - 6 months, To = Now
                rangeTo = now;
                rangeFrom = now.AddMonths(-6);
            }

            return (rangeFrom, rangeTo);
        }
    }
}