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

    [JsonSourceGenerationOptions(WriteIndented = true)]
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

    [JsonSerializable(typeof(PosRefresh))]
    [JsonSerializable(typeof(List<PosBatch>))]
    [JsonSerializable(typeof(List<PosCatalogue>))]
    [JsonSerializable(typeof(List<VatCategory>))]
    [JsonSourceGenerationOptions(WriteIndented =true, IncludeFields =true)]
    public class PosRefresh{
        [JsonInclude] public List<PosCatalogue> Catalogue;
        [JsonInclude] public List<PosBatch> Batches;
        [JsonInclude] public List<VatCategory> VatCategories;
    }

    
    [JsonSerializable(typeof(PosCatalogue))]
    [JsonSourceGenerationOptions(WriteIndented =true, IncludeFields =true)]
    public class PosCatalogue
    {
        [JsonInclude] public long itemcode;
        [JsonInclude] public string itemdesc;
        [JsonInclude] public bool VatCategoryAdjustable;
        [JsonInclude] public bool VatDependsOnUser;
        [JsonInclude] public bool ManualPrice;
        [JsonInclude] public bool EnforceAboveCost;
        [JsonInclude] public long DefaultVatCategory;
        public string[] ToStringArray()
        {
            var r = new Random();
            return new string[] { itemcode.ToString(), itemdesc, DefaultVatCategory.ToString(), itemdesc.Split(" ")[0], itemdesc.Split(" ")[1], itemdesc.Split(" ")[2], r.NextInt64().ToString() };
        }
    }

    [JsonSerializable(typeof(PosBatch))]
    
    [JsonSourceGenerationOptions(WriteIndented =true, IncludeFields =true)]
    public class PosBatch
    {
        [JsonInclude] public long itemcode;
        [JsonInclude] public long batchcode;
        [JsonInclude] public double selling;
        [JsonInclude] public double marked;
        [JsonInclude] public DateOnly expireson;
    }

    [JsonSerializable(typeof(List<PosBatch>))]
    [JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
    public partial class PosBatchSerialize: JsonSerializerContext { }

    [JsonSerializable(typeof(List<PosCatalogue>))]
    [JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
    public partial class PosCatalogueSerialize : JsonSerializerContext { }

    [JsonSerializable(typeof(List<VatCategory>))]
    [JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
    public partial class VatCategoriesSerialize : JsonSerializerContext { }


}
