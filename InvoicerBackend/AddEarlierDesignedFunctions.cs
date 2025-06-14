﻿using System.Text.Json;
using RV.InvNew.Common;

namespace InvoicerBackend
{
    public static class AddEarlierDesignedFunctions
    {
        static bool IsTokenValid(LoginToken T, string AccessLevel)
        {
            using (var ctx = new NewinvContext())
            {
                if (T.TokenID != null && T.Token != null)
                    if (ctx.Tokens.Where(t => t.Tokenid == T.TokenID).First().Tokenvalue == T.Token)
                    {
                        return true;
                    }
                    else
                        return false;
                else
                    return false;
            }
        }

        public static void AddEarlierDesignedEndpoints(this WebApplication app)
        {
            var summaries = new[]
            {
                "Freezing",
                "Bracing",
                "Chilly",
                "Cool",
                "Mild",
                "Warm",
                "Balmy",
                "Hot",
                "Sweltering",
                "Scorching",
            };

            app.MapGet(
                    "/weatherforecast",
                    () =>
                    {
                        var forecast = Enumerable
                            .Range(1, 5)
                            .Select(index => new WeatherForecast(
                                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                                Random.Shared.Next(-20, 55),
                                summaries[Random.Shared.Next(summaries.Length)]
                            ))
                            .ToArray();
                        return forecast;
                    }
                )
                .WithName("GetWeatherForecast")
                .WithOpenApi();

            app.MapPost(
                    "/Login",
                    (LoginCredentials L) =>
                    {
                        using (var ctx = new NewinvContext())
                        {
                            if (L.User != null)
                            {
                                var UserE = ctx
                                    .Credentials.Where(e => e.Active && e.Username == L.User)
                                    .SingleOrDefault();
                                var UserA = ctx
                                    .UserAuthorizations.Where(e => e.Userid == UserE.Userid)
                                    .SingleOrDefault();
                                var UserPC = ctx
                                    .PermissionsListUsersCategories.Where(e =>
                                        e.Userid == UserE.Userid
                                    )
                                    .SingleOrDefault();
                                if (UserE != null)
                                {
                                    var Password = UserE.PasswordPbkdf2;
                                    if (Password != null && Utils.DoPBKDF2(L.Password) == Password)
                                    {
                                        Random rnd = new Random();
                                        Byte[] bTid = new byte[8];
                                        Byte[] bT = new byte[8];
                                        Byte[] bTs = new byte[8];
                                        rnd.NextBytes(bTid);
                                        rnd.NextBytes(bT);
                                        rnd.NextBytes(bTs);
                                        var Tid = Convert.ToBase64String(bTid);
                                        var T = Convert.ToBase64String(bT);
                                        var Ts = Convert.ToBase64String(bTs);
                                        if (UserA == null || UserPC == null || UserE == null)
                                            Console.Error.WriteLine(
                                                $"UserA:{UserA} UserPC:{UserPC} UserE:{UserE}"
                                            );
                                        ctx.Tokens.Add(
                                            new Token()
                                            {
                                                Tokenid = Tid,
                                                Tokensecret = Ts,
                                                Tokenvalue = T,
                                                Privileges = UserA.UserDefaultCap,
                                                CategoriesBitmask = UserPC.Categories,
                                                Userid = UserE.Userid,
                                            }
                                        );
                                        ctx.SaveChanges();
                                        return new LoginToken(Tid, T, Ts, "");
                                    }
                                    else
                                    {
                                        return new LoginToken(
                                            "",
                                            "",
                                            "",
                                            "Error: Wrong username, password or inactive user"
                                        );
                                    }
                                }
                                else
                                    return new LoginToken(
                                        "",
                                        "",
                                        "",
                                        "Error: Wrong username, password or inactive user"
                                    );
                            }
                            else
                            {
                                Console.WriteLine(L);
                                return new LoginToken("", "", "", "User null");
                            }
                        }
                    }
                )
                .WithName("Login")
                .WithOpenApi();

            app.MapPost(
                    "/ElevatedLogin",
                    (LoginCredentials L) =>
                    {
                        using (var ctx = new NewinvContext())
                        {
                            if (L.User != null)
                            {
                                var UserE = ctx
                                    .Credentials.Where(e => e.Active && e.Username == L.User)
                                    .SingleOrDefault();
                                var UserA = ctx
                                    .UserAuthorizations.Where(e => e.Userid == UserE.Userid)
                                    .SingleOrDefault();
                                var UserPC = ctx
                                    .PermissionsListUsersCategories.Where(e =>
                                        e.Userid == UserE.Userid
                                    )
                                    .SingleOrDefault();
                                if (UserE != null)
                                {
                                    var Password = UserE.PasswordPbkdf2;
                                    if (Password != null && Utils.DoPBKDF2(L.Password) == Password)
                                    {
                                        Random rnd = new Random();
                                        Byte[] bTid = new byte[8];
                                        Byte[] bT = new byte[8];
                                        Byte[] bTs = new byte[8];
                                        rnd.NextBytes(bTid);
                                        rnd.NextBytes(bT);
                                        rnd.NextBytes(bTs);
                                        var Tid = Convert.ToBase64String(bTid);
                                        var T = Convert.ToBase64String(bT);
                                        var Ts = Convert.ToBase64String(bTs);
                                        ctx.Tokens.Add(
                                            new Token()
                                            {
                                                Tokenid = Tid,
                                                Tokensecret = Ts,
                                                Tokenvalue = T,
                                                Privileges = UserA.UserCap,
                                                CategoriesBitmask = UserPC.Categories,
                                            }
                                        );
                                        ctx.SaveChanges();
                                        return new LoginToken(Tid, T, Ts, "");
                                    }
                                    else
                                    {
                                        return new LoginToken(
                                            "",
                                            "",
                                            "",
                                            "Error: Wrong username, password or inactive user"
                                        );
                                    }
                                }
                                else
                                    return new LoginToken(
                                        "",
                                        "",
                                        "",
                                        "Error: Wrong username, password or inactive user"
                                    );
                            }
                            else
                            {
                                Console.WriteLine(L);
                                return new LoginToken("", "", "", "User null");
                            }
                        }
                    }
                )
                .WithName("ElevatedLogin")
                .WithOpenApi();

            app.MapPost(
                    "/GetItemsUnrestricted",
                    (LoginToken L) =>
                    {
                        if (!IsTokenValid(L, "ALL"))
                        {
                            return "Error: Error";
                        }
                        else
                        {
                            return "Hello";
                        }
                    }
                )
                .WithName("GetItemsUnrestricted")
                .WithOpenApi();

            app.MapPost(
                    "/PermTimeTest",
                    (AuthenticatedRequest<string> AS) =>
                    {
                        if (AS.Get("View_Server_Time") != null)
                            return new SingleValueString
                            {
                                response = DateTime.UtcNow.ToString("O"),
                            };
                        else
                            throw new UnauthorizedAccessException();
                    }
                )
                .WithName("PermTimeTest");

            app.MapPost(
                    "/PosRefresh",
                    (AuthenticatedRequest<string> AS) =>
                    {
                        if (AS.Get("Refresh", "/PosRefresh") != null)
                        {
                            List<PosCatalogue> PC;
                            List<PosBatch> PB;
                            List<VatCategory> VC;
                            using (var ctx = new NewinvContext())
                            {
                                //ctx.Database.lo
                                long TokenCategoriesBitmask = ctx
                                    .Tokens.Where(e => e.Tokenid == AS.Token.TokenID)
                                    .Select(e => e.CategoriesBitmask)
                                    .First();
                                PC = ctx
                                    .Catalogues.Where(e =>
                                        (e.CategoriesBitmask & TokenCategoriesBitmask) > 0
                                    )
                                    .Select(e => new PosCatalogue
                                    {
                                        itemcode = e.Itemcode,
                                        EnforceAboveCost = e.EnforceAboveCost,
                                        itemdesc = e.DescriptionPos,
                                        ManualPrice = e.PriceManual,
                                        VatCategoryAdjustable = e.VatCategoryAdjustable,
                                        VatDependsOnUser = e.VatDependsOnUser,
                                        DefaultVatCategory = e.DefaultVatCategory,
                                    })
                                    .ToList();
                                PB = ctx
                                    .Inventories.Select(
                                        (e) =>
                                            new PosBatch
                                            {
                                                itemcode = e.Itemcode,
                                                batchcode = e.Batchcode,
                                                selling = e.SellingPrice,
                                                marked = e.MarkedPrice,
                                                expireson = DateOnly.FromDateTime(
                                                    e.ExpDate.GetValueOrDefault(DateTime.MaxValue)
                                                ),
                                                SIH = e.Units,
                                            }
                                    )
                                    .ToList();
                                VC = ctx.VatCategories.ToList();
                            }
                            Console.WriteLine(
                                $"Sending PC: {PC.Count()} PB: {PB.Count} VC: {VC.Count}"
                            );
                            var JSO = new JsonSerializerOptions { IncludeFields = true };
                            //Console.WriteLine(JsonSerializer.Serialize(new PosRefresh() { VatCategories = VC, Batches = PB, Catalogue = PC }, JSO));
                            return new PosRefresh()
                            {
                                VatCategories = VC,
                                Batches = PB,
                                Catalogue = PC,
                            };
                        }
                        else
                        {
                            Console.WriteLine("Unauthorized");
                            throw new UnauthorizedAccessException();
                        }
                        ;
                    }
                )
                .WithName("PosRefresh")
                .WithOpenApi();

            app.MapPost(
                    "/AuthenticatedEcho",
                    (AuthenticatedRequest<string> AS) =>
                    {
                        return AS.Get();
                    }
                )
                .WithName("AuthenticatedEcho")
                .WithOpenApi();

            app.MapPost(
                    "/NewAccount",
                    (AuthenticatedRequest<Account> AAcc) =>
                    {
                        using (var ctx = new NewinvContext())
                        {
                            Account Acc = AAcc.Get();
                            ctx.AccountsInformations.Add(
                                new AccountsInformation
                                {
                                    AccountName = Acc.AccountName,
                                    AccountType = Acc.AccountType,
                                    AccountNo =
                                        ctx.AccountsInformations.Where(a =>
                                                a.AccountType == Acc.AccountType
                                            )
                                            .Select(a => a.AccountNo)
                                            .Max() + 1,
                                }
                            );
                            ctx.SaveChanges();
                        }
                    }
                )
                .WithName("NewAccount")
                .WithOpenApi();
            app.AddEndpointWithBearerAuth<string>(
                "PosRefreshBearerAuth",
                (AS, LoginInfo) =>
                {
                    List<PosCatalogue> PC;
                    List<PosBatch> PB;
                    List<VatCategory> VC;
                    using (var ctx = new NewinvContext())
                    {
                        //ctx.Database.lo
                        long TokenCategoriesBitmask = ctx
                            .Tokens.Where(e => e.Tokenid == LoginInfo.TokenId)
                            .Select(e => e.CategoriesBitmask)
                            .First();
                        PC = ctx
                            .Catalogues.Where(e =>
                                (e.CategoriesBitmask & TokenCategoriesBitmask) > 0
                            )
                            .Select(e => new PosCatalogue
                            {
                                itemcode = e.Itemcode,
                                EnforceAboveCost = e.EnforceAboveCost,
                                itemdesc = e.DescriptionPos,
                                ManualPrice = e.PriceManual,
                                VatCategoryAdjustable = e.VatCategoryAdjustable,
                                VatDependsOnUser = e.VatDependsOnUser,
                                DefaultVatCategory = e.DefaultVatCategory,
                            })
                            .ToList();
                        PB = ctx
                            .Inventories.Select(
                                (e) =>
                                    new PosBatch
                                    {
                                        itemcode = e.Itemcode,
                                        batchcode = e.Batchcode,
                                        selling = e.SellingPrice,
                                        marked = e.MarkedPrice,
                                        expireson = DateOnly.FromDateTime(
                                            e.ExpDate.GetValueOrDefault(DateTime.MaxValue)
                                        ),
                                        SIH = e.Units,
                                    }
                            )
                            .ToList();
                        VC = ctx.VatCategories.ToList();
                    }
                    Console.WriteLine($"Sending PC: {PC.Count()} PB: {PB.Count} VC: {VC.Count}");
                    var JSO = new JsonSerializerOptions { IncludeFields = true };
                    //Console.WriteLine(JsonSerializer.Serialize(new PosRefresh() { VatCategories = VC, Batches = PB, Catalogue = PC }, JSO));
                    return new PosRefresh()
                    {
                        VatCategories = VC,
                        Batches = PB,
                        Catalogue = PC,
                    };
                    ;
                },
                "Refresh"
            );
        }
    }
}
