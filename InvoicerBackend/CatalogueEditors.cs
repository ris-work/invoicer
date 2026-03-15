using RV.InvNew.Common;
using System.Text.Json;
using MyAOTFriendlyExtensions;
using Microsoft.EntityFrameworkCore;

namespace InvoicerBackend
{
    public static class CatalogueEditors
    {
        private static void Log(string a) { System.Console.WriteLine(a); }
        public static WebApplication AddCatalogueEditorHandlers(this WebApplication app)
        {
            app.AddAsyncEndpointWithBearerAuth<Catalogue, bool>("CreateCatalogueItem", async (o, a) => {
                using (var ctx = new NewinvContext())
                {
                    var N = (Catalogue)o;
                    await ctx.Catalogues.AddAsync(N);
                    await ctx.SaveChangesAsync();
                }
                return true;
            }, "Refresh");
            app.AddAsyncPatchEndpointWithBearerAuth<string, bool>("EditCatalogueItem", async (o, a) => {
                Log($"Input: ${((string)o)}");
                Dictionary<string, JsonElement> Patch = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(((string)o));
                long PatchID = Patch["Itemcode"].GetInt64();
                Log($"EditCatalogueItem: ${Patch["Itemcode"]}");
                using (var ctx = new NewinvContext())
                {
                    var ToBePatched = ctx.Catalogues.Where(x => x.Itemcode == PatchID).First();
                    var Patched = ToBePatched.ApplyChangesExceptFilteredFromJson(["Itemcode"], (string)o);
                    Log($"EditCatalogueItem: Patching: Original: {JsonSerializer.Serialize(ToBePatched)}, Patch: {(string)o}, Patched: {JsonSerializer.Serialize(Patched)}");
                    ctx.Entry(ToBePatched).CurrentValues.SetValues(Patched);
                    await ctx.SaveChangesAsync();
                    var post = ctx.Catalogues.Where(e => e.Itemcode == PatchID).First();
                    Log($"EditCatalogueItem: Post-update: {JsonSerializer.Serialize(post)}");
                }
                return true;
            }, [], "Refresh");

            // 1. Create Catalogue Web
            app.AddAsyncEndpointWithBearerAuth<Catalogue, Catalogue>(
                "CreateCatalogueWeb",
                async (DataIn, LoginInfo) =>
                {
                    var cat = (Catalogue)DataIn;

                    // Force CLR defaults for ID and Dates
                    cat.Itemcode = 0;
                    cat.CreatedOn = DateTime.UtcNow;

                    // FIX: Ensure DescriptionPos and DescriptionWeb are not null (Set to 'same')
                    if (string.IsNullOrEmpty(cat.DescriptionPos))
                    {
                        cat.DescriptionPos = cat.Description;
                    }
                    if (string.IsNullOrEmpty(cat.DescriptionWeb))
                    {
                        cat.DescriptionWeb = cat.Description;
                    }

                    using (var ctx = new NewinvContext())
                    {
                        ctx.Catalogues.Add(cat);
                        await ctx.SaveChangesAsync();
                    }
                    return cat;
                },
                "Refresh"
            );

            // 2. Edit Catalogue Web (Patch)
            app.AddAsyncPatchEndpointWithBearerAuth<string, bool>(
                "EditCatalogueWeb",
                async (DataIn, LoginInfo) =>
                {
                    var Patch = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>((string)DataIn);
                    long PatchID = Patch["ItemCode"].GetInt64();

                    using (var ctx = new NewinvContext())
                    {
                        var ToBePatched = ctx.Catalogues.Where(x => x.Itemcode == PatchID).First();

                        // Explicitly define string array to avoid ambiguity
                        string[] removalKeys = new string[] { "CreatedOn" };

                        var Patched = ToBePatched.ApplyChangesExceptFilteredFromJson(
                            removalKeys,
                            (string)DataIn
                        );

                        // FIX: If Description is updated, ensure DescriptionPos/Web are synced if they weren't explicitly sent
                        if (Patch.ContainsKey("Description") && !Patch.ContainsKey("DescriptionPos"))
                        {
                            Patched.DescriptionPos = Patched.Description;
                        }
                        if (Patch.ContainsKey("Description") && !Patch.ContainsKey("DescriptionWeb"))
                        {
                            Patched.DescriptionWeb = Patched.Description;
                        }

                        ctx.Entry(ToBePatched).CurrentValues.SetValues(Patched);
                        await ctx.SaveChangesAsync();
                    }
                    return true;
                },
                new string[] { "CreatedOn" }, // Explicitly string[] here too
                "Refresh"
            );

            // 3. Search Catalogue Web
            app.AddAsyncEndpointWithBearerAuth<SearchRequest, List<CatalogueInventoryView>>(
                "SearchCatalogueWeb",
                async (DataIn, LoginInfo) =>
                {
                    var req = (SearchRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        var query = ctx.CatalogueInventoryViews.AsQueryable();

                        if (!string.IsNullOrWhiteSpace(req.Query))
                        {
                            var lowerQuery = req.Query.ToLower();
                            query = query.Where(c =>
                                (c.Description != null && c.Description.ToLower().Contains(lowerQuery)) ||
                                c.Itemcode.ToString().Contains(lowerQuery)
                            );
                        }

                        return await query
                            .OrderByDescending(c => c.CreatedOn)
                            .Take(100)
                            .ToListAsync();
                    }
                },
                "Refresh"
            );

            // 4. Suggest Catalogue Web (Similar Items)
            app.AddAsyncEndpointWithBearerAuth<SearchRequest, List<Catalogue>>(
                "SuggestCatalogueWeb",
                async (DataIn, LoginInfo) =>
                {
                    var req = (SearchRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        var query = ctx.Catalogues.AsQueryable();

                        if (!string.IsNullOrWhiteSpace(req.Query))
                        {
                            // Try to parse as ItemCode to exclude self
                            if (long.TryParse(req.Query, out long code))
                            {
                                query = query.Where(c => c.Itemcode != code);
                            }

                            // Simple similarity: Description contains the query string
                            var lowerQuery = req.Query.ToLower();
                            query = query.Where(c =>
                                c.Description != null && c.Description.ToLower().Contains(lowerQuery)
                            );
                        }

                        return await query
                            //.OrderByDescending(c => c.CreatedOn)
                            .Take(10)
                            .ToListAsync();
                    }
                },
                "Refresh"
            );

            // 5. Get Catalogue Item Web (By ID)
            app.AddAsyncEndpointWithBearerAuth<CatalogueGetRequest, Catalogue>(
                "GetCatalogueItemWeb",
                async (DataIn, LoginInfo) =>
                {
                    var req = (CatalogueGetRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        var item = await ctx.Catalogues
                            .FirstOrDefaultAsync(c => c.Itemcode == req.ItemCode);

                        if (item == null)
                        {
                            throw new ArgumentException($"Catalogue item with code {req.ItemCode} not found.");
                        }
                        return item;
                    }
                },
                "Refresh"
            );
            return app;
        }

    }
    // DTO for Search/Suggest
    public class SearchRequest
    {
        public string Query { get; set; }
    }
    // DTO for Get by ID
    public class CatalogueGetRequest
    {
        public long ItemCode { get; set; }
    }
    public class SearchCatalogueResult
    {
        public long Itemcode { get; set; }
        public string Description { get; set; }
    }
}


