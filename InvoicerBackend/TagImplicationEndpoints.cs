using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;

namespace InvoicerBackend
{
    // --- Tag Implication Endpoints ---

    // 1. Add Implication
    public class TagImplicationDto
    {
        public string Tag { get; set; }
        public string Implies { get; set; }
        public string Description { get; set; }
    }

    // 3. Update
    public class EditTagImplicationDto
    {
        public long Id { get; set; }
        public string Tag { get; set; }
        public string Implies { get; set; }
        public string Description { get; set; }
    }

    // 5. Transitive Closure Viewer
    public class TransitiveRequest { public string Tag { get; set; } }


    public static class TagImplicationEndpoints
    {
        public static WebApplication AddTagImplicationsEndpoints(this WebApplication app)
        {
            app.AddAsyncEndpointWithBearerAuth<TagImplicationDto, TagsImply>(
                "AddTagImplication",
                async (DataIn, LoginInfo) =>
                {
                    var req = (TagImplicationDto)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        var entry = new TagsImply
                        {
                            Tag = req.Tag,
                            Implies = req.Implies,
                            Description = req.Description ?? "",
                            RecordedAt = DateTime.UtcNow,
                            CreatedBy = (long)LoginInfo.UserId
                        };
                        ctx.TagsImplies.Add(entry);
                        await ctx.SaveChangesAsync();
                        return entry;
                    }
                },
                "Refresh"
            );
            // 2. List All
            app.AddAsyncEndpointWithBearerAuth<object, List<TagsImply>>(
                "GetTagImplications",
                async (DataIn, LoginInfo) =>
                {
                    using (var ctx = new NewinvContext())
                    {
                        return await ctx.TagsImplies.OrderBy(t => t.Tag).ToListAsync();
                    }
                },
                "Refresh"
            );


            app.AddAsyncEndpointWithBearerAuth<EditTagImplicationDto, bool>(
                "EditTagImplication",
                async (DataIn, LoginInfo) =>
                {
                    var req = (EditTagImplicationDto)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        var entry = await ctx.TagsImplies.FirstOrDefaultAsync(t => t.Id == req.Id);
                        if (entry == null) throw new Exception("Not found");

                        entry.Tag = req.Tag;
                        entry.Implies = req.Implies;
                        entry.Description = req.Description ?? "";

                        await ctx.SaveChangesAsync();
                        return true;
                    }
                },
                "Refresh"
            );

            // 4. Delete
            app.AddAsyncEndpointWithBearerAuth<long, bool>(
                "DeleteTagImplication",
                async (DataIn, LoginInfo) =>
                {
                    long id = (long)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        var entry = await ctx.TagsImplies.FirstOrDefaultAsync(t => t.Id == id);
                        if (entry != null)
                        {
                            ctx.TagsImplies.Remove(entry);
                            await ctx.SaveChangesAsync();
                        }
                        return true;
                    }
                },
                "Refresh"
            );

            app.AddAsyncEndpointWithBearerAuth<TransitiveRequest, List<object>>(
                "GetTransitiveImplications",
                async (DataIn, LoginInfo) =>
                {
                    var req = (TransitiveRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        // Load all implications into memory (efficient for small tag sets)
                        var allRules = await ctx.TagsImplies.ToListAsync();

                        var results = new List<object>();
                        var visited = new HashSet<string>();

                        // Recursive function to build chain
                        void FindImplied(string currentTag, string path, int depth)
                        {
                            if (depth > 20) return; // Recursion limit safety

                            var directImplies = allRules.Where(r => r.Tag == currentTag).ToList();

                            foreach (var rule in directImplies)
                            {
                                var newPath = $"{path} -> {rule.Implies}";
                                results.Add(new
                                {
                                    RuleChain = newPath,
                                    ImpliedTag = rule.Implies,
                                    Depth = depth,
                                    RuleId = rule.Id
                                });

                                if (!visited.Contains(rule.Implies))
                                {
                                    visited.Add(rule.Implies);
                                    FindImplied(rule.Implies, newPath, depth + 1);
                                }
                            }
                        }

                        visited.Add(req.Tag);
                        FindImplied(req.Tag, req.Tag, 1);

                        return results;
                    }
                },
                "Refresh"
            );
            return app;
        }
    }
}
