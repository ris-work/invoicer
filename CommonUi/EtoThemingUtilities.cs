using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn.Model;
using Eto.Forms;
using Eto.Drawing;

namespace CommonUi
{
    public static class EtoThemingUtilities
    {
        public static Color GetNestedColor(IReadOnlyDictionary<string, object> config, string context, string control, string colorType, Color defaultColor)
        {
            // Check if "context" exists and is a nested table
            if (config.TryGetValue(context, out object contextTable) && contextTable is TomlTable contextDict)
            {
                // Check if "control" exists within "context" and is a nested table
                if (contextDict.TryGetValue(control, out object controlTable) && controlTable is TomlTable controlDict)
                {
                    // Check if the colorType key exists within "control"
                    if (controlDict.TryGetValue(colorType, out object value) && value is string colorValue)
                    {
                        try
                        {
                            // Parse the color if it's valid
                            System.Console.WriteLine($"Passed @3, {context}.{control}.{colorType}, {controlDict?.GetType()}");
                            return Color.Parse(colorValue); // Parses hex or named colors into Eto.Drawing.Color
                            
                        }
                        catch
                        {
                            // If parsing fails, fall back to the default color
                            return defaultColor;
                        }
                    }
                    else
                    {
                        System.Console.WriteLine($"Failed @3, {controlDict?.GetType()}");
                    }
                }
                else
                {
                     System.Console.WriteLine($"Failed @2, {controlTable?.GetType()}");
                }
            }
            else
            {
                System.Console.WriteLine($"Failed @1, {contextTable?.GetType()}");
            }

            // Fall back to the default color if the key is not found
            return defaultColor;
        }
        public static Size GetNestedSize(IReadOnlyDictionary<string, object> config, string context, string control, string sizeType, Size defaultSize)
        {
            // Check if "context" exists and is a nested table
            if (config.TryGetValue(context, out object contextTable) && contextTable is TomlTable contextDict)
            {
                // Check if "control" exists within "context" and is a nested table
                if (contextDict.TryGetValue(control, out object controlTable) && controlTable is TomlTable controlDict)
                {
                    // Check if the sizeType key exists within "control"
                    if (controlDict.TryGetValue(sizeType, out object value) && value is string sizeValue)
                    {
                        try
                        {
                            // Parse the size string, assuming format "Width,Height" (e.g., "12,18")
                            string[] sizeParts = sizeValue.Split(',');
                            int width = int.Parse(sizeParts[0]);
                            int height = int.Parse(sizeParts[1]);

                            return new Size(width, height);
                        }
                        catch
                        {
                            // If parsing fails, fall back to the default size
                            return defaultSize;
                        }
                    }
                }
            }

            // Fall back to the default size if the key is not found
            return defaultSize;
        }

        public static Font GetNestedFont(IReadOnlyDictionary<string, object> config, string context, string control, Font defaultFont)
        {
            // Check if "context" exists and is a nested table
            if (config.TryGetValue(context, out object contextTable) && contextTable is TomlTable contextDict)
            {
                // Check if "control" exists within "context" and is a nested table
                if (contextDict.TryGetValue(control, out object controlTable) && controlTable is TomlTable controlDict)
                {
                    try
                    {
                        // Retrieve font attributes
                        string family = controlDict.TryGetValue("font_family", out object familyValue) && familyValue is string fontFamily
                            ? fontFamily
                            : defaultFont.Family.Name;

                        float size = controlDict.TryGetValue("font_size", out object sizeValue) && float.TryParse(sizeValue.ToString(), out float fontSize)
                            ? fontSize
                            : defaultFont.Size;

                        FontStyle style = controlDict.TryGetValue("font_style", out object styleValue) && Enum.TryParse(styleValue.ToString(), true, out FontStyle fontStyle)
                            ? fontStyle
                            : defaultFont.FontStyle;

                        // Create and return the font
                        return new Font(new FontFamily(family), size, style);
                    }
                    catch
                    {
                        // If anything fails, return the default font
                        return defaultFont;
                    }
                }
            }

            // Fall back to the default font if the key is not found
            return defaultFont;
        }
    }
}
