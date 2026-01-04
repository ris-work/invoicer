using RV.InvNew.Common;
using System.Text.Json;
using MyAOTFriendlyExtensions;

namespace InvoicerBackend
{
    public static class CatalogueEditors
    {
        private static void Log(string a) { System.Console.WriteLine(a); }
        public static WebApplication AddCatalogueEditorHandlers(this WebApplication app)
        {
            app.AddAsyncEndpointWithBearerAuth<Catalogue>("CreateCatalogueItem", async (o, a) => {
                using (var ctx = new NewinvContext())
                {
                    var N = (Catalogue)o;
                    await ctx.Catalogues.AddAsync(N);
                    await ctx.SaveChangesAsync();
                }
                return true;
            });
            app.AddAsyncEndpointWithBearerAuth<string>("EditCatalogueItem", async (o, a) => {
                Log($"Input: ${(string)o}");
                Dictionary<string, JsonElement> Patch = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>((string)o);
                long PatchID = Patch["itemcode"].GetInt64();
                Log($"EditCatalogueItem: ${Patch["Itemcode"]}");
                using (var ctx = new NewinvContext())
                {
                    var ToBePatched = ctx.Catalogues.Where(x => x.Itemcode == PatchID).First();
                    var Patched = ToBePatched.ApplyChangesExceptFilteredFromJson(["itemcode"], (string)o);
                    Log($"EditCatalogueItem: Patching: Original: {JsonSerializer.Serialize(ToBePatched)}, Patch: {(string)o}, Patched: {Patched}");
                    ctx.Entry(ToBePatched).CurrentValues.SetValues(Patched);
                    await ctx.SaveChangesAsync();
                    var post = ctx.Catalogues.Where(e => e.Itemcode == PatchID).First();
                    Log($"EditCatalogueItem: Post-update: {JsonSerializer.Serialize(post)}");
                }
                return true;
            });
            return app;
        }

    }
}
