using System;
using System.IO.Compression;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Transactions;
using InvoicerBackend;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using MyAOTFriendlyExtensions;
using RV.InvNew.Common;
using Tomlyn.Syntax;

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

Console.WriteLine("CWD: {0}", Directory.GetCurrentDirectory());
Console.WriteLine("[common] CWD: {0}", Config.GetCWD());
Config.Initialize();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpLogging(o =>
{
    //o.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
});
builder.Services.AddHttpLogging();

var app = builder.Build();
app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpLogging();
}

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
                        .PermissionsListUsersCategories.Where(e => e.Userid == UserE.Userid)
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
                        .PermissionsListUsersCategories.Where(e => e.Userid == UserE.Userid)
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
                return new SingleValueString { response = DateTime.UtcNow.ToString("O") };
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
                        .Catalogues.Where(e => (e.CategoriesBitmask & TokenCategoriesBitmask) > 0)
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
                            ctx.AccountsInformations.Where(a => a.AccountType == Acc.AccountType)
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

app.AddNotificationsHandler();
app.MapPost(
        "/NewJournalEntry",
        (AuthenticatedRequest<JournalEntry> AJE) =>
        {
            using (var ctx = new NewinvContext() { })
            {
                JournalEntry AccJE = AJE.Get();
                using var Transaction = ctx.Database.BeginTransaction(
                    System.Data.IsolationLevel.Serializable
                );
                InvoicerBackend.JournalEntries.AddJournalEntry(ctx, AccJE);
                ctx.SaveChanges();
                Transaction.Commit();
            }
        }
    )
    .WithName("NewJournalEntry")
    .WithOpenApi();

app.AddEndpoint<string>(
    "AutogeneratedClockEndpoint",
    (R) =>
    {
        return new SingleValueString { response = DateTime.UtcNow.ToString("O") };
    },
    "VIEW_SERVER_TIME"
);

app.AddEndpointWithBearerAuth<string>(
    "AutogeneratedClockEndpointBearerAuth",
    (R) =>
    {
        return new SingleValueString { response = DateTime.UtcNow.ToString("O") };
    },
    "VIEW_SERVER_TIME"
);

app.AddEndpointWithBearerAuth<long>(
    "CatalogueRead",
    (R) =>
    {
        Console.WriteLine($"==== REQUESTED CATALOGUE ID {R}");
        //var SafeR = ((Catalogue)R).RemoveField("Id");
        Catalogue A;
        using (var ctx = new NewinvContext())
        {
            A = ctx.Catalogues.Where(a => a.Itemcode == (long)R).First();
        }
        Console.WriteLine(
            $"CATALOGUE: Got: {A.Itemcode}, {A.Description}, {JsonSerializer.Serialize(A)}"
        );

        return A;
    },
    "Refresh"
);

app.AddEndpointWithBearerAuth<Catalogue>(
    "CatalogueEdit",
    (R) =>
    {
        Console.WriteLine($"==== REQUESTED EDIT CATALOGUE ID {R}");
        //var SafeR = ((Catalogue)R).RemoveField("Id");

        Catalogue SafeR = (Catalogue)R.RemoveField("Itemcode");
        Catalogue A;
        
        using (var ctx = new NewinvContext())
        {
            A = ctx.Catalogues.Where(a => a.Itemcode == SafeR.Itemcode).First();
            A.ApplyChangesFromFiltered([], JsonSerializer.Serialize(SafeR));
            ctx.SaveChanges();
        }
        Console.WriteLine(
            $"CATALOGUE: Got: {A.Itemcode}, {A.Description}, {JsonSerializer.Serialize(A)}"
        );

        return A;
    },
    "Refresh"
);

app.AddEndpointWithBearerAuth<object>(
    "CatalogueAdd",
    (R) =>
    {
        var SafeR = ((Catalogue)R).RemoveField("Itemcode");
        using (var ctx = new NewinvContext())
        {
            ctx.Catalogues.Add(SafeR);
            ctx.SaveChanges();
        }

        return Results.Accepted();
    },
    "Refresh"
);

app.AddEndpointWithBearerAuth<string>(
    "GetMyDenyList",
    (AS, LoginInfo) => {
        string [] DeniedList;
        using (var ctx = new NewinvContext())
        {
            DeniedList = ctx.UsersFieldLevelAccessControlsDenyLists.Where(e => e.UserId == LoginInfo.UserId).Select(e => e.DeniedField).ToArray();
        }
        return DeniedList;
    },
    "Refresh"
    );

app.AddEndpointWithBearerAuth<string>("BatchRead",
    (AS, LoginInfo) =>
    {
        List<Inventory> Batches;
        using (var ctx = new NewinvContext())
        {
            Batches = ctx.Inventories.Where(e => e.Itemcode == long.Parse((string)AS)).ToList();
        }
        return Batches;
    },
    "Refresh"
    );

app.AddEndpointWithBearerAuth<Inventory>("BatchEdit",
    (AS, LoginInfo) =>
    {
        var Batch = (Inventory)AS;
        var SafeBatch = Batch.RemoveField("Itemcode").RemoveField("Batchcode");
        
        using (var ctx = new NewinvContext())
        {
            var BatchCurrent = ctx.Inventories.Where(e => e.Itemcode == Batch.Itemcode && e.Batchcode == Batch.Itemcode).First();
            BatchCurrent.ApplyChangesFromFiltered([], JsonSerializer.Serialize(Batch));
            ctx.SaveChanges();
            Batch = BatchCurrent;
        }
        return Batch;
    },
    "Refresh"
    );

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
                .Catalogues.Where(e => (e.CategoriesBitmask & TokenCategoriesBitmask) > 0)
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

System.Console.WriteLine("Done setting up!");
app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
