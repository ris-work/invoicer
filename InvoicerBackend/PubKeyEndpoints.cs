using RV.InvNew.Common;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace InvoicerBackend
{

    // DTOs for Cert Generation
    public class CertGenResponse
    {
        public string FileName { get; set; }
        public string Base64Data { get; set; } // PFX Data
        public string Fingerprint { get; set; }
    }
    public static class PubKeyEndpoints
    {
        public static WebApplication AddPubKeyEndpoints(this WebApplication app)
        {
            // 1. Generate & Store (Protected)
            app.AddAsyncEndpointWithBearerAuth<object, CertGenResponse>(
                "CertGen",
                async (DataIn, LoginInfo) =>
                {
                    long userId = (long)LoginInfo.UserId;
                    string username = LoginInfo.Principal ?? "User";

                    using var rsa = RSA.Create(2048);
                    var req = new CertificateRequest($"CN={username}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
                    req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                        new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, false));

                    var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
                    string fp = cert.GetCertHashString(HashAlgorithmName.SHA256);
                    byte[] pfxBytes = cert.Export(X509ContentType.Pfx);

                    using var ctx = new NewinvContext();
                    ctx.AllowedKeys.Add(new AllowedKey
                    {
                        Name = username,
                        Principal = userId,
                        FingerprintSha256 = fp,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CertContents = Convert.ToBase64String(pfxBytes),
                        ValidUntil = DateTime.UtcNow.AddYears(5)
                    });
                    await ctx.SaveChangesAsync();

                    return new CertGenResponse
                    {
                        FileName = $"{username}.pfx",
                        Base64Data = Convert.ToBase64String(pfxBytes),
                        Fingerprint = fp
                    };
                },
                "Refresh"
            );

            // 2. Generate Temp (Protected)
            app.AddAsyncEndpointWithBearerAuth<object, CertGenResponse>(
                "CertGenNoStore",
                async (DataIn, LoginInfo) =>
                {
                    string username = LoginInfo.Principal ?? "User";
                    using var rsa = RSA.Create(2048);
                    var req = new CertificateRequest($"CN={username}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
                    req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                        new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, false));

                    var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
                    byte[] pfxBytes = cert.Export(X509ContentType.Pfx);

                    return new CertGenResponse
                    {
                        FileName = $"{username}_temp.pfx",
                        Base64Data = Convert.ToBase64String(pfxBytes),
                        Fingerprint = cert.GetCertHashString(HashAlgorithmName.SHA256)
                    };
                },
                "Refresh"
            );

            // 3. List Keys
            app.AddAsyncEndpointWithBearerAuth<object, List<AllowedKey>>(
                "GetAllowedKeys",
                async (DataIn, LoginInfo) =>
                {
                    using var ctx = new NewinvContext();
                    return await ctx.AllowedKeys
                        .OrderByDescending(k => k.CreatedAt)
                        .ToListAsync();
                },
                "Refresh"
            );

            // 4. Toggle Key
            app.AddAsyncEndpointWithBearerAuth<long, bool>(
                "ToggleAllowedKey",
                async (KeyIn, LoginInfo) =>
                {
                    long keyId = (long)KeyIn;
                    using var ctx = new NewinvContext();
                    var key = await ctx.AllowedKeys.FirstOrDefaultAsync(k => k.Id == keyId);
                    if (key == null) throw new ArgumentException("Key not found");

                    key.IsActive = !key.IsActive;
                    await ctx.SaveChangesAsync();
                    return key.IsActive;
                },
                "Refresh"
            );

            return app;
        }
    }
}
