using System;
using System.IO;
using Eto.Drawing;
using Eto.Forms;
using ImageMagick;

namespace MyImageProcessing
{
    /// <summary>
    /// Extension methods for MagickImage to perform common tasks.
    /// </summary>
    public static class MagickImageExtensions
    {
        /// <summary>
        /// Clones and resizes the image so that its maximum width or height does not exceed the specified maxDimension while preserving its aspect ratio.
        /// If the image is already smaller than maxDimension, it remains unchanged.
        /// </summary>
        public static MagickImage ResizeToMaxDimension(this MagickImage image, int maxDimension)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            // Clone the original image, ensuring the original remains intact.
            var clone = image.Clone();

            // The geometry string "500x500>" tells ImageMagick to shrink only if the width or height exceeds 500
            var geometry = new MagickGeometry($"{maxDimension}x{maxDimension}>");
            clone.Resize(geometry);
            return (MagickImage)clone;
        }

        /// <summary>
        /// Converts the image to a Base64 string in JPEG format, optionally constraining its file size.
        /// </summary>
        /// <param name="maxKBytes">
        /// If provided, the JPEG encoder adjusts quality automatically so that the final output is under the specified number of kilobytes.
        /// </param>
        public static string ToBase64String(
            this MagickImage image,
            int? maxKBytes = null,
            MagickFormat format = MagickFormat.Jpeg
        )
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            image.Format = format;
            if (maxKBytes.HasValue)
            {
                // "jpeg:extent" directs ImageMagick to adjust encoding to meet the file size constraint (e.g., "200kb").
                image.Settings.SetDefine(MagickFormat.Jpeg, "extent", $"{maxKBytes.Value}KB");
            }
            using (var ms = new MemoryStream())
            {
                image.Write(ms);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        /// <summary>
        /// Converts the image into an Eto.Drawing.Bitmap in JPEG format, optionally constraining its file size.
        /// </summary>
        public static Bitmap ToEtoBitmap(
            this MagickImage image,
            int? maxKBytes = null,
            MagickFormat format = MagickFormat.Jpeg
        )
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            image.Format = format;
            //var writeSettings = new MagickS(); ;
            if (maxKBytes.HasValue)
            {
                image.Settings.SetDefine(MagickFormat.Jpeg, "extent", $"{maxKBytes.Value}kb");
            }

            using (var ms = new MemoryStream())
            {
                image.Write(ms);
                ms.Position = 0;
                return new Bitmap(ms);
            }
        }
    }

    /// <summary>
    /// A factory class that provides a method to create a Panel containing the resized image and its Base64 string.
    /// </summary>
    public static class ImagePanelFactory
    {
        /// <summary>
        /// Creates a Panel that displays a resized version of the input image (with its maximum dimension limited to 500 pixels, preserving the aspect ratio)
        /// and a label showing its Base64 string representation as a constrained JPEG.
        /// The image is centered in a fixed-size (500×500) container.
        /// </summary>
        /// <param name="imagePath">The path to the input image.</param>
        /// <param name="imageKBytes">
        /// The maximum allowed file size in kilobytes for the JPEG output.
        /// </param>
        /// <returns>A Panel that can be embedded in your Eto.Forms application.</returns>
        public static Panel CreateImagePanel(string imagePath, int imageKBytes)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentException("Image path must be provided.", nameof(imagePath));

            // Load the original image completely in memory.
            using (var originalImage = new MagickImage(imagePath))
            {
                // Resize the image so that neither dimension exceeds 500 pixels.
                using (var resizedImage = originalImage.ResizeToMaxDimension(500))
                {
                    // Convert the resized image to a constrained JPEG Base64 string.
                    string base64String = resizedImage.ToBase64String(imageKBytes);

                    // Convert the resized image to an Eto.Drawing.Bitmap.
                    Bitmap etoBitmap = resizedImage.ToEtoBitmap(imageKBytes);

                    // Create an ImageView to display the image.
                    // Instead of forcing a size here, we'll center it in its container below.
                    var imageView = new ImageView { Image = etoBitmap };

                    // Wrap the ImageView in a container of fixed size (500×500) to ensure the image is centered.
                    var imageContainer = new Panel
                    {
                        Size = new Size(250, 250),
                        Content = imageView,
                    };

                    // Create a Label to display the Base64 string.
                    var base64Label = new Label
                    {
                        Text = base64String,
                        Wrap = WrapMode.Word,
                        Width = 100,
                        Height = 20,
                    };
                    var imageSizeLabel = new Label()
                    {
                        Text =
                            $"{imageView.Image.Width}x{imageView.Image.Height} {base64String.Length}",
                        Font = new Eto.Drawing.Font(Eto.Drawing.FontFamilies.Monospace, 10),
                    };

                    // Arrange the image container and the Base64 label in a vertical layout.
                    var layout = new StackLayout
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 10,
                        VerticalContentAlignment = VerticalAlignment.Stretch,
                        Items = { imageContainer, base64Label, imageSizeLabel },
                    };

                    return new Panel { Content = layout };
                }
            }
        }
    }
}
