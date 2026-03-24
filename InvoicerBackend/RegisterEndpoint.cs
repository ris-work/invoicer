using System;
using System.IO.Compression;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Transactions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using MyAOTFriendlyExtensions;
using RV.InvNew.Common;
using RV.InvNew.Common;

namespace InvoicerBackend
{
    public class EndpointDefinition
    {
        public string EndpointName;
        public Type InputType;
        public object Function;
        public Type OutputType;
        public string Privilege;
    }
    
    public static class RegisterEndpoint
    {
        public static Dictionary<string, EndpointDefinition> EndpointsDict = new();
        public delegate object Del(object o);

        public readonly record struct LoginDetails(long? UserId, string TokenId, string Principal, long RequestId, string[] PermittedTo);

        public delegate object DelWithDetails(object o, LoginDetails Login);
        public delegate Task<object> DelWithDetailsAsync(object o, LoginDetails Login);
        public delegate object PatchDelWithDetails(string JsonPatch, LoginDetails Login);

        public static Type UnwrapResultType(Type type)
        {
            // Handle Task (non-generic) -> void
            if (type == typeof(Task)) return typeof(void);

            // Handle Task -> T
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return type.GetGenericArguments()[0];
            }

            // Handle ValueTask -> T
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                return type.GetGenericArguments()[0];
            }

            // Fallback
            return type;
        }

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

        public static WebApplication AddEndpoint<T, To>(
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

        public static WebApplication AddEndpointWithBearerAuth<T, To>(
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
                            // In your JSOptions configuration
                            var JSOptions = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                PropertyNameCaseInsensitive = true
                            };

                            // Add this line to your existing options setup
                            JSOptions.Converters.Add(new FlexibleDateTimeOffsetConverter());
                            var AuthenticatedInner = JsonSerializer.Deserialize<T>(
                                VerificationResultAndMessage.RequestBody, JSOptions
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
                                                VerificationResultAndMessage.Username,
                                                VerificationResultAndMessage.RequestId,
                                                VerificationResultAndMessage.PermittedTo
                                            )
                                        )
                                    )
                                );
                            }
                        }
                        return Results.Unauthorized();
                        throw new UnauthorizedAccessException();
                    }
                )
                .WithName(Name)
                .WithOpenApi();
            TryAddPermissionToDatabase(Permission);
            EndpointsDict.Add(Name, new EndpointDefinition() { EndpointName = Name, Function = D, InputType = D.Method.GetParameters()[0].ParameterType, OutputType = typeof(To), Privilege = Permission });
            return app;
        }

        public static WebApplication AddAsyncEndpointWithBearerAuth<T, To>(
            this WebApplication app,
            string Name,
            DelWithDetailsAsync D,
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
                            // In your JSOptions configuration
                            var JSOptions = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                PropertyNameCaseInsensitive = true
                            };

                            // Add this line to your existing options setup
                            JSOptions.Converters.Add(new FlexibleDateTimeOffsetConverter());
                            var AuthenticatedInner = JsonSerializer.Deserialize<T>(
                                VerificationResultAndMessage.RequestBody, JSOptions
                            );
                            if (AuthenticatedInner != null)
                            {
                                return (
                                    Results.Json<object>(
                                        await D(
                                            AuthenticatedInner,
                                            new LoginDetails(
                                                VerificationResultAndMessage.UserID,
                                                VerificationResultAndMessage.Token,
                                                VerificationResultAndMessage.Username,
                                                VerificationResultAndMessage.RequestId,
                                                VerificationResultAndMessage.PermittedTo
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
            // Use typeof(T) for input as it's explicitly generic
            var inputType = typeof(T);

            // Unwrap the delegate return type (Task<object> -> object)
            var outputType = UnwrapResultType(D.Method.ReturnType);
            EndpointsDict.Add(Name, new EndpointDefinition()
            {
                EndpointName = Name,
                Function = D,
                InputType = inputType,
                OutputType = outputType, // This will be 'object' with current delegate
                Privilege = Permission
            });

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
        public static WebApplication AddPatchEndpointWithBearerAuth<T, To>(
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
                                VerificationResultAndMessage.RequestBody.RemoveFieldFromJsonMultiple(
                                    RemovalKeys
                                );
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
                                                VerificationResultAndMessage.Username,
                                                VerificationResultAndMessage.RequestId,
                                                VerificationResultAndMessage.PermittedTo
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
            EndpointsDict.Add(Name, new EndpointDefinition() { EndpointName = Name, Function = D, InputType = D.Method.GetParameters()[0].ParameterType, OutputType = typeof(To), Privilege = Permission });

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
        public static WebApplication AddAsyncPatchEndpointWithBearerAuth<T, To>(
            this WebApplication app,
            string Name,
            DelWithDetailsAsync D,
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
                                VerificationResultAndMessage.RequestBody.RemoveFieldFromJsonMultiple(
                                    RemovalKeys
                                );
                            ;
                            if (AuthenticatedInner != null)
                            {
                                return (
                                    Results.Json<object>(
                                        await D(
                                            AuthenticatedInner,
                                            new LoginDetails(
                                                VerificationResultAndMessage.UserID,
                                                VerificationResultAndMessage.Token,
                                                VerificationResultAndMessage.Username,
                                                VerificationResultAndMessage.RequestId,
                                                VerificationResultAndMessage.PermittedTo
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
                .WithName(Name).Accepts<T>("application/json").Produces(200, D.Method.ReturnType, "application/json")
                .WithOpenApi();
            TryAddPermissionToDatabase(Permission);
            // Use typeof(T) for input as it's explicitly generic
            var inputType = typeof(T);

            // Unwrap the delegate return type (Task<object> -> object)
            var outputType = UnwrapResultType(D.Method.ReturnType);
            EndpointsDict.Add(Name, new EndpointDefinition()
            {
                EndpointName = Name,
                Function = D,
                InputType = inputType,
                OutputType = typeof(To), // This will be 'object' with current delegate
                Privilege = Permission
            });
            //EndpointsDict.Add(Name, new EndpointDefinition() { EndpointName = Name, Function = D, InputType = D.Method.GetParameters()[0].ParameterType, OutputType = D.Method.ReturnType, Privilege = Permission });

            return app;
        }

        public static WebApplication AddEndpointWithBearerAuth<T, To>(
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
                            // In your JSOptions configuration
                            var JSOptions = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                PropertyNameCaseInsensitive = true
                            };

                            // Add this line to your existing options setup
                            JSOptions.Converters.Add(new FlexibleDateTimeOffsetConverter());
                            var AuthenticatedInner = JsonSerializer.Deserialize<T>(
                                VerificationResultAndMessage.RequestBody, JSOptions
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
                .WithName(Name).Accepts<T>("application/json").Produces(200, D.Method.ReturnType, "application/json")
                .WithOpenApi();
            TryAddPermissionToDatabase(Permission);
            EndpointsDict.Add(Name, new EndpointDefinition() { EndpointName = Name, Function = D, InputType = D.Method.GetParameters()[0].ParameterType, OutputType = typeof(To), Privilege = Permission });
            return app;
        }
    }
}
