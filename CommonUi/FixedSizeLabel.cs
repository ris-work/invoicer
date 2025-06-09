using System;
using Eto.Drawing;
using Eto.Forms;

public class CustomLabel : Drawable
{
    private string text;
    private Font font;

    // Cached formatted text
    private FormattedText cachedFormattedText;
    private SizeF cachedMeasuredSize;
    private float cachedMaxWidth;

    // Cached rendered bitmap (graphics)
    private Bitmap cachedBitmap;
    private Size cachedBitmapSize;

    /// <summary>
    /// The text to display.
    /// </summary>
    public string Text
    {
        get => text;
        set
        {
            if (text != value)
            {
                text = value;
                InvalidateCache();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// The font used by the label; defaults to new Font(FontFamilies.Sans, 12).
    /// </summary>
    public Font Font
    {
        get => font ?? (font = new Font(FontFamilies.Sans, 10));
        set
        {
            if (font != value)
            {
                font = value;
                InvalidateCache();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// The foreground (text) color.
    /// </summary>
    public Color ForegroundColor { get; set; } = CommonUi.ColorSettings.ForegroundColor;

    /// <summary>
    /// The background color.
    /// </summary>
    //public Color BackgroundColor { get; set; } = Colors.Transparent;
    public Color BackgroundColor { get; set; } = CommonUi.ColorSettings.BackgroundColor;

    /// <summary>
    /// Horizontal text alignment.
    /// </summary>
    public FormattedTextAlignment TextAlignment { get; set; } = FormattedTextAlignment.Left;

    /// <summary>
    /// Vertical text alignment.
    /// </summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Center;

    public CustomLabel()
    {
        BackgroundColor = CommonUi.ColorSettings.BackgroundColor;

        // Invalidate caches when the control is resized.
        SizeChanged += (sender, e) =>
        {
            InvalidateCache();
            Console.Error.WriteLine("SizeChanged: Cache invalidated due to size change.");
        };
    }

    /// <summary>
    /// Invalidate all caches (formatted text and rendered graphics).
    /// </summary>
    private void InvalidateCache()
    {
        // Invalidate cached layout.
        cachedFormattedText = null;

        // Dispose and invalidate cached graphics.
        if (cachedBitmap != null)
        {
            cachedBitmap.Dispose();
            cachedBitmap = null;
        }
    }

    /// <summary>
    /// Ensures the cached formatted text and its measured size are updated with the given available width.
    /// </summary>
    /// <param name="availableWidth">The available width for layout.</param>
    private void EnsureFormattedTextCache(float availableWidth)
    {
        // Recreate the formatted text if missing or if available width has changed significantly.
        if (cachedFormattedText == null || Math.Abs(cachedMaxWidth - availableWidth) > 0.1f)
        {
            var ft = new FormattedText
            {
                Text = Text ?? "",
                Font = Font,
                ForegroundBrush = new SolidBrush(ForegroundColor),
                // Set the maximum width so that text is trimmed appropriately.
                MaximumWidth = availableWidth,
                Wrap = FormattedTextWrapMode.None, // Disable wrapping; we want a single line.
                Trimming = FormattedTextTrimming.WordEllipsis,
                Alignment = TextAlignment,
            };

            cachedFormattedText = ft;
            cachedMeasuredSize = ft.Measure();
            cachedMaxWidth = availableWidth;
            Console.Error.WriteLine("FormattedText cache rebuilt with width: {0}", availableWidth);
        }
    }

    /// <summary>
    /// Ensures the cached bitmap (graphics) is valid for the current control bounds.
    /// If not, it renders the control’s graphics into a new Bitmap.
    /// </summary>
    /// <param name="controlRect">The current control rectangle.</param>
    private void EnsureGraphicsCache(RectangleF controlRect)
    {
        // Recreate the cached bitmap if the bitmap size does not match the control's size.
        if (cachedBitmap == null || cachedBitmapSize != (Size)controlRect.Size)
        {
            // Update the formatted text cache (which also sets cachedMeasuredSize).
            EnsureFormattedTextCache(controlRect.Width);

            // Create a new bitmap with the current control's dimensions.
            int bmpWidth = (int)controlRect.Width;
            int bmpHeight = (int)controlRect.Height;
            cachedBitmap = new Bitmap(bmpWidth, bmpHeight, PixelFormat.Format24bppRgb);
            cachedBitmapSize = (Size)controlRect.Size;
            Console.Error.WriteLine(
                "Graphics cache rebuilt with size: {0}x{1}",
                bmpWidth,
                bmpHeight
            );

            using (var bmpGraphics = new Graphics(cachedBitmap))
            {
                // Save the current transform state.
                using (var state = bmpGraphics.SaveTransformState())
                {
                    // The transform is now in a known state.
                    // Set anti-aliasing and other graphics settings as needed.
                    bmpGraphics.AntiAlias = false; // or AntiAlias.Off if preferred

                    // Set the clipping region to the full bitmap area.
                    bmpGraphics.SetClip(new RectangleF(PointF.Empty, controlRect.Size));

                    // Draw the background.
                    using (var bgBrush = new SolidBrush(BackgroundColor))
                    {
                        bmpGraphics.FillRectangle(
                            bgBrush,
                            new RectangleF(PointF.Empty, controlRect.Size)
                        );
                    }

                    // Compute vertical offset based on vertical alignment.
                    float y = 0;
                    if (VerticalAlignment == VerticalAlignment.Center)
                        y = (controlRect.Height - cachedMeasuredSize.Height) / 2;
                    else if (VerticalAlignment == VerticalAlignment.Bottom)
                        y = (controlRect.Height - cachedMeasuredSize.Height);

                    // Draw the cached formatted text at (0, y).
                    bmpGraphics.DrawText(
                        font,
                        new SolidBrush(ForegroundColor),
                        new RectangleF(new PointF(0, y), new PointF(Width, Height)),
                        cachedFormattedText.Text,
                        cachedFormattedText.Wrap,
                        cachedFormattedText.Alignment,
                        cachedFormattedText.Trimming
                    );

                    // When the using block ends, state.Dispose() restores the original transform.
                }
                // Any changes to the transform made above are now undone.
            }

            // When state is disposed, the transform is restored.
        }
    }

    /// <summary>
    /// Paints the control. If caches are valid, it reuses the cached bitmap (graphics)
    /// to avoid expensive layout and text rendering calls.
    /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        var controlRect = new RectangleF(PointF.Empty, this.Size);

        // Clip drawing to the control's bounds.
        g.SetClip(controlRect);

        // Draw the background for the control before drawing the cached bitmap.
        using (var bgBrush = new SolidBrush(BackgroundColor))
        {
            g.FillRectangle(bgBrush, controlRect);
        }

        // Ensure region caches (layout and graphics) are valid.
        EnsureGraphicsCache(controlRect);

        Console.Error.WriteLine("Using cached graphics.");

        // Draw the cached bitmap onto the control.
        // Draw the image at (0,0) with its native size (so no scaling occurs).
        g.DrawImage(cachedBitmap, new PointF(0, 0));

        // Reset the clip.
        g.ResetClip();
    }
}
