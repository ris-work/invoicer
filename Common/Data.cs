using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            string Terminal
        );


}
