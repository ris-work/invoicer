using Microsoft.EntityFrameworkCore;
using MyAOTFriendlyExtensions;
using RV.InvNew.Common;
using System.Text.Json;

namespace InvoicerBackend
{
    // --- PII Endpoints ---

    // 1. Create PII (with optional Account Creation)
    public class CreatePiiRequest
    {
        public Pii PiiData { get; set; }
        public bool CreateAccount { get; set; }
        public int AccountType { get; set; } // 0=Asset, 1=Liability, etc. (if creating)
    }
    public class SearchPiiRequest { public string Query { get; set; } }

    // --- PII Image Endpoints ---

    // 1. Add PII Image
    public class AddPiiImageRequest
    {
        public long PiiId { get; set; }
        public string ImageBase64 { get; set; }
    }

    // 2. Get PII Images
    public class GetPiiImagesRequest
    {
        public long PiiId { get; set; }
    }
    public static class PiiEndpoints
    {
        public static WebApplication AddPiiEndpoints(this WebApplication app)
        {
            app.AddAsyncEndpointWithBearerAuth<CreatePiiRequest>(
                "CreatePiiWeb",
                async (DataIn, LoginInfo) =>
                {
                    var req = (CreatePiiRequest)DataIn;
                    var pii = req.PiiData;

                    pii.Id = 0; // Force new

                    using (var ctx = new NewinvContext())
                    {
                        ctx.Piis.Add(pii);
                        await ctx.SaveChangesAsync(); // Save to get ID

                        if (req.CreateAccount)
                        {
                            var acc = new AccountsInformation
                            {
                                AccountName = pii.Name,
                                AccountPii = pii.Id,
                                AccountType = req.AccountType,
                                AccountMin = -1000000000,
                                AccountMax = 1000000000,
                                HumanFriendlyId = $"PII-{pii.Id}"
                            };
                            ctx.AccountsInformations.Add(acc);
                            await ctx.SaveChangesAsync();
                        }
                    }
                    return pii;
                },
                "Refresh"
            );

            // 2. Edit PII (Patch)
            app.AddAsyncPatchEndpointWithBearerAuth<string>(
                "EditPiiWeb",
                async (DataIn, LoginInfo) =>
                {
                    var Patch = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>((string)DataIn);
                    long id = Patch["Id"].GetInt64();

                    using (var ctx = new NewinvContext())
                    {
                        var entity = await ctx.Piis.FirstOrDefaultAsync(p => p.Id == id);
                        if (entity == null) throw new ArgumentException("PII not found");

                        var patched = entity.ApplyChangesExceptFilteredFromJson(new[] { "Id" }, (string)DataIn);
                        ctx.Entry(entity).CurrentValues.SetValues(patched);
                        await ctx.SaveChangesAsync();
                    }
                    return true;
                },
                new[] { "Id" },
                "Refresh"
            );


            app.AddAsyncEndpointWithBearerAuth<SearchPiiRequest>(
                "SearchPiiWeb",
                async (DataIn, LoginInfo) =>
                {
                    var req = (SearchPiiRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        var q = ctx.Piis.AsQueryable();
                        if (!string.IsNullOrEmpty(req.Query))
                        {
                            q = q.Where(p => p.Name.ToLower().Contains(req.Query.ToLower()) ||
                                             (p.Email != null && p.Email.ToLower().Contains(req.Query.ToLower())));
                        }
                        return await q.OrderByDescending(p => p.Id).Take(50).ToListAsync();
                    }
                },
                "Refresh"
            );

            app.AddAsyncEndpointWithBearerAuth<AddPiiImageRequest>(
    "AddPiiImage",
    async (DataIn, LoginInfo) =>
    {
        var req = (AddPiiImageRequest)DataIn;

        using (var ctx = new NewinvContext())
        {
            // Determine next ImageNo (if not auto-increment)
            // Assuming ImageNo might need to be calculated if it's not Identity
            long maxNo = 0;
            if (await ctx.PiiImages.AnyAsync(i => i.PiiId == req.PiiId))
            {
                maxNo = await ctx.PiiImages.Where(i => i.PiiId == req.PiiId).MaxAsync(i => i.ImageNo);
            }

            var img = new PiiImage
            {
                PiiId = req.PiiId,
                ImageNo = maxNo + 1,
                Image = req.ImageBase64
            };

            ctx.PiiImages.Add(img);
            await ctx.SaveChangesAsync();
            return img;
        }
    },
    "Refresh"
);


            app.AddAsyncEndpointWithBearerAuth<GetPiiImagesRequest>(
                "GetPiiImages",
                async (DataIn, LoginInfo) =>
                {
                    var req = (GetPiiImagesRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        return await ctx.PiiImages
                            .Where(i => i.PiiId == req.PiiId)
                            .OrderBy(i => i.ImageNo)
                            .ToListAsync();
                    }
                },
                "Refresh"
            );


            return app;
        }

    }
}
