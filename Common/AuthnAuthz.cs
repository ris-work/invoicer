using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace RV.InvNew.Common
{
    public record LoginToken(string? TokenID, string? Token, string? SecretToken, string? Error);

    public static partial class LoginBearerTokenVerifier
    {
        public static bool VerifyToken(LoginToken Token)
        {
            return false;
        }
        public static void AddToRequestLog(HttpRequest Request, bool WasItBad, string? RequestedPrivilegeLevel, long? PrincipalUserId, LoginToken Token, string? ExistingPrivilegeList, string? Endpoint = null)
        {
            using (var ctx = new NewinvContext())
            {
                if (WasItBad)
                {
                    ctx.Database.ExecuteSql(
                        $"INSERT INTO requests_bad(principal, token, request_body, type, requested_privilege_level, endpoint, provided_privilege_levels) VALUES ({PrincipalUserId}, {JsonSerializer.Serialize<LoginToken>(Token)}, {Request.Body}, {typeof(Request).ToString()}, {RequestedPrivilegeLevel}, {Endpoint}, {ExistingPrivilegeList})"
                    );
                    /*ctx.RequestsBads.Add(
                        new RequestsBad
                        {
                            Principal = this.PrincipalUserId,
                            RequestBody = JsonSerializer.Serialize<T>(Request),
                            Token = JsonSerializer.Serialize<LoginToken>(Token),
                        }
                    );*/
                }
                else
                {
                    ctx.Database.ExecuteSql(
                        $"INSERT INTO requests(principal, token, request_body, type, requested_privilege_level, endpoint, provided_privilege_levels) VALUES ({PrincipalUserId}, {JsonSerializer.Serialize<LoginToken>(Token)}, {Request.Body}, {typeof(Request).ToString()}, {RequestedPrivilegeLevel}, {Endpoint}, {ExistingPrivilegeList})"
                    );
                    /*ctx.Requests.Add(
                        new Request
                        {
                            Principal = this.PrincipalUserId ?? -1,
                            RequestBody = JsonSerializer.Serialize<T>(Request),
                            Token = JsonSerializer.Serialize<LoginToken>(Token),
                        }
                    );*/
                }
                //ctx.SaveChanges();
            }
        }
        public static bool VerifyAuthorization(HttpRequest Request, string PrivilegeLevel, string Endpoint)
        {
            string Principal;
            long? PrincipalUserId = null;
            string? ExistingPrivilegeList = null;
            bool auth_success;
            //string ExistingPrivilegeList = "";
            Console.WriteLine($"Requested {PrivilegeLevel}");
            StringValues BearerToken;
            bool HasBearerToken = Request.Headers.TryGetValue("Bearer", out BearerToken);
            LoginToken Token = JsonSerializer.Deserialize<LoginToken>(BearerToken[0]);
            using (var ctx = new NewinvContext())
            {
                try
                {
                    ExistingPrivilegeList = ctx
                        .Tokens.Where(t => t.Tokenid == Token.TokenID)
                        .First()
                        .Privileges.ToLowerInvariant();
                    if (Token.TokenID != null && Token != null)
                        if (
                            ctx.Tokens.Where(t => t.Tokenid == Token.TokenID)
                                .First()
                                .Tokenvalue == Token.Token
                            && ExistingPrivilegeList
                                .Split(',')
                                .Contains(PrivilegeLevel.ToLowerInvariant())
                        )
                        {
                            //output = this.Request;
                            auth_success = true;
                            var PrincipalEntry = ctx
                                .Credentials.Where(e =>
                                    e.Userid
                                    == ctx.Tokens.Where(t => t.Tokenid == Token.TokenID)
                                        .First()
                                        .Userid
                                )
                                .First();
                            Principal = PrincipalEntry.Username;
                            PrincipalUserId = PrincipalEntry.Userid;
                        }
                        else
                            auth_success = false;
                    else
                        auth_success = false;
                }
                catch (Exception E)
                {
                    auth_success = false;
                }
            }

            if (auth_success)
            {
                AddToRequestLog(Request, false, PrivilegeLevel.ToLowerInvariant(), PrincipalUserId, Token, Endpoint);
                return true;
            }
            else
            {
                Console.Error.WriteLine(
                    $"{PrivilegeLevel.ToLowerInvariant()} not in {ExistingPrivilegeList}"
                );
                AddToRequestLog(Request, true, PrivilegeLevel.ToLowerInvariant(), PrincipalUserId, Token, Endpoint);
                return false;
            }
            return false;
        }
    }

    public record LoginTokenElevated(
        string? TokenID,
        string? Token,
        string? SecretToken,
        string? Error
    );

    public record LoginCredentials(string User, string Password, string Terminal, string? TOTP);

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(AuthenticatedRequest<string>))]
    public partial class AuthenticatedRequest<T>
    {
        [JsonInclude]
        public LoginToken Token;

        [JsonInclude]
        public T Request;
        public string Principal;
        public long? PrincipalUserId;
        private string? ExistingPrivilegeList = null;

        public void AddToRequestLog(T Request, bool WasItBad, string? RequestedPrivilegeLevel, string? Endpoint = null)
        {
            using (var ctx = new NewinvContext())
            {
                if (WasItBad)
                {
                    ctx.Database.ExecuteSql(
                        $"INSERT INTO requests_bad(principal, token, request_body, type, requested_privilege_level, endpoint, provided_privilege_levels) VALUES ({this.PrincipalUserId}, {JsonSerializer.Serialize<LoginToken>(Token)}, {JsonSerializer.Serialize<T>(Request)}, {typeof(Request).ToString()}, {RequestedPrivilegeLevel}, {Endpoint}, {ExistingPrivilegeList})"
                    );
                    /*ctx.RequestsBads.Add(
                        new RequestsBad
                        {
                            Principal = this.PrincipalUserId,
                            RequestBody = JsonSerializer.Serialize<T>(Request),
                            Token = JsonSerializer.Serialize<LoginToken>(Token),
                        }
                    );*/
                }
                else
                {
                    ctx.Database.ExecuteSql(
                        $"INSERT INTO requests(principal, token, request_body, type, requested_privilege_level, endpoint, provided_privilege_levels) VALUES ({this.PrincipalUserId}, {JsonSerializer.Serialize<LoginToken>(Token)}, {JsonSerializer.Serialize<T>(Request)}, {typeof(Request).ToString()}, {RequestedPrivilegeLevel}, {Endpoint}, {ExistingPrivilegeList})"
                    );
                    /*ctx.Requests.Add(
                        new Request
                        {
                            Principal = this.PrincipalUserId ?? -1,
                            RequestBody = JsonSerializer.Serialize<T>(Request),
                            Token = JsonSerializer.Serialize<LoginToken>(Token),
                        }
                    );*/
                }
                //ctx.SaveChanges();
            }
        }

        public AuthenticatedRequest(T Request, LoginToken Token)
        {
            this.Token = Token;
            this.Request = Request;
        }

        public T? Get()
        {
            T? output;
            bool auth_success;
            using (var ctx = new NewinvContext())
            {
                if (this.Token.TokenID != null && this.Token != null)
                    try
                    {
                        if (
                            ctx.Tokens.Where(t => t.Tokenid == this.Token.TokenID)
                                .First()
                                .Tokenvalue == this.Token.Token
                        )
                        {
                            output = this.Request;
                            auth_success = true;
                            var PrincipalEntry = ctx
                                .Credentials.Where(e =>
                                    e.Userid
                                    == ctx.Tokens.Where(t => t.Tokenid == this.Token.TokenID)
                                        .First()
                                        .Userid
                                )
                                .First();
                            Principal = PrincipalEntry.Username;
                            PrincipalUserId = PrincipalEntry.Userid;
                        }
                        else
                            auth_success = false;
                    }
                    catch (Exception _)
                    {
                        auth_success = false;
                    }
                else
                    auth_success = false;
            }
            if (auth_success)
            {
                AddToRequestLog(Request, false, null);
                return this.Request;
            }
            else
            {
                AddToRequestLog(Request, true, null);
                return default(T);
            }
        }

        public T? Get(string PrivilegeLevel, string? Endpoint = null)
        {
            T? output;
            bool auth_success;
            //string ExistingPrivilegeList = "";
            Console.WriteLine($"Requested {PrivilegeLevel}");
            using (var ctx = new NewinvContext())
            {
                try
                {
                    ExistingPrivilegeList = ctx
                        .Tokens.Where(t => t.Tokenid == this.Token.TokenID)
                        .First()
                        .Privileges.ToLowerInvariant();
                    if (this.Token.TokenID != null && this.Token != null)
                        if (
                            ctx.Tokens.Where(t => t.Tokenid == this.Token.TokenID)
                                .First()
                                .Tokenvalue == this.Token.Token
                            && ExistingPrivilegeList
                                .Split(',')
                                .Contains(PrivilegeLevel.ToLowerInvariant())
                        )
                        {
                            output = this.Request;
                            auth_success = true;
                            var PrincipalEntry = ctx
                                .Credentials.Where(e =>
                                    e.Userid
                                    == ctx.Tokens.Where(t => t.Tokenid == this.Token.TokenID)
                                        .First()
                                        .Userid
                                )
                                .First();
                            Principal = PrincipalEntry.Username;
                            PrincipalUserId = PrincipalEntry.Userid;
                        }
                        else
                            auth_success = false;
                    else
                        auth_success = false;
                }
                catch (Exception E)
                {
                    auth_success = false;
                }
            }

            if (auth_success)
            {
                AddToRequestLog(Request, false, PrivilegeLevel.ToLowerInvariant(), Endpoint);
                return this.Request;
            }
            else
            {
                Console.Error.WriteLine(
                    $"{PrivilegeLevel.ToLowerInvariant()} not in {ExistingPrivilegeList}"
                );
                AddToRequestLog(Request, true, PrivilegeLevel.ToLowerInvariant(), Endpoint);
                return default(T);
            }
        }
    }
}
