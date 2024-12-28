using Eto.Drawing;
using Eto.Forms;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Text;
using Tomlyn;
using RV.InvNew.Common;

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
