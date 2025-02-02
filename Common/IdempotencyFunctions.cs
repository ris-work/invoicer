using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RV.InvNew.Common
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Idempotent<AuthenticatedRequest<string>>))]
    public partial class Idempotent<T>
    {
        [JsonInclude]
        public string IdempotencyKey;

        [JsonInclude]
        public T RequestBody;

        public T GetValueIfNew()
        {
            using (var ctx = new NewinvContext())
            {
                if (!ctx.Idempotencies.Any(e => e.Key == IdempotencyKey))
                {
                    return RequestBody;
                }
                else
                    return default(T);
            }
        }

        public void Done()
        {
            using (var ctx = new NewinvContext())
            {
                ctx.Idempotencies.Add(
                    new Idempotency
                    {
                        Key = IdempotencyKey,
                        Request = JsonSerializer.Serialize<T>(RequestBody),
                    }
                );
            }
        }
    }
}
