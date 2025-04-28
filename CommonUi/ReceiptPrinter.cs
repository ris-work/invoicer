using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
//using EtoFE;
using RV.InvNew.CommonUi;
using Tomlyn.Model;

public class ReceiptPrinter
{
    private readonly List<string[]> invoiceItems;
    private readonly IReadOnlyDictionary<string, object> config;
    private int currentIndex; // Tracks the current item being printed
    private bool morePages; // Keeps track of whether more pages are needed

    public ReceiptPrinter(List<string[]> items, IReadOnlyDictionary<string, object> configuration)
    {
        invoiceItems = items;
        config = configuration;
    }

    private T GetValueOrDefault<T>(string key, T defaultValue)
    {
        return GetValueOrDefault(config, key, defaultValue);
    }

    private T GetValueOrDefault<T>(
        IReadOnlyDictionary<string, object> config,
        string key,
        T defaultValue
    )
    {
        var keys = key.Split('.'); // Split the dot-separated key
        object current = config;

        foreach (var part in keys)
        {
            if (
                current is IDictionary<string, object> dictionary
                && dictionary.TryGetValue(part, out var next)
            )
            {
                current = next; // Navigate deeper into the dictionary
            }
            else
            {
                Console.WriteLine($"Key not found: {key}"); // Log unsuccessful read
                return defaultValue;
            }
        }

        // Log successful read
        Console.WriteLine(
            $"Key found: {key}, Value: {current}, Value's type: {current.GetType()}, Default's type: {defaultValue.GetType()}"
        );

        // Attempt to cast the value
        if (current is T typedValue)
        {
            return typedValue;
        }
        else if (typeof(T) == typeof(float) && current is int intValue)
        {
            // Cast int to float when the target type is float
            return (T)(object)(float)intValue;
        }
        else if (typeof(T) == typeof(double) && current is int intValue_)
        {
            // Cast int to double when the target type is float
            return (T)(object)(double)intValue_;
        }
        else if (typeof(T) == typeof(float) && current is double doubleValue)
        {
            // Cast int to float when the target type is float
            return (T)(object)(float)doubleValue;
        }

        return defaultValue; // Return the default if casting fails
    }

    public void PrintReceipt()
    {
        // Load settings from configuration
        float pageHeight = GetValueOrDefault("ReceiptPrinter.PageHeight", 600f); // Custom page height
        float pageWidth = GetValueOrDefault("ReceiptPrinter.PageWidth", 800f); // Custom page width
        string fontFamily = GetValueOrDefault(
            "ReceiptPrinter.FontFamily",
            SystemFont.Default.ToString()
        );
        float fontSize = GetValueOrDefault("ReceiptPrinter.FontSize", 10f);
        float lineHeight = GetValueOrDefault("ReceiptPrinter.LineHeight", 20f);

        currentIndex = 0; // Start from the first item
        morePages = true; // Assume multiple pages initially

        // Create a print document
        var printDocument = new PrintDocument();
        printDocument.PageCount = 1;

        // --- Top Images Loading with Debug ---
        // Instead of inferring List<object>, we load as object first.
        object topImagesObj = GetValueOrDefault<object>(
            config,
            "ReceiptPrinter.images.top",
            new List<object>()
        );

        // Convert topImagesObj to an enumerable sequence.
        IEnumerable<object> topImagesEnumerable;
        if (topImagesObj is Tomlyn.Model.TomlArray tomlTop)
        {
            topImagesEnumerable = tomlTop.Cast<object>();
        }
        else if (topImagesObj is IEnumerable<object> enumerableTop)
        {
            topImagesEnumerable = enumerableTop;
        }
        else
        {
            topImagesEnumerable = Enumerable.Empty<object>();
        }

        var topImages = topImagesEnumerable
            .Select(img =>
            {
                // Each image should be a dictionary; if not, skip it.
                var imgDict = img as IDictionary<string, object>;
                if (imgDict == null)
                    return null;

                // Create the anonymous object.
                var topImg = new
                {
                    FilePath = imgDict["Path"].ToString(),
                    XPosition = imgDict["XPosition"].ToString(), // Expected: "left", "center", or "bottom" (interpreted as right)
                    YPosition = imgDict["YPosition"].ToString(), // Expected: "top" or "bottom"
                    Height = Convert.ToInt32(imgDict["Height"]),
                    Width = Convert.ToInt32(imgDict["Width"]),
                };

                // Debug: print the loaded top image.
                System.Console.WriteLine(
                    $"Top Image loaded: Path={topImg.FilePath}, XPosition={topImg.XPosition}, YPosition={topImg.YPosition}, Width={topImg.Width}, Height={topImg.Height}"
                );
                return topImg;
            })
            .Where(img => img != null)
            .ToList();

        // --- Bottom Images Loading with Debug ---
        object bottomImagesObj = GetValueOrDefault<object>(
            config,
            "ReceiptPrinter.images.bottom",
            new List<object>()
        );

        IEnumerable<object> bottomImagesEnumerable;
        if (bottomImagesObj is Tomlyn.Model.TomlArray tomlBottom)
        {
            bottomImagesEnumerable = tomlBottom.Cast<object>();
        }
        else if (bottomImagesObj is IEnumerable<object> enumerableBottom)
        {
            bottomImagesEnumerable = enumerableBottom;
        }
        else
        {
            bottomImagesEnumerable = Enumerable.Empty<object>();
        }

        var bottomImages = bottomImagesEnumerable
            .Select(img =>
            {
                var imgDict = img as IDictionary<string, object>;
                if (imgDict == null)
                    return null;

                var bottomImg = new
                {
                    FilePath = imgDict["Path"].ToString(),
                    XPosition = imgDict["XPosition"].ToString(), // Expected: "left", "center", or "bottom" (interpreted as right)
                    YPosition = imgDict["YPosition"].ToString(), // Expected: "top" or "bottom"
                    Height = Convert.ToInt32(imgDict["Height"]),
                    Width = Convert.ToInt32(imgDict["Width"]),
                };

                System.Console.WriteLine(
                    $"Bottom Image loaded: Path={bottomImg.FilePath}, XPosition={bottomImg.XPosition}, YPosition={bottomImg.YPosition}, Width={bottomImg.Width}, Height={bottomImg.Height}"
                );
                return bottomImg;
            })
            .Where(img => img != null)
            .ToList();

        printDocument.PrintPage += (sender, e) =>
        {
            Size pageSize = Size.Round(e.PageSize);

            // draw a border around the printable area
            //var rect = new Rectangle(pageSize);
            float y = 0; // Start position for drawing content
            var font = new Font(fontFamily, fontSize);
            //e.Graphics.DrawRectangle(Pens.Silver, rect);

            // draw title
            e.Graphics.DrawText(font, Colors.Black, new Point(50, 20), "document.Name");

            // Enforce custom page boundaries
            var bounds = new RectangleF(0, 0, pageWidth, pageHeight);
            e.Graphics.SetClip(bounds);

            // Header for the receipt
            e.Graphics.DrawText(font, Colors.Black, new PointF(0, y), "Receipt");
            y += lineHeight;

            e.Graphics.DrawText(font, Colors.Black, new PointF(0, y), "=======================");
            y += lineHeight;
            e.Graphics.DrawText(
                font,
                Colors.LightGrey,
                new PointF(0, y),
                "======================="
            );
            y += lineHeight;

            e.Graphics.DrawText(font, Colors.Gray, new PointF(0, y), "=======================");
            y += lineHeight;
            e.Graphics.DrawText(font, Colors.DarkGray, new PointF(0, y), "=======================");
            y += lineHeight;
            e.Graphics.FillRectangle(Colors.Black, 0, y, pageWidth, 100);
            e.Graphics.DrawText(font, Colors.White, new PointF(0, y), "WHITE ON BLACK");
            y += 100;
            y += lineHeight;
            // ---------- Top Images Processing (Header) ----------
            float topOffset = y; // starting y-position for top images
            foreach (var image in topImages)
            {
                // Convert relative FilePath to an absolute path
                string imagePath = image.FilePath;
                if (!Path.IsPathRooted(imagePath))
                {
                    imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
                }
                System.Console.WriteLine($"Top image final path: {imagePath}");
                if (!File.Exists(imagePath))
                {
                    System.Console.WriteLine($"Top image file not found: {imagePath}");
                    continue;
                }

                // Calculate horizontal position based on XPosition alignment
                float imgX = 0;
                if (image.XPosition.Equals("left", StringComparison.OrdinalIgnoreCase))
                    imgX = 0;
                else if (image.XPosition.Equals("center", StringComparison.OrdinalIgnoreCase))
                    imgX = (pageWidth - image.Width) / 2;
                else if (image.XPosition.Equals("bottom", StringComparison.OrdinalIgnoreCase))
                    imgX = pageWidth - image.Width; // "bottom" is interpreted as right-aligned
                else
                    imgX = 0;

                // For top images, Y is defined by our running topOffset.
                float imgY = topOffset;

                using (var bmp = new Bitmap(imagePath))
                {
                    e.Graphics.DrawImage(
                        bmp,
                        new RectangleF(imgX, imgY, image.Width, image.Height)
                    );
                }

                // Increment topOffset by the image's height plus a spacing gap.
                topOffset += image.Height + 10;
            }
            y = topOffset;

            // Draw items for this page
            while (currentIndex < invoiceItems.Count && y + lineHeight < pageHeight)
            {
                var item = invoiceItems[currentIndex];
                int defaultPadding = 80; // Default padding if array size mismatch or padding not found

                // Extract the padding array dynamically
                var paddingConfig =
                    config.TryGetValue("padding", out var paddingObj)
                    && paddingObj is TomlArray paddingArray
                        ? paddingArray.Select(value => Convert.ToInt32(value)).ToList()
                        : new List<int>(); // Fallback to an empty list if "padding" isn't found or invalid

                float x = 0; // Starting X position for the first column

                for (int i = 0; i < item.Length; i++)
                {
                    string value = item[i];

                    // Determine the padding for the current column
                    int columnPadding = i < paddingConfig.Count ? paddingConfig[i] : defaultPadding;
                    System.Console.WriteLine(
                        $"Itertation: {i}, Padding: {columnPadding}, Supplied paddings: {paddingConfig.Count}"
                    );

                    // Initialize the truncated value
                    string truncatedValue = value;

                    // Measure the text size
                    var textSize = e.Graphics.MeasureString(font, truncatedValue);

                    // Dynamically truncate the string until it visually fits within the column
                    while (textSize.Width > columnPadding && truncatedValue.Length > 0)
                    {
                        truncatedValue = truncatedValue.Substring(0, truncatedValue.Length - 1);
                        textSize = e.Graphics.MeasureString(font, truncatedValue);
                    }

                    // Ensure only the truncated text is drawn
                    if (x + columnPadding <= pageWidth) // Respect page boundaries
                    {
                        e.Graphics.DrawText(font, Colors.Black, new PointF(x, y), truncatedValue);
                    }

                    // Increment the X position for the next column
                    x += columnPadding; // Advance X by the current column's width
                }

                y += lineHeight; // Move to the next row
                currentIndex++;
            }
            // ---------- Bottom Images Processing (Footer) ----------
            float bottomOffset = pageHeight; // starting from the bottom of the page
            // Process bottom images in reverse order so that they stack upward.
            foreach (var image in bottomImages.AsEnumerable().Reverse())
            {
                // Convert relative FilePath to an absolute path
                string imagePath = image.FilePath;
                if (!Path.IsPathRooted(imagePath))
                {
                    imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
                }
                System.Console.WriteLine($"Bottom image final path: {imagePath}");
                if (!File.Exists(imagePath))
                {
                    System.Console.WriteLine($"Bottom image file not found: {imagePath}");
                    continue;
                }

                // Calculate horizontal position based on XPosition alignment
                float imgX = 0;
                if (image.XPosition.Equals("left", StringComparison.OrdinalIgnoreCase))
                    imgX = 0;
                else if (image.XPosition.Equals("center", StringComparison.OrdinalIgnoreCase))
                    imgX = (pageWidth - image.Width) / 2;
                else if (image.XPosition.Equals("bottom", StringComparison.OrdinalIgnoreCase))
                    imgX = pageWidth - image.Width;
                else
                    imgX = 0;

                // For bottom images, anchor them from the bottom upward.
                float imgY = 0;
                if (image.YPosition.Equals("bottom", StringComparison.OrdinalIgnoreCase))
                {
                    bottomOffset -= image.Height;
                    imgY = bottomOffset;
                    bottomOffset -= 10; // additional spacing between images
                }
                else if (image.YPosition.Equals("top", StringComparison.OrdinalIgnoreCase))
                {
                    bottomOffset -= image.Height;
                    imgY = bottomOffset;
                    bottomOffset -= 10;
                }
                else
                {
                    bottomOffset -= image.Height;
                    imgY = bottomOffset;
                    bottomOffset -= 10;
                }

                using (var bmp = new Bitmap(imagePath))
                {
                    e.Graphics.DrawImage(
                        bmp,
                        new RectangleF(imgX, imgY, image.Width, image.Height)
                    );
                }
            }

            // Check if more pages are needed
            if (currentIndex < invoiceItems.Count)
            {
                morePages = true;
            }
            else
            {
                morePages = false;
                e.Graphics.DrawText(
                    font,
                    Colors.Black,
                    new PointF(0, y),
                    "======================="
                );
                y += lineHeight;

                e.Graphics.DrawText(
                    font,
                    Colors.Black,
                    new PointF(0, y),
                    "Thank you for your purchase!"
                );
            }

            // If more pages are needed, call Print again
            //e.Cancel = morePages;
        };

        // Start printing
        printDocument.Print();
        System.Console.WriteLine("Press any key to proceed");
        System.Console.ReadKey();
    }

    public static IReadOnlyDictionary<string, object> LoadConfig(string configPath)
    {
        var config = new Dictionary<string, object>
        {
            { "ReceiptPrinter.FontFamily", "Arial" },
            { "ReceiptPrinter.FontSize", 12f },
            { "ReceiptPrinter.LineHeight", 25f },
            { "ReceiptPrinter.PageHeight", 600f },
            { "ReceiptPrinter.PageWidth", 800f },
        };
        System.Console.WriteLine("Sample config:");
        System.Console.WriteLine(Tomlyn.Toml.FromModel(config));
        if (System.IO.File.Exists("theme.toml"))
            config = Tomlyn.Toml.ToModel(System.IO.File.ReadAllText("theme.toml")).ToDictionary();
        System.Console.WriteLine("Using config:");
        System.Console.WriteLine(Tomlyn.Toml.FromModel(config));
        System.IO.File.WriteAllText("current_config.toml", Tomlyn.Toml.FromModel(config));

        return config;
    }
}

// Example usage
public class Program
{
    [STAThread]
    public static void Main()
    {
        var config = ReceiptPrinter.LoadConfig("theme.toml");

        var invoiceItems = new List<string[]>
        {
            new string[] { "Item1", "2", "$20.00" },
            new string[] { "Item2", "1", "$10.00" },
            new string[] { "Item3", "5", "$50.00" },
            // Add more items if needed
        };

        var receiptPrinter = new ReceiptPrinter(invoiceItems, config);
        receiptPrinter.PrintReceipt();
    }
}


// Example usage
/*
public class Program
{
    [STAThread]
    public static void Main()
    {
        // Load configuration from theme.toml
        var config = ReceiptPrinter.LoadConfig("theme.toml");

        var invoiceItems = new List<string[]>
        {
            new string[] { "Item1", "2", "$20.00" },
            new string[] { "Item2", "1", "$10.00" },
            new string[] { "Item3", "5", "$50.00" }
        };

        var receiptPrinter = new ReceiptPrinter(invoiceItems, config);
        receiptPrinter.PrintReceipt();
    }
}
*/
