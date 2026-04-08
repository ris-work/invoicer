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
using Microsoft.Extensions.FileProviders;
using MyAOTFriendlyExtensions;
using RV.InvNew.Common;
using Tomlyn.Syntax;

Console.WriteLine("CWD: {0}", Directory.GetCurrentDirectory());
Console.WriteLine("[common] CWD: {0}", Config.GetCWD());
Config.Initialize();

string? WebUIHttpsCertPath = null; ; // Path to .pfx file
string? WebUIHttpsCertPassword = null; // Password for the .pfx
int WebUIPort = 5062;
int WebUIPortTLS = 5001;
int WebUIPortMTLS = 5002;
string WebUIAddr = "127.0.0.1";
string WebUIAddrInsecure = "127.0.0.1";

if (Config.modelDict.ContainsKey("WebUIAddressInsecure"))
{
    WebUIAddrInsecure = (((string)((string)Config.modelDict["WebUIAddressInsecure"])));
}

if (Config.modelDict.ContainsKey("WebUIAddress"))
{
    WebUIAddr = (((string)((string)Config.modelDict["WebUIAddress"])));
}

if (Config.modelDict.ContainsKey("WebUIPort"))
{
    WebUIPortTLS = (((int)((long)Config.modelDict["WebUIPort"])));
}

if (Config.modelDict.ContainsKey("WebUIPortTLS"))
{
    WebUIPortTLS = (((int)((long)Config.modelDict["WebUIPortTLS"])));
}

if (Config.modelDict.ContainsKey("WebUIPortMTLS"))
{
    WebUIPortMTLS = (((int)((long)Config.modelDict["WebUIPortMTLS"])));
}

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

builder.WebHost.ConfigureKestrel(options =>
{
    // Pre-calculate ports
    int httpPort = WebUIPort;
    int httpsPort = WebUIPortTLS; // Optional mTLS
    int mtlsPort = WebUIPortMTLS; // Forced mTLS (Prompt)
                                  // Always enabled. Uses custom cert if provided, otherwise generates self-signed.
                                  //int httpsPort = Config.WebUIPort + 1;
    Console.WriteLine($"Listening: Insecure: {WebUIAddrInsecure}:{WebUIPort} Secure: {WebUIAddr}:{WebUIPortTLS} MTLS: {WebUIAddr}:{WebUIPortMTLS}");
    System.Security.Cryptography.X509Certificates.X509Certificate2 serverCert;

    if (!string.IsNullOrEmpty(WebUIHttpsCertPath) && File.Exists(WebUIHttpsCertPath))
    {
        serverCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(WebUIHttpsCertPath, WebUIHttpsCertPassword);
        Console.WriteLine($"[SSL] Loaded HTTPS certificate from: {WebUIHttpsCertPath}");
    }
    else
    {
        // Auto-generate self-signed cert
        // DO NOT use 'using' here, or if you do, export the key.
        // We use a temporary RSA key to create the cert, then export/import so the cert owns its own key.
        using (var rsa = System.Security.Cryptography.RSA.Create(2048))
        {
            var req = new System.Security.Cryptography.X509Certificates.CertificateRequest("CN=HealthMonitorLocal", rsa, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);

            // Key Usage
            req.CertificateExtensions.Add(new System.Security.Cryptography.X509Certificates.X509KeyUsageExtension(System.Security.Cryptography.X509Certificates.X509KeyUsageFlags.DigitalSignature | System.Security.Cryptography.X509Certificates.X509KeyUsageFlags.KeyEncipherment, false));

            // Extended Key Usage (Server Authentication)
            req.CertificateExtensions.Add(new System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension(
                new System.Security.Cryptography.OidCollection {
                    new System.Security.Cryptography.Oid("1.3.6.1.5.5.7.3.1")
                }, false));

            // SAN (Correct way)
            var sanBuilder = new System.Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName("localhost");
            sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback); // Add 127.0.0.1 explicitly
            req.CertificateExtensions.Add(sanBuilder.Build());

            var tempCert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(5));

            // FIX: Export to PFX and re-import so the private key is persisted in the X509Certificate2 object
            // and not tied to the 'rsa' variable which is about to be disposed.
            serverCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(tempCert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx));
        }
        Console.WriteLine($"[SSL] Generated self-signed certificate for port {httpsPort}");
    }

    Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> httpsConfigure = listenOptions =>
    {
        listenOptions.UseHttps(serverCert, (e) => { e.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.DelayCertificate; });
        // Enable Client Certificate reception for mTLS endpoints
    };

    // Helper to get ListenOptions based on address type
    Action<int, Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions>> listen = (port, configure) =>
    {
        if (WebUIAddr == "localhost" || WebUIAddr == "127.0.0.1")
            options.ListenLocalhost(port, configure);
        else
            options.Listen(System.Net.IPAddress.Parse(WebUIAddr), port, configure);
    };
    Action<int, Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions>> listenInsecure = (port, configure) =>
    {
        if (WebUIAddrInsecure == "localhost" || WebUIAddrInsecure == "127.0.0.1")
            options.ListenLocalhost(port, configure);
        else
            options.Listen(System.Net.IPAddress.Parse(WebUIAddrInsecure), port, configure);
    };

    // --- Listener 1: HTTP ---
    listenInsecure(httpPort, opt => { /* No config needed */ });

    // --- Listener 2: HTTPS (Optional Cert) ---
    listen(httpsPort, opt => {
        opt.UseHttps(serverCert, (o) => {
            o.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.AllowCertificate;
            o.ClientCertificateValidation = (cert, chain, policy) => true;
        });
    });

    // --- Listener 3: HTTPS (Forced mTLS Prompt) ---
    listen(mtlsPort, opt => {
        opt.UseHttps(serverCert, (o) => {
            o.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.RequireCertificate;
            o.ClientCertificateValidation = (cert, chain, policy) => true;
        });
    });
});
//builder.Services.AddOpenApi(o => { o.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1; });
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen( o => { o.SwaggerDoc("v3", new Microsoft.OpenApi.OpenApiInfo { Title = "RVPos", Version = "v3" }); } );

builder.Services.AddHttpLogging(o =>
{
    //o.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
});
builder.Services.AddHttpLogging();
builder.Services.AddRazorPages();
builder.Services.AddOpenApi();

var app = builder.Build();

app.Use(async (context, next) =>
{
    bool IsPubKeyAuthenticated = false;
    string username = "Anonymous";

    // --- PRIORITY 1: Client Certificate Authentication ---
    var clientCert = context.Connection.ClientCertificate;
    if (clientCert != null)
    {
        string fp = clientCert.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256);
        Console.WriteLine($"[Auth] Incoming Cert FP: {fp}");

        using (var ctx = new NewinvContext())
        {
            var key = ctx.AllowedKeys.FirstOrDefault(k => k.FingerprintSha256 == fp && k.IsActive == true && k.ValidUntil > DateTime.Now);
            if (key != null)
            {
                IsPubKeyAuthenticated = true;
                username = key.Name;
                Console.WriteLine($"[Auth] Cert Auth Success: {username} ({fp.Substring(0, 8)}...)");
            }
            else
            {
                // CRITICAL: If a cert is presented but is INVALID/INACTIVE, we stop immediately.
                // We do NOT fallback to Anonymous or Basic Auth. 
                Console.WriteLine($"[Auth] Cert Auth Failed: Inactive/Unknown FP {fp.Substring(0, 8)}...");

                context.Response.StatusCode = 403; // Forbidden
                await context.Response.WriteAsync("Certificate Invalid or Inactive.");
                return; // Stop pipeline
            }
        }
    }

    // Store state in HttpContext.Items (simple dictionary)
    context.Items["IsPubKeyAuthenticated"] = IsPubKeyAuthenticated;
    context.Items["ClientCertFingerprint"] = context.Connection.ClientCertificate;
    context.Items["Username"] = username;

    await next();
});

// 1. Define the path to your 'static' folder
var staticFilePath = Path.Combine(builder.Environment.ContentRootPath, "Pages","static");
app.UseResponseCompression();
app.MapOpenApi();
//RV.InvNew.Common.TranscriptionService.TestSampleTranscribe().GetAwaiter().GetResult();
using (var ctx = new NewinvContext())
{
    LoyaltyPointsManager.TestLoyaltyPoints();
}
//SalesProcessor.TestAllBranchesApplyDiscountsAndSurcharges();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger(o => { o.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0; });
    //app.UseSwaggerUI(o => { o.SwaggerEndpoint("/openapi/v1.json", "OpenAPI V1"); });
    app.UseHttpLogging();
}
app.UseSwaggerUI(options => {
    options.SwaggerEndpoint("../openapi/v1.json", "v1");
    options.ConfigObject.Urls = new[] {
            new Swashbuckle.AspNetCore.SwaggerUI.UrlDescriptor {
                Url = "../openapi/v1.json",
                Name = "V1 Docs"
            }
        };
});




app.AddEarlierDesignedEndpoints();
app.AddNotificationsHandler();
app.MapPost(
        "/NewJournalEntry",
        (AuthenticatedRequest<AccountsJournalEntry> AJE) =>
        {
            using (var ctx = new NewinvContext() { })
            {
                AccountsJournalEntry AccJE = AJE.Get();
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

app.AddEndpoint<string, object>(
    "AutogeneratedClockEndpoint",
    (R) =>
    {
        return new SingleValueString { response = DateTime.UtcNow.ToString("O") };
    },
    "VIEW_SERVER_TIME"
);

app.AddEndpointWithBearerAuth<string, object>(
    "AutogeneratedClockEndpointBearerAuth",
    (R) =>
    {
        return new SingleValueString { response = DateTime.UtcNow.ToString("O") };
    },
    "VIEW_SERVER_TIME"
);

app.AddEndpointWithBearerAuth<long, List<Catalogue>>(
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

app.AddEndpointWithBearerAuth<Catalogue, Catalogue>(
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

app.AddEndpointWithBearerAuth<object, object>(
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

app.AddEndpointWithBearerAuth<string, string[]>(
    "GetMyDenyList",
    (AS, LoginInfo) =>
    {
        string[] DeniedList;
        using (var ctx = new NewinvContext())
        {
            DeniedList = ctx
                .UsersFieldLevelAccessControlsDenyLists.Where(e => e.UserId == LoginInfo.UserId)
                .Select(e => e.DeniedField)
                .ToArray();
        }
        return DeniedList;
    },
    "Refresh"
);
app.AddEndpointWithBearerAuth<string, string[]>(
    "GetUniversalDenyList",
    (AS, LoginInfo) =>
    {
        string[] DeniedList;
        using (var ctx = new NewinvContext())
        {
            DeniedList = ctx
                .UsersFieldLevelAccessControlsDenyLists.Where(e => e.UserId == LoginInfo.UserId)
                .Select(e => e.DeniedField)
                .ToArray();
        }
        return DeniedList;
    },
    "Refresh"
);

app.AddEndpointWithBearerAuth<string, List<Inventory>>(
    "BatchRead",
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

app.AddEndpointWithBearerAuth<Inventory, List<Inventory>>(
    "BatchEdit",
    (AS, LoginInfo) =>
    {
        var Batch = (Inventory)AS;
        var SafeBatch = Batch.RemoveField("Itemcode").RemoveField("Batchcode");

        using (var ctx = new NewinvContext())
        {
            var BatchCurrent = ctx
                .Inventories.Where(e =>
                    e.Itemcode == Batch.Itemcode && e.Batchcode == Batch.Itemcode
                )
                .First();
            BatchCurrent.ApplyChangesFromFiltered([], JsonSerializer.Serialize(Batch));
            ctx.SaveChanges();
            Batch = BatchCurrent;
        }
        return Batch;
    },
    "Refresh"
);
app.UseRouting();
//app.AddCatalogueDefaultImageEndpoints();
app.AddCatalogueEditorHandlers();
app.AddJournalEndpoints();
app.AddAnalyticsEndpoints();
app.AddBackOfficeAccountingEndpoints();
app.AddCycleCountEndpoints();
app.AddRequestsEndpoints();
app.AddPhysicalMapEndpoints();
app.AddRefDocsEndpoints();
app.AddCatalogueImageEndpoints();
app.AddBatchDefaultImageEndpoints();
app.AddBatchEditors();
app.AddFlowEndpoints();
app.AddPiiEndpoints();
app.AddAccountsInformationEndpoints();
app.AddSchedulerEndpoints();
app.AddTagImplicationsEndpoints();
app.AddSuggestedPriceEndpoints();
app.AddEndpointsDefinitionEndpoint();
app.AddSalesSimulationEndpoints();
app.AddPrivilegeUtilities();
app.AddInvoicePersistenceEndpoints();

app.AddInventoryAdjustmentsEndpoints();
app.AddPurchaseEndpoints();


app.AddDiagnosticEndpoints();
// 2. Configure StaticFiles to use this folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(staticFilePath),
    RequestPath = "/Pages/static" // This defines the URL prefix
});



app.MapRazorPages();

System.Console.WriteLine("Done setting up!");
app.Run();



internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
