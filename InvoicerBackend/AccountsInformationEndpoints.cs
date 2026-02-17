using Microsoft.EntityFrameworkCore;
using MyAOTFriendlyExtensions;
using RV.InvNew.Common;
using System.Text.Json;

namespace InvoicerBackend
{
    // 3. Search Accounts
    public class SearchAccountRequest { public string Query { get; set; } }
    // 4. Link Account to PII (Flow Step)
    public class LinkAccountPiiRequest
    {
        public long AccountNo { get; set; }
        public long PiiId { get; set; }
    }
    public static class AccountsInformationEndpoints
    {

        
        public static WebApplication AddAccountsInformationEndpoints(this WebApplication app)
        {
            // --- Accounts Endpoints ---

            // 1. Create Account
            app.AddAsyncEndpointWithBearerAuth<AccountsInformation>(
                "CreateAccountWeb",
                async (DataIn, LoginInfo) =>
                {
                    var acc = (AccountsInformation)DataIn;
                    acc.AccountNo = 0; // Force new ID
                    using (var ctx = new NewinvContext())
                    {
                        ctx.AccountsInformations.Add(acc);
                        await ctx.SaveChangesAsync();
                    }
                    return acc;
                },
                "Refresh"
            );

            // 2. Edit Account (Patch) - Filtered
            app.AddAsyncPatchEndpointWithBearerAuth<string>(
                "EditAccountWeb",
                async (DataIn, LoginInfo) =>
                {
                    var Patch = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>((string)DataIn);
                    long id = Patch["AccountNo"].GetInt64();

                    using (var ctx = new NewinvContext())
                    {
                        var entity = await ctx.AccountsInformations.FirstOrDefaultAsync(a => a.AccountNo == id);
                        if (entity == null) throw new ArgumentException("Account not found");

                        // Filter: Do not allow editing Objective Accounting fields like AccountNo, AccountType, AccountPii (use link flow)
                        // Allow: Limits, Discounts, Flags, Names
                        string[] removalKeys = new[] { "AccountNo", "AccountType", "AccountPii" };

                        var patched = entity.ApplyChangesExceptFilteredFromJson(removalKeys, (string)DataIn);
                        ctx.Entry(entity).CurrentValues.SetValues(patched);
                        await ctx.SaveChangesAsync();
                    }
                    return true;
                },
                new[] { "AccountNo", "AccountType", "AccountPii" },
                "Refresh"
            );

            app.AddAsyncEndpointWithBearerAuth<SearchAccountRequest>(
                "SearchAccountsWeb",
                async (DataIn, LoginInfo) =>
                {
                    var req = (SearchAccountRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        var q = ctx.AccountsInformations.AsQueryable();
                        if (!string.IsNullOrEmpty(req.Query))
                        {
                            q = q.Where(a => a.AccountName.ToLower().Contains(req.Query.ToLower()));
                        }
                        return await q.OrderByDescending(a => a.AccountNo).Take(50).ToListAsync();
                    }
                },
                "Refresh"
            );

            app.AddAsyncEndpointWithBearerAuth<LinkAccountPiiRequest>(
                "LinkAccountPiiWeb",
                async (DataIn, LoginInfo) =>
                {
                    var req = (LinkAccountPiiRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        var acc = await ctx.AccountsInformations.FirstOrDefaultAsync(a => a.AccountNo == req.AccountNo);
                        if (acc == null) throw new ArgumentException("Account not found");

                        acc.AccountPii = req.PiiId;
                        await ctx.SaveChangesAsync();
                        return true;
                    }
                },
                "Refresh"
            );

            return app;
        }
    }
}
