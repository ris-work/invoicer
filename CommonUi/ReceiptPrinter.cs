using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using RV.InvNew.CommonUi;
using EtoFE;

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
        return config.TryGetValue(key, out var value) && value is T typedValue ? typedValue : defaultValue;
    }

    public void PrintReceipt()
    {
        // Load settings from configuration
        float pageHeight = GetValueOrDefault("ReceiptPrinter.PageHeight", 600f); // Custom page height
        float pageWidth = GetValueOrDefault("ReceiptPrinter.PageWidth", 800f); // Custom page width
        string fontFamily = GetValueOrDefault("ReceiptPrinter.FontFamily", SystemFont.Default.ToString());
        float fontSize = GetValueOrDefault("ReceiptPrinter.FontSize", 10f);
        float lineHeight = GetValueOrDefault("ReceiptPrinter.LineHeight", 20f);

        currentIndex = 0; // Start from the first item
        morePages = true; // Assume multiple pages initially

        // Create a print document
        var printDocument = new PrintDocument();
        printDocument.PageCount = 1;

        printDocument.PrintPage += (sender, e) =>
        {
            Size pageSize = Size.Round(e.PageSize);

            // draw a border around the printable area
            var rect = new Rectangle(pageSize);
            float y = 0; // Start position for drawing content
            var font = new Font(fontFamily, fontSize);
            e.Graphics.DrawRectangle(Pens.Silver, rect);

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
            

            // Draw items for this page
            while (currentIndex < invoiceItems.Count && y + lineHeight < pageHeight)
            {
                var item = invoiceItems[currentIndex];
                var padding = 10;
                //string line = string.Join("", item.Take(item.Length - 1).Select(x => x.PadRight(padding))) + item.Last();
                int defaultPadding = 8; // Default padding if array size mismatch or padding not found

                // Extract the padding array dynamically
                var paddingConfig = config.TryGetValue("padding", out var paddingObj) && paddingObj is IEnumerable<int> paddingArray
                    ? paddingArray.ToList()
                    : new List<int>(); // Fallback to an empty list if "padding" isn't found or invalid

                // Build the output string
                string line = string.Join("", item.Select((value, index) =>
                {
                    // Use padding from the array if available, otherwise use default padding
                    int padding = index < paddingConfig.Count ? paddingConfig[index] : defaultPadding;

                    // Apply padding, except for the last element
                    return index < item.Length - 1 ? value.PadRightOrClamp(padding) : value;
                }));
                e.Graphics.DrawText(font, Colors.Black, new PointF(0, y), line);
                y += lineHeight;

                currentIndex++;
            }

            // Check if more pages are needed
            if (currentIndex < invoiceItems.Count)
            {
                morePages = true;
            }
            else
            {
                morePages = false;
                e.Graphics.DrawText(font, Colors.Black, new PointF(0, y), "=======================");
                y += lineHeight;

                e.Graphics.DrawText(font, Colors.Black, new PointF(0, y), "Thank you for your purchase!");
            }

            // If more pages are needed, call Print again
            //e.Cancel = morePages;
        };

        // Start printing
        printDocument.Print();
    }

    public static IReadOnlyDictionary<string, object> LoadConfig(string configPath)
    {
        var config = new Dictionary<string, object>
        {
            { "ReceiptPrinter.FontFamily", "Arial" },
            { "ReceiptPrinter.FontSize", 12f },
            { "ReceiptPrinter.LineHeight", 25f },
            { "ReceiptPrinter.PageHeight", 600f },
            { "ReceiptPrinter.PageWidth", 800f }
        };
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
            new string[] { "Item3", "5", "$50.00" }
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