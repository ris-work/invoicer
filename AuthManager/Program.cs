using Eto.Drawing;
using Eto.Forms;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Text;
using Tomlyn;

namespace AuthManager
{
    public static class Config
    {
        public static Tomlyn.Model.TomlTable model;
        public static void Initialize()
        {
            String TomlUnparsed = System.IO.File.ReadAllText("connstring.toml");
            model = Toml.ToModel(TomlUnparsed);
        }
    }

    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

            Config.Initialize();
            new Application(Eto.Platform.Detect).Run(new MainForm());
        }
    }

    public static class Utils
    {
        public static string DoPBKDF2(string Password)
        {
            byte[] salt = Encoding.UTF8.GetBytes(((String)Config.model["Salt"])); // divide by 8 to convert bits to bytes
            Console.WriteLine($"Salt: {Convert.ToBase64String(salt)}");

            // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: Password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));
            return hashed;
        }
    }
}
