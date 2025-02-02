using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RV.InvNew.Common
{
    public record LoginToken(string? TokenID, string? Token, string? SecretToken, string? Error);

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

        public void AddToRequestLog(T Request, bool WasItBad)
        {
            using (var ctx = new NewinvContext())
            {
                if (WasItBad)
                {
                    ctx.RequestsBads.Add(
                        new RequestsBad
                        {
                            Principal = this.PrincipalUserId,
                            RequestBody = JsonSerializer.Serialize<T>(Request),
                            Token = JsonSerializer.Serialize<LoginToken>(Token),
                        }
                    );
                }
                else
                {
                    ctx.Requests.Add(
                        new Request
                        {
                            Principal = this.PrincipalUserId ?? -1,
                            RequestBody = JsonSerializer.Serialize<T>(Request),
                            Token = JsonSerializer.Serialize<LoginToken>(Token),
                        }
                    );
                }
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
                    if (
                        ctx.Tokens.Where(t => t.Tokenid == this.Token.TokenID).First().Tokenvalue
                        == this.Token.Token
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
            if (auth_success)
            {
                return this.Request;
            }
            else
                return default(T);
        }

        public T? Get(string PrivilegeLevel)
        {
            T? output;
            bool auth_success;
            string ExistingPrivilegeList = "";
            using (var ctx = new NewinvContext())
            {
                ExistingPrivilegeList = ctx
                    .Tokens.Where(t => t.Tokenid == this.Token.TokenID)
                    .First()
                    .Privileges.ToLowerInvariant();
                if (this.Token.TokenID != null && this.Token != null)
                    if (
                        ctx.Tokens.Where(t => t.Tokenid == this.Token.TokenID).First().Tokenvalue
                            == this.Token.Token
                        && ctx.Tokens.Where(t => t.Tokenid == this.Token.TokenID)
                            .First()
                            .Privileges.ToLowerInvariant()
                            .Split(',')
                            .Contains(PrivilegeLevel.ToLowerInvariant())
                    )
                    {
                        output = this.Request;
                        auth_success = true;
                        Principal = ctx
                            .Credentials.Where(e =>
                                e.Userid
                                == ctx.Tokens.Where(t => t.Tokenid == this.Token.TokenID)
                                    .First()
                                    .Userid
                            )
                            .First()
                            .Username;
                    }
                    else
                        auth_success = false;
                else
                    auth_success = false;
            }
            if (auth_success)
            {
                return this.Request;
            }
            else
            {
                Console.Error.WriteLine(
                    $"{PrivilegeLevel.ToLowerInvariant()} not in {ExistingPrivilegeList}"
                );
                return default(T);
            }
        }
    }
}
