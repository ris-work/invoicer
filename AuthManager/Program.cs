using System;
using System.Text;
using Eto.Drawing;
using Eto.Forms;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using RV.InvNew.Common;
using Tomlyn;

namespace RV.InvNew.AuthManager
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Config.Initialize();
            new Application(Eto.Platform.Detect).Run(new MainForm());
        }
    }
}
