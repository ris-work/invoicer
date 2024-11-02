using RV.InvNew.Common;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Text.Json;

static bool IsTokenValid(LoginToken T, string AccessLevel)
{
    using (var ctx = new NewinvContext())
    {
        if (T.TokenID != null && T.Token != null)
            if (ctx.Tokens.Where(t => t.Tokenid == T.TokenID).First().Tokenvalue == T.Token)
            {
                return true;
            }
            else return false;
        else return false;
    }

}

Console.WriteLine("CWD: {0}", Directory.GetCurrentDirectory());
Console.WriteLine("[common] CWD: {0}", Config.GetCWD() );
Config.Initialize();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpLogging(o => {
    o.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpLogging();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapPost("/Login", (LoginCredentials L) =>
{
    using (var ctx = new NewinvContext())
    {
        if (L.User != null)
        {
            var UserE = ctx.Credentials.Where(e => e.Active && e.Username == L.User).SingleOrDefault();
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
                    ctx.Tokens.Add(new Token() { Tokenid = Tid, Tokensecret = Ts, Tokenvalue = T });
                    ctx.SaveChanges();
                    return new LoginToken(Tid, T, Ts, "");

                }
                else
                {
                    return new LoginToken("", "", "", "Error: Wrong username, password or inactive user");
                }
            }
            else return new LoginToken("", "", "", "Error: Wrong username, password or inactive user");
        }
        else { Console.WriteLine(L); return new LoginToken("", "", "", "User null"); }
    }
}).WithName("Login")
.WithOpenApi();

app.MapPost("/GetItemsUnrestricted", (LoginToken L) =>
{
    if (!IsTokenValid(L, "ALL")){
        return "Error: Error";
    }
    else
    {
        return "Hello";
    }
}).WithName("GetItemsUnrestricted")
.WithOpenApi();

app.MapPost("/PosRefresh", (AuthenticatedRequest<string> AS) =>
{
    if (AS.Get() != null)
    {
        List<PosCatalogue> PC;
        List<PosBatch> PB;
        List<VatCategory> VC;
        using (var ctx = new NewinvContext())
        {
            PC = ctx.Catalogues.Select(e => new PosCatalogue
            {
                itemcode = e.Itemcode,
                EnforceAboveCost = e.EnforceAboveCost,
                itemdesc = e.DescriptionPos,
                ManualPrice = e.PriceManual,
                VatCategoryAdjustable = e.VatCategoryAdjustable,
                VatDependsOnUser = e.VatDependsOnUser,
                DefaultVatCategory = e.DefaultVatCategory
                
            }).ToList();
            PB = ctx.Inventories.Select((e) => new PosBatch
            {
                itemcode = e.Itemcode,
                batchcode = e.Batchcode,
                selling = e.SellingPrice,
                marked = e.MarkedPrice,
                expireson = DateOnly.FromDateTime(e.ExpDate.GetValueOrDefault(DateTime.MaxValue)),
                SIH = e.Units,
            }).ToList();
            VC = ctx.VatCategories.ToList();

        }
        Console.WriteLine($"Sending PC: {PC.Count()} PB: {PB.Count} VC: {VC.Count}");
        var JSO = new JsonSerializerOptions { IncludeFields = true };
        //Console.WriteLine(JsonSerializer.Serialize(new PosRefresh() { VatCategories = VC, Batches = PB, Catalogue = PC }, JSO));
        return new PosRefresh() { VatCategories = VC, Batches = PB, Catalogue = PC };

    }
    else {
        Console.WriteLine("Unauthorized");
        throw new UnauthorizedAccessException();

    };
}).WithName("PosRefresh")
.WithOpenApi();

app.MapPost("/AuthenticatedEcho", (AuthenticatedRequest<string> AS) =>
{
    return AS.Get();
})
    .WithName("AuthenticatedEcho")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


