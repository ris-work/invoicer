using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RV.LabelRetriever
{
    [JsonSerializable(typeof(ItemLabel))]
    public class ItemLabel
    {
        public long itemcode { get; set; }
        public string label_i18n_ta { get; set; }
        public string label_i18n_si { get; set; }
        public string label_i18n_default { get; set; }
        public Dictionary<string, (string, object, string?)> ToDict()
        {
            return new Dictionary<string, (string, object, string?)>()
        {
            { "Itemcode", ("Item code", this.itemcode, null) },
            { "DefaultDesc", ("Default Description", this.label_i18n_default, null) },
            { "ta", ("தமிழ்", this.label_i18n_ta, null) },
            { "si", ("සිංහල", this.label_i18n_si, null) },
        };
        }

        public static ItemLabel FromDictionary(Dictionary<string, object> DIn)
        {
            return new ItemLabel()
            {
                itemcode = (int)(long)DIn["Itemcode"],
                label_i18n_default = (string)DIn["DefaultDesc"],
                label_i18n_si = (string)DIn["ta"],
                label_i18n_ta = (string)DIn["si"],
            };
        }
    }

    public static class LabelRetriever
    {
        public static HttpClient HC = new HttpClient();
        public static List<ItemLabel> LIL = new List<ItemLabel>();
        public static Dictionary<long, (string Tamil, string Sinhala)> _I18nLabels = new();
        public static Dictionary<long, (string Default, string Tamil, string Sinhala)> _I18nLabelsWithDefault = new();
        public static Dictionary<long, ItemLabel> _I18nLabelsOriginal = new();
        public static IReadOnlyDictionary<long, (string Tamil, string Sinhala)> I18nLabels;
        public static IReadOnlyDictionary<long, (string Default, string Tamil, string Sinhala)> I18nLabelsWithDefault;
        public static IReadOnlyDictionary<long, ItemLabel> I18nLabelsOriginal;
        public static bool Initialized = false;
        public static string username = "i18n_user";
        public static string password = "i18n_saru";

        public static async Task<int> Initialize()
        {
            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            HC.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(byteArray)
            );
            //Console.WriteLine("Started requesting....");
            var strResp = HC.GetStringAsync("https://in.test.vz.al/i18n/sample.php")
                .GetAwaiter()
                .GetResult();
            //Console.WriteLine($"strResp: {strResp}");
            LIL = JsonSerializer.Deserialize<List<ItemLabel>>(strResp);
            //Console.WriteLine("Started looping....");
            foreach (ItemLabel IL in LIL)
            {
                //System.Console.WriteLine($"{IL.itemcode}, {IL.label_i18n_ta} ");
                _I18nLabels.Add(IL.itemcode, (IL.label_i18n_ta, IL.label_i18n_si));
            }
            foreach (ItemLabel IL in LIL)
            {
                //System.Console.WriteLine($"{IL.itemcode}, {IL.label_i18n_ta} ");
                _I18nLabelsWithDefault.Add(IL.itemcode, (IL.label_i18n_default, IL.label_i18n_ta, IL.label_i18n_si));
            }
            foreach (ItemLabel IL in LIL)
            {
                //System.Console.WriteLine($"{IL.itemcode}, {IL.label_i18n_ta} ");
                _I18nLabelsOriginal.Add(IL.itemcode, IL);
            }
            I18nLabels = _I18nLabels;
            I18nLabelsWithDefault = _I18nLabelsWithDefault;
            I18nLabelsOriginal = _I18nLabelsOriginal;
            Initialized = true;
            return 0;
        }

        public static string LookupTamil(string PLU_CODE)
        {
            if (!Initialized)
                Initialize().GetAwaiter().GetResult();
            return I18nLabels.GetValueOrDefault(int.Parse(PLU_CODE), ("", "")).Item1;
        }

        public static string LookupTamil(int Itemcode)
        {
            if (!Initialized)
                Initialize().GetAwaiter().GetResult();
            return I18nLabels.GetValueOrDefault(Itemcode, ("", "")).Item1;
        }

        public static string LookupSinhala(string PLU_CODE)
        {
            if (!Initialized)
                Initialize().GetAwaiter().GetResult();
            return I18nLabels.GetValueOrDefault(int.Parse(PLU_CODE), ("", "")).Item2;
        }

        public static string LookupSinhala(int Itemcode)
        {
            if (!Initialized)
                Initialize().GetAwaiter().GetResult();
            return I18nLabels.GetValueOrDefault(Itemcode, ("", "")).Item2;
        }
    }
}
