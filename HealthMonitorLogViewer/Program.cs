using Eto.Drawing;
using Eto.Forms;
using System;

namespace HealthMonitor
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Config.LogFile = args[0];
            }
            Console.WriteLine($"LogFile: {Config.LogFile}");
            new Application(Eto.Platform.Detect).Run(new MainForm());
        }
    }
}

public static class Config
{
    public static string LogFile = "logs.sqlite3";
}