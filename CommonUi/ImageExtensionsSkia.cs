using System;
using System.Drawing;
using System.IO;
using Eto.Drawing;
using Eto.Forms;
using SkiaSharp;

namespace MySkiaApp
{
    // Extension methods to process SKBitmap images.
    public static class SkiaSharpImageExtensions
    {
        /// <summary>
        /// Resizes the bitmap so that its largest dimension does not exceed maxDimension,
        /// preserving the aspect ratio. If the image is already small enough, a clone is returned.
        /// </summary>
        public static SKBitmap ResizeToMaxDimension(this SKBitmap bitmap, int maxDimension)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            int originalWidth = bitmap.Width;
            int originalHeight = bitmap.Height;

            // Return a copy if resizing is not needed.
            if (originalWidth <= maxDimension && originalHeight <= maxDimension)
                return bitmap;

            // Calculate scaling factor to reduce the larger dimension to maxDimension.
            float scale = (float)maxDimension / Math.Max(originalWidth, originalHeight);
            int newWidth = (int)(originalWidth * scale);
            int newHeight = (int)(originalHeight * scale);

            SKBitmap resized = new SKBitmap(newWidth, newHeight);
            using (var canvas = new SKCanvas(resized))
            {
                canvas.Clear(SKColors.Transparent);
                var destRect = new SKRect(0, 0, newWidth, newHeight);
                canvas.DrawBitmap(bitmap, destRect);
            }
            return resized;
        }

        /// <summary>
        /// Encodes the bitmap as a JPEG byte array, iteratively reducing quality until the
        /// output size is less than or equal to maxKBytes kilobytes.
        /// </summary>
        public static byte[] ToJpegBytesWithLimit(this SKBitmap bitmap, int maxKBytes)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            int maxByteSize = maxKBytes * 1024;
            int quality = 100;
            SKData encodedData = null;

            using (var image = SKImage.FromBitmap(bitmap))
            {
                // Reduce quality iteratively until we meet the size limit or quality gets very low.
                do
                {
                    encodedData?.Dispose();
                    encodedData = image.Encode(SKEncodedImageFormat.Jpeg, quality);
                    if (encodedData == null)
                        throw new Exception("Failed to encode image.");

                    if (encodedData.Size <= maxByteSize || quality <= 10)
                        break;

                    quality -= 5;
                } while (true);
            }

            byte[] result = encodedData.ToArray();
            encodedData.Dispose();
            return result;
        }

        /// <summary>
        /// Converts the bitmap to a Base64 encoded string after JPEG encoding with the file size limit.
        /// </summary>
        public static string ToJpegBase64String(this SKBitmap bitmap, int maxKBytes)
        {
            byte[] jpegBytes = bitmap.ToJpegBytesWithLimit(maxKBytes);
            return Convert.ToBase64String(jpegBytes);
        }

        /// <summary>
        /// Converts the bitmap into an Eto.Drawing.Bitmap by encoding it to JPEG with a size limit.
        /// </summary>
        public static Eto.Drawing.Bitmap ToEtoBitmap(this SKBitmap bitmap, int maxKBytes)
        {
            byte[] jpegBytes = bitmap.ToJpegBytesWithLimit(maxKBytes);
            using (var ms = new MemoryStream(jpegBytes))
            {
                return new Eto.Drawing.Bitmap(ms);
            }
        }
    }

    // Factory class that creates a panel displaying the processed image and its Base64 string.
    public static class ImagePanelFactorySkia
    {
        /// <summary>
        /// Loads an image from the given path, resizes it so that its maximum dimension is 500 pixels,
        /// encodes it as a JPEG (with a file size limit in kilobytes), and returns an Eto panel containing
        /// the image (centered in a 500×500 container) and its Base64 string.
        /// </summary>
        public static Panel CreateImagePanel(string imagePath, int imageKBytes)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentException("Image path must be provided.", nameof(imagePath));

            // Load the image into an SKBitmap.
            SKBitmap original = SKBitmap.Decode(imagePath);
            if (original == null)
                throw new Exception("Failed to load image from path: " + imagePath);

            // Resize while preserving aspect ratio.
            SKBitmap resized = original.ResizeToMaxDimension(500);
            original.Dispose(); // Dispose original if no longer needed.

            // Generate the JPEG Base64 string with the size constraint.
            string base64String = resized.ToJpegBase64String(imageKBytes);
            // Convert to Eto.Drawing.Bitmap for display.
            Eto.Drawing.Bitmap etoBitmap = resized.ToEtoBitmap(imageKBytes);

            // Create an ImageView for displaying the image.
            var imageView = new ImageView { Image = etoBitmap };

            // Wrap the image in a fixed 500×500 panel to center it.
            var imageContainer = new Panel
            {
                Size = new Eto.Drawing.Size(250, 250),
                Content = imageView,
            };

            // Create a label to show the Base64 string.
            var base64Label = new Label
            {
                Text = base64String,
                Wrap = WrapMode.Word,
                Width = 100,
                Height = 20,
            };
            var imageSizeLabel = new Label()
            {
                Text = $"{imageView.Image.Width}x{imageView.Image.Height} {base64String.Length}",
                Font = new Eto.Drawing.Font(Eto.Drawing.FontFamilies.Monospace, 10),
            };

            // Arrange the image and label vertically.
            var layout = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 10,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Items = { imageContainer, base64Label, imageSizeLabel },
            };

            // Dispose the resized bitmap since we've generated the outputs.
            resized.Dispose();

            return new Panel { Content = layout };
        }
    }

    // Main form that integrates the image panel into the application's UI.
    public class MainForm : Form
    {
        public MainForm()
        {
            Title = "SkiaSharp Logo Test";
            ClientSize = new Eto.Drawing.Size(800, 600);

            // Create the image panel using "logo.png" with a maximum size of 200 KB.
            Panel imagePanel = ImagePanelFactorySkia.CreateImagePanel("logo.png", 200);

            Content = new StackLayout
            {
                Padding = 10,
                Spacing = 10,
                Items =
                {
                    new Label { Text = "Logo Image (SkiaSharp) with Base64 String:" },
                    imagePanel,
                },
            };
        }
    }

    // Program entry point.
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            var app = new Application();
            app.Run(new MainForm());
        }
    }
}
