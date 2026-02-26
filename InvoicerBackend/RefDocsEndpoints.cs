using InvoicerBackend;
using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;
using System;
using System.Linq;

namespace InvoicerBackend
{
    public static class RefDocsEndpoints
    {
        // DTO for requesting transcriptions by ID
        public class TranscriptionIdRequest
        {
            public long RefId { get; set; }
        }

        public static WebApplication AddRefDocsEndpoints(this WebApplication app)
        {
            // Save RefDoc (POST)
            app.AddAsyncEndpointWithBearerAuth<RefDoc, RefDoc>(
                "SaveRefDoc",
                async (DataIn, LoginInfo) =>
                {
                    var doc = (RefDoc)DataIn;
                    doc.RefId = 0; // Force CLR default

                    if (!string.IsNullOrEmpty(doc.RefImage))
                    {
                        if (doc.RefImage.Length > 11184810)
                            throw new ArgumentException("Document image size exceeds the 8MiB limit.");
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

            // ReTranscribe RefDoc (POST)
            app.AddAsyncEndpointWithBearerAuth<RefDocsTranscription, RefDocsTranscription>(
                "ReTranscribeRefDoc",
                async (DataIn, LoginInfo) =>
                {
                    var req = (RefDocsTranscription)DataIn;

                    using (var ctx = new NewinvContext())
                    {
                        var result = await TranscriptionService.AITranscribe(req.RefDoc, req.TranscriberLlmName, ctx);
                        return result;
                    }
                },
                "Refresh"
            );

            // GetDocuments (POST) - Changed from GET
            app.AddAsyncEndpointWithBearerAuth<object, List<RefDoc>>(
                "GetDocuments",
                async (DataIn, LoginInfo) =>
                {
                    using (var ctx = new NewinvContext())
                    {
                        return await ctx.RefDocs
                            .OrderByDescending(d => d.CreatedAt)
                            .Take(100)
                            .ToListAsync();
                    }
                },
                "Refresh"
            );

            // SearchDocuments (POST)
            app.AddAsyncEndpointWithBearerAuth<DocumentSearchRequest, object>(
                "SearchDocuments",
                async (DataIn, LoginInfo) =>
                {
                    var req = (DocumentSearchRequest)DataIn;
                    var (from, to) = CalculateTimeRange(req.From, req.To);

                    using (var ctx = new NewinvContext())
                    {
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

                        return await query
                            .Select(x => x.doc)
                            .Distinct()
                            .OrderByDescending(d => d.CreatedAt)
                            .ToListAsync();
                    }
                },
                "Refresh"
            );

            // SuggestDocuments (POST)
            app.AddAsyncEndpointWithBearerAuth<DocumentSearchRequest, object>(
                "SuggestDocuments",
                async (DataIn, LoginInfo) =>
                {
                    var req = (DocumentSearchRequest)DataIn;
                    var (from, to) = CalculateTimeRange(req.From, req.To);

                    using (var ctx = new NewinvContext())
                    {
                        var query = ctx.RefDocs
                            .Join(ctx.RefDocsTranscriptions,
                                  doc => doc.RefId,
                                  trans => trans.RefDoc,
                                  (doc, trans) => new { doc, trans })
                            .Where(x => x.doc.CreatedAt >= from && x.doc.CreatedAt <= to);

                        if (!string.IsNullOrWhiteSpace(req.Query))
                        {
                            var terms = req.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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

            // GetTranscriptions (POST) - Changed from GET
            app.AddAsyncEndpointWithBearerAuth<TranscriptionIdRequest, List<RefDocsTranscription>>(
                "GetTranscriptions",
                async (DataIn, LoginInfo) =>
                {
                    var req = (TranscriptionIdRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        return await ctx.RefDocsTranscriptions
                            .Where(t => t.RefDoc == req.RefId)
                            .OrderByDescending(t => t.TranscribedAt)
                            .ToListAsync();
                    }
                },
                "Refresh"
            );
            // GetUntranscribed (Fetches docs where transcription is missing or empty)

            // GetUntranscribed (NEW)
            // GetUntranscribed (Target of the complaint)
            // GetUntranscribed
            app.AddAsyncEndpointWithBearerAuth<object, List<RefDoc>>(
                "GetUntranscribed",
                async (DataIn, LoginInfo) =>
                {
                    using (var ctx = new NewinvContext())
                    {
                        // x.t is IEnumerable<RefDocsTranscription>, so we must check the collection
                        var query = ctx.RefDocs
                            .GroupJoin(ctx.RefDocsTranscriptions,
                                      d => d.RefId,
                                      t => t.RefDoc,
                                      (d, t) => new { d, t })
                            // Logic: No transcriptions exist OR All transcriptions have empty content
                            .Where(x => !x.t.Any() || x.t.All(trans => string.IsNullOrEmpty(trans.TranscribedContent)))
                            .Select(x => x.d)
                            .OrderByDescending(d => d.CreatedAt)
                            .Take(100);

                        return await query.ToListAsync();
                    }
                },
                "Refresh"
            );

            return app;
        }

        public class DocumentSearchRequest
        {
            public string Query { get; set; }
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
        }

        private static (DateTime From, DateTime To) CalculateTimeRange(DateTime? from, DateTime? to)
        {
            DateTime now = DateTime.UtcNow;
            DateTime rangeFrom, rangeTo;

            if (from.HasValue && to.HasValue)
            {
                rangeFrom = from.Value.Kind == DateTimeKind.Utc ? from.Value : from.Value.ToUniversalTime();
                rangeTo = to.Value.Kind == DateTimeKind.Utc ? to.Value : to.Value.ToUniversalTime();
            }
            else if (to.HasValue)
            {
                rangeTo = to.Value.Kind == DateTimeKind.Utc ? to.Value : to.Value.ToUniversalTime();
                rangeFrom = rangeTo.AddMonths(-6);
            }
            else if (from.HasValue)
            {
                rangeFrom = from.Value.Kind == DateTimeKind.Utc ? from.Value : from.Value.ToUniversalTime();
                rangeTo = rangeFrom.AddMonths(6);
            }
            else
            {
                rangeTo = now;
                rangeFrom = now.AddMonths(-6);
            }

            return (rangeFrom, rangeTo);
        }
    }
}