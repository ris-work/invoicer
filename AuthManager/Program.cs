using Eto.Drawing;
using Eto.Forms;
using System;
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

    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

            Config.Initialize();
            new Application(Eto.Platform.Detect).Run(new MainForm());
        }
    }
}
