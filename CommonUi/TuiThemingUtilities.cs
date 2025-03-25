using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using Terminal.Gui;
using Tomlyn;
using Tomlyn.Model;

namespace CommonUi
{


    public static class TuiThemingUtilities
    {
        // Loads theming configuration from a TOML file
        public static Dictionary<string, object> LoadConfiguration(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("TOML configuration file not found.", filePath);
            }

            // Read and parse TOML content
            string tomlContent = File.ReadAllText(filePath);
            TomlTable tomlData = Toml.Parse(tomlContent).ToModel();

            // Convert to Dictionary for easy access
            var configDictionary = new Dictionary<string, object>();
            foreach (var key in tomlData.Keys)
            {
                configDictionary[key] = tomlData[key];
            }
            return configDictionary;
        }

        // Utility for parsing colors (returns default fallback if unsupported)
        public static Color ParseColor(string colorValue, Color defaultColor)
        {
            try
            {
                if (Enum.TryParse(colorValue, true, out Color parsedColor))
                {
                    return parsedColor;
                }
                return defaultColor; // Fallback for unsupported or invalid colors
            }
            catch
            {
                return defaultColor;
            }
        }

        // Applies the control theming (background and foreground colors)
        public static void ApplyControlColors(View view, Dictionary<string, object> config, string context, string control)
        {
            string backgroundKey = $"{context}.{control}.background_color";
            string foregroundKey = $"{context}.{control}.foreground_color";

            Color defaultBackground = Color.Black; // Default fallback
            Color defaultForeground = Color.White; // Default fallback

            // Retrieve background and foreground colors
            Color backgroundColor = config.TryGetValue(backgroundKey, out object backgroundColorValue) && backgroundColorValue is string backgroundColorString
                ? TuiThemingUtilities.ParseColor(backgroundColorString, defaultBackground)
                : defaultBackground;

            Color foregroundColor = config.TryGetValue(foregroundKey, out object foregroundColorValue) && foregroundColorValue is string foregroundColorString
                ? TuiThemingUtilities.ParseColor(foregroundColorString, defaultForeground)
                : defaultForeground;

            // Create a new ColorScheme instance
            view.ColorScheme = new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(foregroundColor, backgroundColor),
                Focus = new Terminal.Gui.Attribute(foregroundColor, backgroundColor),
                HotNormal = new Terminal.Gui.Attribute(foregroundColor, backgroundColor),
                HotFocus = new Terminal.Gui.Attribute(foregroundColor, backgroundColor)
            };
        }

        // Applies changed state colors (background and foreground)

        public static void ApplyChangedColors(View view, Dictionary<string, object> config, string context, string control)
        {
            string backgroundChangedKey = $"{context}.{control}.background_color_changed";
            string foregroundChangedKey = $"{context}.{control}.foreground_color_changed";

            Color defaultChangedBackground = Color.DarkGray; // Fallback for changed background
            Color defaultChangedForeground = Color.BrightYellow; // Fallback for changed foreground

            // Retrieve changed background and foreground colors
            Color changedBackgroundColor = config.TryGetValue(backgroundChangedKey, out object backgroundChangedColorValue) && backgroundChangedColorValue is string backgroundChangedColorString
                ? TuiThemingUtilities.ParseColor(backgroundChangedColorString, defaultChangedBackground)
                : defaultChangedBackground;

            Color changedForegroundColor = config.TryGetValue(foregroundChangedKey, out object foregroundChangedColorValue) && foregroundChangedColorValue is string foregroundChangedColorString
                ? TuiThemingUtilities.ParseColor(foregroundChangedColorString, defaultChangedForeground)
                : defaultChangedForeground;

            // Create a new ColorScheme for the changed state
            view.ColorScheme = new ColorScheme
            {
                Normal = view.ColorScheme.Normal, // Retain the current normal state
                Focus = view.ColorScheme.Focus, // Retain the current focused state
                HotNormal = new Terminal.Gui.Attribute(changedForegroundColor, changedBackgroundColor),
                HotFocus = new Terminal.Gui.Attribute(changedForegroundColor, changedBackgroundColor)
            };
        }

        // Configures control size based on TOML settings
        public static void ApplyControlSize(View view, Dictionary<string, object> config, string context, string control)
        {
            string sizeKey = $"{context}.{control}.control_size";

            if (config.TryGetValue(sizeKey, out object sizeValue) && sizeValue is string sizeString)
            {
                try
                {
                    string[] dimensions = sizeString.Split(',');
                    if (dimensions.Length == 2)
                    {
                        int width = int.Parse(dimensions[0]);
                        int height = int.Parse(dimensions[1]);
                        view.Width = width;
                        view.Height = height;
                    }
                }
                catch
                {
                    // Use default size if parsing fails (e.g., leave current size unchanged)
                }
            }
        }
    }
}
