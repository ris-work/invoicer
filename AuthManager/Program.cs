using Eto.Drawing;
using Eto.Forms;
using System;
using Tomlyn;

namespace AuthManager
{

    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            String TomlUnparsed = System.IO.File.ReadAllText("connstring.toml");
            var model = Toml.ToModel(TomlUnparsed);
            
            new Application(Eto.Platform.Detect).Run(new MainForm());
        }
    }
}
