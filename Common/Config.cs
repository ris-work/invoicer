using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Tomlyn;

namespace RV.InvNew.Common
{
    public static class Config
    {
        public static Tomlyn.Model.TomlTable model;
        public static IReadOnlyDictionary<string, object> modelDict;

        public static void Initialize()
        {
            Console.WriteLine(
                "[common] Current Directory: {0}",
                System.IO.Directory.GetCurrentDirectory()
            );
            String TomlUnparsed = System.IO.File.ReadAllText("connstring.toml");
            model = Toml.ToModel(TomlUnparsed);
            modelDict = (IReadOnlyDictionary<string, object>)model.ToDictionary();
        }

        public static string GetCWD()
        {
            return Directory.GetCurrentDirectory();
        }
    }

    public static class Utils
    {
        public static string DoPBKDF2(string Password)
        {
            byte[] salt = Encoding.UTF8.GetBytes(((String)Config.model["Salt"])); // divide by 8 to convert bits to bytes
            Console.WriteLine($"Salt: {Convert.ToBase64String(salt)}");

            // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
            string hashed = Convert.ToBase64String(
                KeyDerivation.Pbkdf2(
                    password: Password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 100000,
                    numBytesRequested: 256 / 8
                )
            );
            return hashed;
        }
    }
}
