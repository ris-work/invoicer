// See https://aka.ms/new-console-template for more information
using System.Buffers;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using HealthMonitor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tomlyn;
using Tomlyn.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Transport;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices;

//SQLitePCL.Batteries_V2.Init();
/*if (!OperatingSystem.IsWindows()) SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
else SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());*/

void StartPing(string dest)
{
    while (true)
    {
        try
        {
            using (var ctx = new LogsContext())
            {
                if (Config.AutoVacuum)
                {
                    try
                    {
                        ctx.Database.ExecuteSqlRaw($"PRAGMA auto_vacuum=FULL;");
                    }
                    catch (System.Exception E)
                    {
                        Console.WriteLine($"Error while VACUUM/ANALYZE: {E.ToString()}");
                    }
                }
                var pingSender = new System.Net.NetworkInformation.Ping();
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaabbbbbbbbbb";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                PingReply reply = pingSender.Send(dest, 4000, buffer);
                Console.WriteLine(
                    "{0} {1} {2}",
                    dest,
                    reply.RoundtripTime,
                    reply.Buffer.SequenceEqual(buffer)
                );
                ctx.Pings.Add(
                    new HealthMonitor.Ping
                    {
                        WasItOkNotCorrupt = reply.Buffer.SequenceEqual(buffer) ? 1 : 0,
                        DidItSucceed = reply.Status == IPStatus.Success ? 1 : 0,
                        Dest = dest,
                        Latency = (int)reply.RoundtripTime,
                        TimeNow = DateTime.UtcNow.ToString("o"),
                    }
                );
                ctx.SaveChanges();
            }
        }
        catch (Win32Exception E) {
            if (Config.Verbose) Console.WriteLine(E.ToString());
        }
        catch (System.Exception E)
        {
            if (Config.Verbose) Console.WriteLine(E.ToString());
        }
        Thread.Sleep(Config.SleepTimeMsBetweenPointsPing);
    }
}

void StartServer()
{
    var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(new WebApplicationOptions
    {
        //ContentRootPath = Directory.GetCurrentDirectory(),
        ContentRootPath = AppContext.BaseDirectory,
        Args = Array.Empty<string>()
    });


    // Configure the server address
    string url = $"http://{Config.WebUIAddress}:{Config.WebUIPort}";
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Pre-calculate ports
        int httpPort = Config.WebUIPort;
        int httpsPort = Config.WebUIPort + 1; // Optional mTLS
        int mtlsPort = Config.WebUIPort + 2; // Forced mTLS (Prompt)
        // Always enabled. Uses custom cert if provided, otherwise generates self-signed.
        //int httpsPort = Config.WebUIPort + 1;
        System.Security.Cryptography.X509Certificates.X509Certificate2 serverCert;

        if (!string.IsNullOrEmpty(Config.WebUIHttpsCertPath) && File.Exists(Config.WebUIHttpsCertPath))
        {
            serverCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(Config.WebUIHttpsCertPath, Config.WebUIHttpsCertPassword);
            Console.WriteLine($"[SSL] Loaded HTTPS certificate from: {Config.WebUIHttpsCertPath}");
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
            listenOptions.UseHttps(serverCert, (e) => {e.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.DelayCertificate; });
            // Enable Client Certificate reception for mTLS endpoints
        };

        // Helper to get ListenOptions based on address type
        Action<int, Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions>> listen = (port, configure) =>
        {
            if (Config.WebUIAddress == "localhost" || Config.WebUIAddress == "127.0.0.1")
                options.ListenLocalhost(port, configure);
            else
                options.Listen(System.Net.IPAddress.Parse(Config.WebUIAddress), port, configure);
        };

        // --- Listener 1: HTTP ---
        listen(httpPort, opt => { /* No config needed */ });

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

    // Add Services
    builder.Services.AddRazorPages().AddRazorRuntimeCompilation(options =>
    {
        options.FileProviders.Clear();
        options.FileProviders.Add(new PhysicalFileProvider(AppContext.BaseDirectory));
        options.FileProviders.Add(new EmbeddedFileProvider(typeof(Program).Assembly));
    });
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
    });

    var app = builder.Build();

    // Configure HTTP Pipeline
    app.UseRouting();
    var StaticFilePath = Path.Combine(builder.Environment.ContentRootPath, "Pages", "static");
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(StaticFilePath), RequestPath = "/static" });
    
    app.UseResponseCompression();
    app.UseStaticFiles(); // For d3.js in wwwroot/static
    app.MapRazorPages();
    Console.WriteLine($"[App] Starting from: {AppContext.BaseDirectory}");


    GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);
    GC.WaitForPendingFinalizers();

    // --- 1. Ad-Hoc Auth Middleware ---
    app.Use(async (context, next) =>
    {
        // Parse Authorization Header manually
        string authHeader = context.Request.Headers["Authorization"];
        bool isAuthenticated = false;
        string username = "Anonymous";

        // --- PRIORITY 1: Client Certificate Authentication ---
        var clientCert = context.Connection.ClientCertificate;
        if (clientCert != null)
        {
            string fp = clientCert.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256);
            Console.WriteLine($"[Auth] Incoming Cert FP: {fp}");

            using (var ctx = new LogsContext())
            {
                var key = ctx.AllowedKeys.FirstOrDefault(k => k.Sha256Fingerprint == fp && k.IsActive == 1);
                if (key != null)
                {
                    isAuthenticated = true;
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

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic "))
        {
            var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
            try
            {
                var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                var parts = decodedCredentials.Split(':', 2);

                // Simple string check against Config
                if (parts.Length == 2 && parts[0] == Config.AuthUser && parts[1] == Config.AuthPass)
                {
                    isAuthenticated = true;
                    username = parts[0];
                }
            }
            catch { /* Ignore decoding errors */ }
        }

        // Store state in HttpContext.Items (simple dictionary)
        context.Items["IsAuthenticated"] = isAuthenticated;
        context.Items["Username"] = username;

        // Enforce Mandatory Mode
        if (!isAuthenticated && Config.AuthMode == "Mandatory")
        {
            context.Response.StatusCode = 401;
            context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"HealthMonitor\"");
            await context.Response.WriteAsync("Authorization Required");
            return; // Stop pipeline
        }

        await next();
    });

    // --- API Endpoints ---

    // --- 2. Helper Endpoint to check status for UI ---
    app.MapGet("/api/auth/status", (HttpContext ctx) =>
    {
        return Results.Json(new
        {
            authenticated = ctx.Items["IsAuthenticated"],
            user = ctx.Items["Username"]
        });
    });

    // --- 3. Endpoint to Force Login (for Optional mode) ---
    app.MapGet("/Auth", (HttpContext ctx) =>
    {
        bool isAuth = ctx.Items.ContainsKey("IsAuthenticated") && (bool)ctx.Items["IsAuthenticated"];

        // If already auth, just show status
        if (isAuth)
        {
            ctx.Response.ContentType = "text/html";
            return ctx.Response.WriteAsync($"<html><body style='background:#1e1e1e;color:white;font-family:sans-serif;padding:20px;'><h1>Authenticated as {ctx.Items["Username"]}</h1><p>You can close this or <a href='/' style='color:#add8e6'>Go Back</a>.</p></body></html>");
        }

        // If not auth, force the prompt by sending 401
        ctx.Response.StatusCode = 401;
        ctx.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"HealthMonitor\"");
        return ctx.Response.WriteAsync("Please authenticate.");
    });


    // API: Get Ping Stats (Latency and Success Rate grouped by Decaminute)
    app.MapGet("/api/pings", (int days, HttpContext hctx) =>
    {

        bool isAuth = hctx.Items.ContainsKey("IsAuthenticated") && (bool)hctx.Items["IsAuthenticated"];
        int maxDays = isAuth ? 365 : 7;
        // --- ADD THIS CHECK ---
        if (days > maxDays)
        {
            return Results.BadRequest(new { error = "Web UI limited to last 7 days. Please use the desktop application for historical data." });
        }
        var cutoff = DateTime.UtcNow.AddDays(-days);
        using var ctx = new LogsContext();

        // Replicating the logic from NetworkPingStatsPanel
        var query = ctx.Pings
            .Where(p => p.TimeNow.CompareTo(cutoff.ToString("o")) >= 0).AsNoTracking();
                                                                        //.ToList();

        var grouped = query
            .GroupBy(p => new { p.Dest, Decaminute = p.TimeNow.Substring(0, 18) })
            .Select(g => new
            {
                g.Key.Dest,
                g.Key.Decaminute,
                LatencyAverage = g.Average(x => x.Latency),
                SuccessRate = g.Average(x => (x.WasItOkNotCorrupt == 1 || x.DidItSucceed == 1) ? 1.0 : 0.0) * 100
            })
            .OrderBy(x => x.Decaminute);
            //.ToList();

        // Structure data for D3: Dictionary<Dest, List<Point>>
        var result = grouped.GroupBy(x => x.Dest)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new
                {
                    x.Decaminute,
                    x.LatencyAverage,
                    x.SuccessRate,
                    // Helper for D3 time parsing
                    TimeIso = DateTime.Parse(x.Decaminute + "0").ToString("o")
                }).ToList()
            );

        return Results.Json(result);
    });

    // API: Get Process Stats (Optional, based on your schema)
    app.MapGet("/api/processes", (int days) =>
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        using var ctx = new LogsContext();

        // Returning raw process history for the last X days
        var data = ctx.ProcessHistories
            .Where(p => p.TimeNow.CompareTo(cutoff.ToString("o")) >= 0)
            .OrderByDescending(p => p.TimeNow)
            .Take(1000).AsNoTracking() // Limit to prevent browser crash for now
            .ToList();

        return Results.Json(data);
    });

    // API: Get list of unique processes with their last known window titles
    app.MapGet("/api/processes/list", () =>
    {
        using var ctx = new LogsContext();
        // Assuming WindowTitlesMainModules is a view or table in your context based on your Eto code
        // If not, fallback: ctx.ProcessHistories.GroupBy(x => x.ProcessName).Select(g => new { MainModulePath = g.Key, WindowName = "" })
        try
        {
            return Results.Json(ctx.WindowTitlesMainModules.Select(x => new { x.MainModulePath, x.WindowName }).AsNoTracking().ToList());
        }
        catch
        {
            // Fallback if the view doesn't exist in this specific context version
            return Results.Json(ctx.ProcessHistories.Select(x => new { MainModulePath = x.ProcessName, WindowName = x.MainWindowTitle }).Distinct().AsNoTracking().ToList());
        }
    });

    // API: Get hourly stats for a specific process path
    app.MapGet("/api/processes/stats", (string path, int days, HttpContext hctx) =>
    {
        bool isAuth = hctx.Items.ContainsKey("IsAuthenticated") && (bool)hctx.Items["IsAuthenticated"];
        int maxDays = isAuth ? 365 : 7;
        // --- ADD THIS CHECK ---
        if (days > maxDays)
        {
            return Results.BadRequest(new { error = "Web UI limited to last 7 days. Please use the desktop application for historical data." });
        }
        var cutoff = DateTime.UtcNow.AddDays(-days);
        using var ctx = new LogsContext();

        // Querying the hourly stats table
        // Note: Assumes StatsHourlyMainModulePaths exists. 
        // If you need to calculate this on the fly from ProcessHistories, that requires a more complex groupby query.
        try
        {
            var data = ctx.StatsHourlyMainModulePaths
                .Where(x => x.MainModulePath.ToLower() == path.ToLower() && x.Hour.CompareTo(cutoff.ToString("yyyy-MM-dd HH")) >= 0)
                .OrderBy(x => x.Hour)
                .Select(x => new
                {
                    x.Hour,
                    CpuPercent = x.CpuPercent ?? 0,
                    AvgMem = (x.AvgWorkingSet ?? 0) / (1024 * 1024), // Convert to MiB
                    PeakMem = double.Parse(x.MaxWorkingSetForOneInstance) / (1024 * 1024) // Convert to MiB
                })
                .AsNoTracking()
                .ToList();

            return Results.Json(data);
        }
        catch (System.Exception ex)
        {
            return Results.Json(new { Error = ex.Message });
        }
    });


    // ADD these endpoints inside StartServer (e.g., after app.MapRazorPages()):

    // --- 4. Certificate Generation (Auth Required) ---
    app.MapGet("/certgen", async (HttpContext hctx) =>
    {
        bool isAuth = hctx.Items.ContainsKey("IsAuthenticated") && (bool)hctx.Items["IsAuthenticated"];
        if (!isAuth) return Results.Unauthorized();

        string username = hctx.Items["Username"]?.ToString() ?? "user";
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var req = new System.Security.Cryptography.X509Certificates.CertificateRequest($"CN={username}", rsa, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new System.Security.Cryptography.X509Certificates.X509KeyUsageExtension(System.Security.Cryptography.X509Certificates.X509KeyUsageFlags.DigitalSignature, false));
        req.CertificateExtensions.Add(new System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension(
            new System.Security.Cryptography.OidCollection { new System.Security.Cryptography.Oid("1.3.6.1.5.5.7.3.2") }, false));

        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        string fp = cert.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256);

        // Store in DB
        using (var ctx = new LogsContext())
        {
            ctx.AllowedKeys.Add(new AllowedKey { Name = username, Sha256Fingerprint = fp, AddedTime = DateTime.UtcNow.ToString("o"), IsActive = 1 });
            ctx.SaveChanges();
        }

        Console.WriteLine($"[CertGen] Generated for {username}. SHA256 FP: {fp}");
        return Results.File(cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx), "application/x-pkcs12", $"{username}.pfx");
    });

    // --- 5. Certificate Generation (No Store) ---
    app.MapGet("/certgendontstore", async (HttpContext hctx) =>
    {
        bool isAuth = hctx.Items.ContainsKey("IsAuthenticated") && (bool)hctx.Items["IsAuthenticated"];
        if (!isAuth) return Results.Unauthorized();

        string username = hctx.Items["Username"]?.ToString() ?? "user";
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var req = new System.Security.Cryptography.X509Certificates.CertificateRequest($"CN={username}", rsa, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new System.Security.Cryptography.X509Certificates.X509KeyUsageExtension(System.Security.Cryptography.X509Certificates.X509KeyUsageFlags.DigitalSignature, false));
        req.CertificateExtensions.Add(new System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension(
            new System.Security.Cryptography.OidCollection { new System.Security.Cryptography.Oid("1.3.6.1.5.5.7.3.2") }, false));

        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        string fp = cert.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256);

        Console.WriteLine($"[CertGenNoStore] Generated for {username}. SHA256 FP: {fp}");
        return Results.File(cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx), "application/x-pkcs12", $"{username}.pfx");
    });

    // --- 6. Certificate Authentication Endpoint ---
    app.MapGet("/certauth", async (HttpContext hctx) =>
    {
        var clientCert = hctx.Connection.ClientCertificate;
        if (clientCert == null) { var try1=  await hctx.Connection.GetClientCertificateAsync(); if(try1 == null) return Results.Json(new { authenticated = false, error = "No certificate presented." }, statusCode: 403); }

        string fp = clientCert.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256);
        using var ctx = new LogsContext();

        var key = ctx.AllowedKeys.FirstOrDefault(k => k.Sha256Fingerprint == fp && k.IsActive == 1);
        if (key != null)
        {
            hctx.Items["IsAuthenticated"] = true;
            hctx.Items["Username"] = key.Name;
            return Results.Json(new { authenticated = true, user = key.Name });
        }
        return Results.Json(new { authenticated = false, error = "Unknown certificate." });
    });

    // --- API: Certificate Management (Auth Mandatory) ---

    // Get list of all allowed certificates
    app.MapGet("/api/certs", (HttpContext hctx) =>
    {
        // Enforce Mandatory Auth
        if (!hctx.Items.ContainsKey("IsAuthenticated") || !(bool)hctx.Items["IsAuthenticated"])
            return Results.Unauthorized();

        using var ctx = new LogsContext();
        // Return list ordered by newest first
        return Results.Json(ctx.AllowedKeys.OrderByDescending(k => k.AddedTime).ToList());
    });

    // Toggle certificate active status (Activate/Deactivate)
    app.MapPost("/api/certs/toggle/{id}", (int id, HttpContext hctx) =>
    {
        // Enforce Mandatory Auth
        if (!hctx.Items.ContainsKey("IsAuthenticated") || !(bool)hctx.Items["IsAuthenticated"])
            return Results.Unauthorized();

        using var ctx = new LogsContext();
        var key = ctx.AllowedKeys.FirstOrDefault(k => k.Id == id);
        if (key == null) return Results.NotFound(new { error = "Key not found" });

        // Toggle 0 <-> 1
        key.IsActive = (key.IsActive == 1) ? 0 : 1;
        ctx.SaveChanges();

        return Results.Json(new { success = true, id = key.Id, isActive = key.IsActive });
    });

    Console.WriteLine($"Web Server starting on {url}");
    app.Run();
}

string ConfigFile = System.IO.File.ReadAllText("HealthMonitor.toml");
List<string> destinations = new List<string>();
var TM = TomlSerializer.Deserialize<TomlTable>(ConfigFile);
var TA = ((TomlArray)TM["destinations"]);
int RetentionDays = 7;
if (TM.ContainsKey("RetentionDays"))
{
    RetentionDays = ((int)((long)TM["RetentionDays"]));
}
if (TM.ContainsKey("LogFile"))
{
    Config.LogFile = (((string)TM["LogFile"]));
}
if (TM.ContainsKey("SleepTimeMsBetweenPointsPing"))
{
    Config.SleepTimeMsBetweenPointsPing = (((int)((long)TM["SleepTimeMsBetweenPointsPing"])));
}
if (TM.ContainsKey("SleepTimeMsBetweenPointsProc"))
{
    Config.SleepTimeMsBetweenPointsProc = (((int)((long)TM["SleepTimeMsBetweenPointsProc"])));
}
if (Config.SleepTimeMsBetweenPointsPing < 500)
{
    Config.SleepTimeMsBetweenPointsPing = 500;
    Console.WriteLine("Warning: Ping monitoring sleep time between points is too low, set to 500.");
}
if (Config.SleepTimeMsBetweenPointsProc < 5000)
{
    Config.SleepTimeMsBetweenPointsProc = 300000;
    Console.WriteLine(
        "Warning: Process monitoring sleep time betweeen points is too low, set to 5000. It would generate too much of data and recommended value is more than 300000ms for long runs."
    );
}
if (TM.ContainsKey("AutoVacuumOnStartup"))
{
    Config.AutoVacuumOnStartup = ((bool)(TM["AutoVacuumOnStartup"]));
    Console.WriteLine($"Vacuum on startup set to {Config.AutoVacuumOnStartup}");
}
if (TM.ContainsKey("AutoVacuum"))
{
    Config.AutoVacuum = ((bool)(TM["AutoVacuum"]));
    Console.WriteLine($"Vacuum on startup set to {Config.AutoVacuum}");
}
if (TM.ContainsKey("Title"))
{
    Config.Title = ((string)(TM["Title"]));
    Console.WriteLine($"Console.Title set to \"{Config.Title}\".");
}
if (TM.ContainsKey("WebUI"))
{
    Config.WebUI = ((bool)(TM["WebUI"]));
}
if (TM.ContainsKey("Verbose"))
{
    Config.Verbose = ((bool)(TM["Verbose"]));
}
if (TM.ContainsKey("WebUIAddress"))
{
    Config.WebUIAddress = ((string)(TM["WebUIAddress"]));
}
if (TM.ContainsKey("WebUIPort"))
{
    Config.WebUIPort = (((int)((long)TM["WebUIPort"])));
}
if (TM.ContainsKey("AuthMode")) Config.AuthMode = (string)TM["AuthMode"];
if (TM.ContainsKey("AuthType")) Config.AuthType = (string)TM["AuthType"];
if (TM.ContainsKey("AuthUser")) Config.AuthUser = (string)TM["AuthUser"];
if (TM.ContainsKey("AuthPass")) Config.AuthPass = (string)TM["AuthPass"];
Console.WriteLine(
    $"LogFile: {Config.LogFile}, RetentionDays: {RetentionDays}, \nSleepTimeMsBetweenPointsProc (time waited for next proc stats collection): {Config.SleepTimeMsBetweenPointsProc}ms, \nSleepTimeMsBetweenPointsPing (likewise for pings): {Config.SleepTimeMsBetweenPointsPing}ms."
);
FileInfo F = new FileInfo(Config.LogFile);
try
{
    Console.WriteLine($"File size (log) : {F.Length}");
}
catch (System.Exception E)
{
    Console.WriteLine($"Unable to get File Info: {E.ToString()}");
}
if (Config.AutoVacuumOnStartup)
{
    try
    {
        using (var ctx = new LogsContext())
        {
            ctx.Database.ExecuteSqlRaw($"VACUUM;");
            ctx.Database.ExecuteSqlRaw($"ANALYZE;");
        }
    }
    catch (System.Exception E)
    {
        Console.WriteLine($"Error while VACUUM/ANALYZE: {E.ToString()}");
    }
}
if (Config.WebUI)
{
    (new Thread(() => StartServer())).Start();
}
if (TM.ContainsKey("WebUIHttpsCertPath"))
{
    Config.WebUIHttpsCertPath = (string)TM["WebUIHttpsCertPath"];
    Console.WriteLine($"Custom HTTPS Cert Path set to: {Config.WebUIHttpsCertPath}");
}
if (TM.ContainsKey("WebUIHttpsCertPassword"))
{
    Config.WebUIHttpsCertPassword = (string)TM["WebUIHttpsCertPassword"];
}

destinations = TA.Select(x => (string)x).ToList();

//destinations = new string[] {"192.168.1.1", "8.8.8.8", "1.1.1.1"};
foreach (var item in destinations)
{
    var t = new Thread(() =>
    {
        StartPing(item);
    });
    t.Start();
    Console.WriteLine("Ping thread started for: {0}", item);
}
;

(
    new Thread(() =>
    {
        Console.WriteLine($"Retention set to: {RetentionDays} days.");
        while (true)
        {
            var list = System.Diagnostics.Process.GetProcesses();
            try
            {
                using (var ctx = new LogsContext())
                {
                    if (Config.AutoVacuum)
                    {
                        try
                        {
                            ctx.Database.ExecuteSqlRaw($"PRAGMA auto_vacuum=FULL;");
                        }
                        catch (System.Exception E)
                        {
                            Console.WriteLine($"Error while VACUUM/ANALYZE: {E.ToString()}");
                        }
                    }
                    var days_string = RetentionDays.ToString();
                    var PingCleaner = ctx.Database.ExecuteSql(
                        $"DELETE FROM pings WHERE time_now < {DateTime.Now.Subtract(TimeSpan.FromDays(RetentionDays))};"
                    );
                    var ProcessHistoryCleaner = ctx.Database.ExecuteSql(
                        $"DELETE FROM main.process_history WHERE time_now < {DateTime.Now.Subtract(TimeSpan.FromDays(RetentionDays))};"
                    );
                    Console.WriteLine(
                        $"{PingCleaner.ToString()}, {ProcessHistoryCleaner.ToString()}"
                    );
                    //PingCleaner.ToList();
                    //ProcessHistoryCleaner.ToList();
                    string time_now = DateTime.Now.ToString("o");
                    foreach (var item in list)
                    {
                        try
                        {
                            int syst = 0,
                                ut = 0,
                                tt = 0,
                                tc = 0;
                            string wsmem = "0",
                                vmuse = "0",
                                prmemuse = "0";
                            string sttt = "0";
                            string mmpath = "",
                                mmver = "";
                            try
                            {
                                mmpath = item.MainModule?.FileName ?? item.ProcessName ;
                                mmver = item.MainModule?.FileVersionInfo.ToString()?? "";
                            }
                            catch (System.Exception E) { if(Config.Verbose) Console.WriteLine(E.ToString()); }
                            try
                            {
                                vmuse = item.VirtualMemorySize64.ToString();
                                syst = (int)item.PrivilegedProcessorTime.TotalMilliseconds;
                                ut = (int)item.UserProcessorTime.TotalMilliseconds;
                                tt = (int)item.TotalProcessorTime.TotalMilliseconds;
                                sttt = item.StartTime.ToString("o");
                                wsmem = item.WorkingSet64.ToString();
                                prmemuse = item.PrivateMemorySize64.ToString();
                                tc = item.Threads.Count;
                            }
                            catch (Win32Exception e) { if (Config.Verbose) Console.WriteLine(e.ToString()); }
                            catch (System.Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                            ctx.ProcessHistories.Add(
                                new ProcessHistory
                                {
                                    MainWindowTitle = item.MainWindowTitle,
                                    PagedMemoryUse = item.PagedMemorySize64.ToString(),
                                    Pid = item.Id,
                                    PrivateMemoryUse = prmemuse,
                                    ProcessName = item.ProcessName,
                                    Started = sttt,
                                    SystemTime = syst,
                                    UserTime = ut,
                                    TotalTime = tt,
                                    ThreadCount = tc,
                                    TimeNow = time_now,
                                    VirtualMemoryUse = vmuse,
                                    WorkingSet = wsmem,
                                    MainModulePath = mmpath,
                                    MainModuleVersion = mmver,
                                }
                            );
                        }
                        catch (Win32Exception E)
                        {
                            if (Config.Verbose) Console.WriteLine(E.ToString());
                            //Console.WriteLine(E.ToString());
                        }
                        catch (System.Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                    ctx.SaveChanges();
                }
            }
            catch (System.Exception E)
            {
                Console.WriteLine(E.ToString());
            }

            Thread.Sleep(Config.SleepTimeMsBetweenPointsProc);
        }
        GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
    })
).Start();
try
{
    Console.Title = Config.Title;
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.BackgroundColor = ConsoleColor.DarkBlue;
    Console.CursorVisible = true;
}
catch (System.Exception E)
{
    Console.WriteLine($"Unable to set title: {E.ToString()}, {E.StackTrace}");
}

public static class Config
{
    public static string LogFile = "logs.sqlite3.rvhealthmonitorlogfile";
    public static int SleepTimeMsBetweenPointsPing = 30000;
    public static int SleepTimeMsBetweenPointsProc = 300000;
    public static bool AutoVacuumOnStartup = true;
    public static bool AutoVacuum = true;
    public static string Title =
        "Health Monitor (logging service), © Rishikeshan S/L, License: Open Software License, V3 (no later).";
    public static bool WebUI = false;
    public static string WebUIAddress = "localhost";
    public static int WebUIPort = 8888;
    public static string AuthMode = "None"; // Options: "None", "Optional", "Mandatory"
    public static string AuthType = "Basic"; // Options: "Basic", "Digest"
    public static string AuthUser = "admin";
    public static string AuthPass = "password";
    public static string WebUIHttpsCertPath = ""; // Path to .pfx file
    public static string WebUIHttpsCertPassword = ""; // Password for the .pfx
    public static bool Verbose = false;
}
