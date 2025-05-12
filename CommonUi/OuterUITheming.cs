using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Eto.Drawing;
#if WINDOWS
using System.Windows;
#endif

namespace CommonUi
{
    /// <summary>
    /// A universal class for the application’s color settings. It reads from the deserialized TOML configuration
    /// (for example, Program.ConfigDict) and exposes Eto.Drawing.Color properties for the entire program lifecycle.
    /// </summary>
    public static class ColorSettings
    {
        public static Color AlternatingColor1 { get; private set; }
        public static Color AlternatingColor2 { get; private set; }
        public static Color ForegroundColor { get; private set; }
        public static Color BackgroundColor { get; private set; }
        public static Color SelectedColumnColor { get; private set; }
        public static Color LesserBackgroundColor { get; private set; }
        public static Color LesserForegroundColor { get; private set; }
#if WINDOWS
        // New properties that return WPF brushes directly
        public static System.Windows.Media.SolidColorBrush BackgroundBrush
            => new System.Windows.Media.SolidColorBrush(BackgroundColor.ToMediaColor());

        public static System.Windows.Media.SolidColorBrush ForegroundBrush
            => new System.Windows.Media.SolidColorBrush(ForegroundColor.ToMediaColor());
        public static System.Windows.Media.SolidColorBrush LesserBackgroundBrush
            => new System.Windows.Media.SolidColorBrush(LesserBackgroundColor.ToMediaColor());

        public static System.Windows.Media.SolidColorBrush LesserForegroundBrush
            => new System.Windows.Media.SolidColorBrush(LesserForegroundColor.ToMediaColor());

        public static System.Windows.Media.Color ToMediaColor(this Eto.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb((byte)color.Ab, (byte)color.Rb, (byte)color.Gb, (byte)color.Bb);
        }
#endif

        /// <summary>
        /// Initializes the global color settings from the provided configuration dictionary.
        /// </summary>
        /// <param name="configDict">The deserialized TOML configuration as a dictionary.</param>
        public static void Initialize(IReadOnlyDictionary<string, object> configDict)
        {
            AlternatingColor1 = ColorParser.ParseColor(
                (string)configDict.GetValueOrDefault("AlternatingColor1", "Brown")
            );
            AlternatingColor2 = ColorParser.ParseColor(
                (string)configDict.GetValueOrDefault("AlternatingColor2", "Black")
            );
            ForegroundColor = ColorParser.ParseColor(
                (string)configDict.GetValueOrDefault("ForegroundColor", "White")
            );
            BackgroundColor = ColorParser.ParseColor(
                (string)configDict.GetValueOrDefault("BackgroundColor", "Black")
            );
            SelectedColumnColor = ColorParser.ParseColor(
                (string)configDict.GetValueOrDefault("SelectedColumnColor", "SlateBlue")
            );
            LesserBackgroundColor = ColorParser.ParseColor(
                (string)configDict.GetValueOrDefault("LesserBackgroundColor", "SlateBlue")
            );
            LesserForegroundColor = ColorParser.ParseColor(
                (string)configDict.GetValueOrDefault("LesserForegroundColor", "SlateBlue")
            );
        }

        ///<summary>
        ///Dump universal settings
        ///</summary>
        public static void Dump()
        {
            System.Console.WriteLine(
                $"Alt1: {AlternatingColor1}, Alt2: {AlternatingColor2}, Sel: {SelectedColumnColor}, FG: {ForegroundColor}, BG: {BackgroundColor}, LFG: {LesserForegroundColor}, LBG: {LesserBackgroundColor}."
            );
        }

        /// <summary>
        /// Retrieves panel-specific color settings.
        /// Expects a section in the TOML like:
        ///
        /// [panel1]
        /// ForegroundColor=xxx
        /// BackgroundColor=xxx
        ///
        /// </summary>
        /// <param name="panelName">The panel’s section name.</param>
        /// <param name="configDict">The root configuration dictionary.</param>
        /// <returns>A PanelSettings instance with parsed colors, or null if the panel is not defined.</returns>
        public static PanelSettings GetPanelSettings(
            string panelName,
            IReadOnlyDictionary<string, object> configDict
        )
        {
            if (
                configDict.TryGetValue(panelName, out object panelConfigObj)
                && panelConfigObj is IDictionary<string, object> panelConfig
            )
            {
                PanelSettings panelSettings = new PanelSettings();

                if (panelConfig.ContainsKey("ForegroundColor"))
                {
                    panelSettings.ForegroundColor = ColorParser.ParseColor(
                        panelConfig["ForegroundColor"].ToString()
                    );
                }
                else
                {
                    Console.WriteLine($"Per panel colours: {panelConfig.ToString()} ForegroundColor not found");
                }
                if (panelConfig.ContainsKey("BackgroundColor"))
                {
                    panelSettings.BackgroundColor = ColorParser.ParseColor(
                        panelConfig["BackgroundColor"].ToString()
                    );
                }
                else
                {
                    Console.WriteLine($"Per panel colours: {panelConfig.ToString()} BackgroundColor not found");
                }
                if (panelConfig.ContainsKey("LesserBackgroundColor"))
                {
                    panelSettings.LesserBackgroundColor = ColorParser.ParseColor(
                        panelConfig["LesserBackgroundColor"].ToString()
                    );
                }
                else
                {
                    Console.WriteLine($"Per panel colours: {panelConfig.ToString()} LesserBackgroundColor not found");
                }
                if (panelConfig.ContainsKey("LesserForegroundColor"))
                {
                    panelSettings.LesserForegroundColor = ColorParser.ParseColor(
                        panelConfig["LesserForegroundColor"].ToString()
                    );
                }
                else
                {
                    Console.WriteLine($"Per panel colours: {panelConfig.ToString()} LesserForegroundColor not found");
                }
                if (panelConfig.ContainsKey("AlternatingColor1"))
                {
                    panelSettings.AlternatingColor1 = ColorParser.ParseColor(
                        panelConfig["AlternatingColor1"].ToString()
                    );
                }
                else
                {
                    Console.WriteLine($"Per panel colours: {panelConfig.ToString()} AlternatingColor1 not found");
                }
                if (panelConfig.ContainsKey("AlternatingColor2"))
                {
                    panelSettings.AlternatingColor2 = ColorParser.ParseColor(
                        panelConfig["AlternatingColor2"].ToString()
                    );
                }
                else
                {
                    Console.WriteLine($"Per panel colours: {panelConfig.ToString()} AlternatingColor2 not found");
                }
                if (panelConfig.ContainsKey("SelectedColumnColor"))
                {
                    panelSettings.SelectedColumnColor = ColorParser.ParseColor(
                        panelConfig["SelectedColumnColor"].ToString()
                    );
                }
                else
                {
                    Console.WriteLine($"Per panel colours: {panelConfig.ToString()} SelectedColumnColor not found");
                }
                return panelSettings;
            }
            return null;
        }
    }

    /// <summary>
    /// Holds per‑panel color settings.
    /// </summary>
    public class PanelSettings
    {
        public Color ForegroundColor { get; set; }
        public Color BackgroundColor { get; set; }
        public Color AlternatingColor1 { get; set; }
        public Color AlternatingColor2 { get; set; }
        public Color SelectedColumnColor { get; set; }
        public Color LesserBackgroundColor { get; set; }
        public Color LesserForegroundColor { get; set; }
    }

    /// <summary>
    /// Helper class to convert a string in various formats to an Eto.Drawing.Color.
    /// Supported formats:
    ///  • Tuple: (r,g,b) or (r, g, b, a)
    ///  • Hex: #rgb, #rgba, #rrggbb, #rrggbbaa
    ///  • Color name from Eto.Drawing.Colors (e.g., "Red", "Blue")
    /// </summary>
    public static class ColorParser
    {
        public static Color ParseColor(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Color string is null or empty.");

            input = input.Trim();

            // Tuple format: (r, g, b) or (r, g, b, a)
            if (input.StartsWith("(") && input.EndsWith(")"))
            {
                string inner = input.Substring(1, input.Length - 2);
                var parts = inner.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3 || parts.Length > 4)
                    throw new FormatException(
                        "Invalid tuple format for color. Expected (r,g,b) or (r,g,b,a)."
                    );

                int r = int.Parse(parts[0].Trim());
                int g = int.Parse(parts[1].Trim());
                int b = int.Parse(parts[2].Trim());
                int a = 255;
                if (parts.Length == 4)
                {
                    a = int.Parse(parts[3].Trim());
                }
                return Color.FromArgb(r, g, b, a);
            }

            // Hex format (starting with #)
            if (input.StartsWith("#"))
            {
                string hex = input.Substring(1);
                if (hex.Length == 3) // #rgb
                {
                    string rHex = new string(hex[0], 2);
                    string gHex = new string(hex[1], 2);
                    string bHex = new string(hex[2], 2);
                    int r = int.Parse(rHex, NumberStyles.HexNumber);
                    int g = int.Parse(gHex, NumberStyles.HexNumber);
                    int b = int.Parse(bHex, NumberStyles.HexNumber);
                    return Color.FromArgb(r, g, b, 255);
                }
                if (hex.Length == 4) // #rgba
                {
                    string rHex = new string(hex[0], 2);
                    string gHex = new string(hex[1], 2);
                    string bHex = new string(hex[2], 2);
                    string aHex = new string(hex[3], 2);
                    int r = int.Parse(rHex, NumberStyles.HexNumber);
                    int g = int.Parse(gHex, NumberStyles.HexNumber);
                    int b = int.Parse(bHex, NumberStyles.HexNumber);
                    int a = int.Parse(aHex, NumberStyles.HexNumber);
                    return Color.FromArgb(r, g, b, a);
                }
                if (hex.Length == 6) // #rrggbb
                {
                    int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                    int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                    int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                    return Color.FromArgb(r, g, b, 255);
                }
                if (hex.Length == 8) // #rrggbbaa
                {
                    int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                    int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                    int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                    int a = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
                    return Color.FromArgb(r, g, b, a);
                }
                throw new FormatException("Invalid hex color format.");
            }

            // Assume the input is a named color from Eto.Drawing.Colors.
            // This uses reflection to find a matching public static property for the color.
            PropertyInfo colorProperty = typeof(Colors).GetProperty(
                input,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase
            );
            if (colorProperty != null)
            {
                return (Color)colorProperty.GetValue(null, null);
            }

            throw new FormatException($"Unable to parse color from input: {input}");
        }
    }
}
