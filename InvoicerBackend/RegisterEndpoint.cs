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
using MyAOTFriendlyExtensions;
using RV.InvNew.Common;
using RV.InvNew.Common;

namespace InvoicerBackend
{
    public static class RegisterEndpoint
    {
        public delegate object Del(object o);

        public readonly record struct LoginDetails(long? UserId, string TokenId, string Principal);

        public delegate object DelWithDetails(object o, LoginDetails Login);
        public delegate object PatchDelWithDetails(string JsonPatch, LoginDetails Login);

        public static void TryAddPermissionToDatabase(string Permission)
        {
            using (var ctx = new NewinvContext())
            {
                try
                {
                    ctx.PermissionsLists.Add(
                        new PermissionsList() { Permission = Permission.ToUpperInvariant() }
                    );
                    ctx.SaveChanges();
                }
                catch (Exception E)
                {
                    Console.WriteLine($"{E.ToString}, {E.StackTrace}");
                }
            }
        }

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
            TryAddPermissionToDatabase(Permission);
            return app;
        }

        public static WebApplication AddEndpointWithBearerAuth<T>(
            this WebApplication app,
            string Name,
            DelWithDetails D,
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
                                return (
                                    Results.Json<object>(
                                        D(
                                            AuthenticatedInner,
                                            new LoginDetails(
                                                VerificationResultAndMessage.UserID,
                                                VerificationResultAndMessage.Token,
                                                VerificationResultAndMessage.Username
                                            )
                                        )
                                    )
                                );
                            }
                        }
                        throw new UnauthorizedAccessException();
                    }
                )
                .WithName(Name)
                .WithOpenApi();
            TryAddPermissionToDatabase(Permission);

            return app;
        }

        /// <summary>
        /// Takes a patch JSON, removes keys from the RemovalKeys list runs the PatchDelWithDetails with the filtered JSON string.
        /// Bearer authenticated. Permissions needed will be added to the global reference list if supplied and not present there already.
        /// The global reference list is only for the reference when managing the users and will show up on the user editor and will not be added automatically to all/any users.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app">It is this</param>
        /// <param name="Name"></param>
        /// <param name="D">The function to be called when authentication/authorization is successful</param>
        /// <param name="RemovalKeys">Keys unauthorized or unauthorized field list</param>
        /// <param name="Permission">Required access level</param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public static WebApplication AddPatchEndpointWithBearerAuth<T>(
            this WebApplication app,
            string Name,
            DelWithDetails D,
            string[] RemovalKeys,
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
                            var AuthenticatedInner =
                                VerificationResultAndMessage.RequestBody.RemoveFieldFromJsonMultiple(RemovalKeys);
                            ;
                            if (AuthenticatedInner != null)
                            {
                                return (
                                    Results.Json<object>(
                                        D(
                                            AuthenticatedInner,
                                            new LoginDetails(
                                                VerificationResultAndMessage.UserID,
                                                VerificationResultAndMessage.Token,
                                                VerificationResultAndMessage.Username
                                            )
                                        )
                                    )
                                );
                            }
                        }
                        throw new UnauthorizedAccessException();
                    }
                )
                .WithName(Name)
                .WithOpenApi();
            TryAddPermissionToDatabase(Permission);

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
                                return Results.Content(
                                    JsonSerializer.Serialize(D(AuthenticatedInner)),
                                    "application/json"
                                );
                            }
                        }
                        throw new UnauthorizedAccessException();
                    }
                )
                .WithName(Name)
                .WithOpenApi();
            TryAddPermissionToDatabase(Permission);
            return app;
        }
    }
}
