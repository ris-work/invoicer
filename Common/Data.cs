using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RV.InvNew.Common
{

        public record LoginToken(
            string? TokenID,
            string? Token,
            string? SecretToken,
            string? Error
        );

        public record LoginCredentials(
            string User,
            string Password,
            string Terminal,
            string? TOTP
        );

    [JsonSourceGenerationOptions(WriteIndented =true)]
    [JsonSerializable(typeof(AuthenticatedRequest<string>))]
    public partial class AuthenticatedRequest<T>
    {
        [JsonInclude]
        public LoginToken Token;
        [JsonInclude]
        public T Request;

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
                    if (ctx.Tokens.Where(t => t.Tokenid == this.Token.TokenID).First().Tokenvalue == this.Token.Token)
                    {
                        output = this.Request;
                        auth_success = true;
                    }
                    else auth_success = false;
                else auth_success = false;
            }
            if (auth_success)
            {
                return this.Request;
            }
            else return default(T);
        }
    }


}
