using System;
using System.IO.Compression;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Transactions;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using RV.InvNew.Common;
using RV.InvNew.Common;

namespace InvoicerBackend
{
    public static class RegisterEndpoint
    {
        public delegate object Del(object o);

        public static WebApplication AddEndpoint<T>(
            this WebApplication app,
            string Name,
            Del D,
            string Permission = ""
        )
        {
            app.MapPost(
                    $"/{Name}",
                    (AuthenticatedRequest<T> a) =>
                    {
                        var AuthenticatedInner = a.Get(Permission, $"/{Name}");
                        if (AuthenticatedInner != null)
                        {
                            return D(AuthenticatedInner);
                        }
                        throw new UnauthorizedAccessException();
                    }
                )
                .WithName(Name)
                .WithOpenApi();
            return app;
        }

        public static WebApplication AddEndpointWithBearerAuth<T>(
            this WebApplication app,
            string Name,
            Del D,
            string Permission = ""
        )
        {
            app.MapPost(
                    $"/{Name}",
                    async (HttpRequest a) =>
                    {
                        var VerificationResultAndMessage =
                            await LoginBearerTokenVerifier.VerifyIfAuthorizationIsOk(
                                a,
                                Permission,
                                Name
                            );
                        if (VerificationResultAndMessage.Success)
                        {
                            System.Console.WriteLine(
                                $"Authenticated Request Content: {VerificationResultAndMessage.RequestBody}, Length: {VerificationResultAndMessage.RequestBody.Length}"
                            );
                            var AuthenticatedInner = JsonSerializer.Deserialize<T>(
                                VerificationResultAndMessage.RequestBody
                            );
                            if (AuthenticatedInner != null)
                            {
                                return D(AuthenticatedInner);
                            }
                        }
                        throw new UnauthorizedAccessException();
                    }
                )
                .WithName(Name)
                .WithOpenApi();
            return app;
        }
    }
}
