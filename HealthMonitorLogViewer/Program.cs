using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using ScottPlot;

namespace HealthMonitor
{
    public static class PlotUtils
    {
        public static readonly MarkerShape[] AllMarkers = new MarkerShape[]
        {
            MarkerShape.Asterisk,
            MarkerShape.FilledCircle,
            MarkerShape.FilledTriangleUp,
            MarkerShape.Cross,
            MarkerShape.FilledTriangleDown,
        };

        public static MarkerShape GetRandomMarkerShape()
        {
            Random random = new Random();
            int MarkerIndex = random.Next(AllMarkers.Length);
            return AllMarkers[MarkerIndex];
        }

        public static readonly LinePattern[] AllLinePatterns = new LinePattern[]
        {
            LinePattern.Solid,
            LinePattern.Dashed,
            LinePattern.DenselyDashed,
        };

        public static LinePattern GetRandomLinePattern()
        {
            Random random = new Random();
            int LinePatternIndex = random.Next(AllLinePatterns.Length);
            return AllLinePatterns[LinePatternIndex];
        }
    }

    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Config.Platform = Eto.Platform.Detect.ToString();
            Config.PNGFilter.Extensions = new string[] { "*.png" };
            Config.PNGFilter.Name = "Portable Network Graphics (*.png)";
            Config.SVGFilter.Extensions = new string[] { "*.svg" };
            Config.SVGFilter.Name = "Scalable Vector Graphics (*.svg)";
            if (args.Length > 0)
            {
                Config.LogFile = args[0];
            }
            if (args.Length > 1)
            {
                Config.Platform = args[1];
            }
            Console.WriteLine($"LogFile: {Config.LogFile}");
            new Application(Eto.Platform.Detect).Run(new MainForm());
        }
    }
}

public static class Config
{
    public static string LogFile = "logs.sqlite3.rvhealthmonitorlogfile";
    public static string Platform = Eto.Platform.Detect.ToString();
    public static Eto.Forms.FileFilter PNGFilter = new Eto.Forms.FileFilter();
    public static Eto.Forms.FileFilter SVGFilter = new Eto.Forms.FileFilter();
}
