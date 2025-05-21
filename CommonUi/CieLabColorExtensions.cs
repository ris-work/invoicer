using System;
using Eto.Drawing;

namespace MyExtensions
{
    public static class ColorExtensions
    {
        /// <summary>
        /// Returns a new Color whose hue is rotated by “degrees” (using the CIELAB space)
        /// while keeping the perceptual lightness (and thus brightness and contrast)
        /// unchanged.
        /// </summary>
        /// <param name="color">The original Eto Color</param>
        /// <param name="degrees">Angle in degrees to rotate the hue. Positive values rotate clockwise.</param>
        /// <returns>A new Color with the hue rotated.</returns>
        public static Color HueRotate(this Color color, double degrees)
        {
            // Get the original sRGB components normalized to 0..1.
            // (Note that Eto’s Color gives red/green/blue components as one-byte values via Rb, Gb, Bb,
            // and the alpha as a byte via Ab.)
            double r = color.Rb / 255.0;
            double g = color.Gb / 255.0;
            double b = color.Bb / 255.0;
            int alpha = color.Ab; // alpha remains unchanged

            // Convert sRGB to linear RGB.
            r = (r <= 0.04045) ? (r / 12.92) : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = (g <= 0.04045) ? (g / 12.92) : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = (b <= 0.04045) ? (b / 12.92) : Math.Pow((b + 0.055) / 1.055, 2.4);

            // Convert linear RGB to XYZ using the D65 illuminant.
            double X = r * 0.4124 + g * 0.3576 + b * 0.1805;
            double Y = r * 0.2126 + g * 0.7152 + b * 0.0722;
            double Z = r * 0.0193 + g * 0.1192 + b * 0.9505;

            // Use normalized D65 white point values.
            double Xn = 0.95047;
            double Yn = 1.0;
            double Zn = 1.08883;

            // Convert XYZ to CIELAB.
            double fx = F_xyz(X / Xn);
            double fy = F_xyz(Y / Yn);
            double fz = F_xyz(Z / Zn);

            double L = 116 * fy - 16; // Lightness remains intact
            double a_lab = 500 * (fx - fy);
            double b_lab = 200 * (fy - fz);

            // Now compute the chroma (saturation) and current hue angle in radians.
            double chroma = Math.Sqrt(a_lab * a_lab + b_lab * b_lab);
            double hueRadians = Math.Atan2(b_lab, a_lab);

            // Add the hue offset (converted to radians).
            double newHue = hueRadians + (degrees * Math.PI / 180.0);
            double new_a = chroma * Math.Cos(newHue);
            double new_b = chroma * Math.Sin(newHue);

            // Convert the adjusted Lab (with the same L) back to XYZ.
            double fy_inv = (L + 16) / 116.0;
            double fx_inv = new_a / 500.0 + fy_inv;
            double fz_inv = fy_inv - new_b / 200.0;

            double Xr =
                (Math.Pow(fx_inv, 3) > 0.008856)
                    ? Math.Pow(fx_inv, 3)
                    : (fx_inv - 16.0 / 116.0) / 7.787;
            double Yr = (L > (903.3 * 0.008856)) ? Math.Pow(fy_inv, 3) : L / 903.3;
            double Zr =
                (Math.Pow(fz_inv, 3) > 0.008856)
                    ? Math.Pow(fz_inv, 3)
                    : (fz_inv - 16.0 / 116.0) / 7.787;

            // Scale XYZ by the white point.
            double X_final = Xr * Xn;
            double Y_final = Yr * Yn;
            double Z_final = Zr * Zn;

            // Convert XYZ back to linear RGB.
            double r_lin = X_final * 3.2406 + Y_final * -1.5372 + Z_final * -0.4986;
            double g_lin = X_final * -0.9689 + Y_final * 1.8758 + Z_final * 0.0415;
            double b_lin = X_final * 0.0557 + Y_final * -0.2040 + Z_final * 1.0570;

            // Convert linear RGB back to sRGB (apply gamma correction).
            double R =
                (r_lin <= 0.0031308) ? 12.92 * r_lin : 1.055 * Math.Pow(r_lin, 1.0 / 2.4) - 0.055;
            double G =
                (g_lin <= 0.0031308) ? 12.92 * g_lin : 1.055 * Math.Pow(g_lin, 1.0 / 2.4) - 0.055;
            double B_out =
                (b_lin <= 0.0031308) ? 12.92 * b_lin : 1.055 * Math.Pow(b_lin, 1.0 / 2.4) - 0.055;

            // Clamp each component to the [0,1] range.
            R = Clamp(R, 0, 1);
            G = Clamp(G, 0, 1);
            B_out = Clamp(B_out, 0, 1);

            // Convert to byte values.
            int redByte = (int)Math.Round(R * 255);
            int greenByte = (int)Math.Round(G * 255);
            int blueByte = (int)Math.Round(B_out * 255);

            // Create and return a new Color.
            // IMPORTANT: Color.FromArgb expects the order (red, green, blue, alpha)
            return Color.FromArgb(redByte, greenByte, blueByte, alpha);
        }

        /// <summary>
        /// Helper function for the Lab conversion.
        /// </summary>
        private static double F_xyz(double t)
        {
            return (t > 0.008856) ? Math.Pow(t, 1.0 / 3.0) : (7.787 * t + 16.0 / 116.0);
        }

        /// <summary>
        /// Clamps a double value between min and max.
        /// </summary>
        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        /// <summary>
        /// Returns a new Color whose contrast (perceptual lightness difference) is adjusted
        /// by shifting the Lab lightness (L) away from or toward a mid–lightness value (50)
        /// while keeping the hue and chroma unchanged.
        /// A positive percentage increases contrast, and a negative percentage decreases it.
        /// </summary>
        /// <param name="color">The original Eto Color</param>
        /// <param name="percent">The percentage change in contrast (e.g., +20 increases contrast, -20 decreases it).</param>
        /// <returns>A new Color with the adjusted contrast.</returns>
        public static Color AdjustContrast(this Color color, double percent)
        {
            double r = color.Rb / 255.0;
            double g = color.Gb / 255.0;
            double b = color.Bb / 255.0;
            int alpha = color.Ab;

            // Convert sRGB to linear RGB.
            r = (r <= 0.04045) ? (r / 12.92) : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = (g <= 0.04045) ? (g / 12.92) : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = (b <= 0.04045) ? (b / 12.92) : Math.Pow((b + 0.055) / 1.055, 2.4);

            // Convert linear RGB to XYZ.
            double X = r * 0.4124 + g * 0.3576 + b * 0.1805;
            double Y = r * 0.2126 + g * 0.7152 + b * 0.0722;
            double Z = r * 0.0193 + g * 0.1192 + b * 0.9505;

            // D65 white point.
            double Xn = 0.95047;
            double Yn = 1.0;
            double Zn = 1.08883;

            // Convert XYZ to CIELAB.
            double fx = F_xyz(X / Xn);
            double fy = F_xyz(Y / Yn);
            double fz = F_xyz(Z / Zn);

            double L = 116 * fy - 16;
            double a_lab = 500 * (fx - fy);
            double b_lab = 200 * (fy - fz);

            // Adjust contrast by moving L away from or toward the midpoint (50).
            // For example, a value of +20 makes L's deviation from 50 20% larger.
            double newL = 50 + (L - 50) * (1 + percent / 100.0);
            newL = Clamp(newL, 0, 100);

            // Convert the modified Lab back to XYZ.
            double fy_inv = (newL + 16) / 116.0;
            double fx_inv = a_lab / 500.0 + fy_inv;
            double fz_inv = fy_inv - b_lab / 200.0;

            double Xr =
                (Math.Pow(fx_inv, 3) > 0.008856)
                    ? Math.Pow(fx_inv, 3)
                    : (fx_inv - 16.0 / 116.0) / 7.787;
            double Yr = (newL > (903.3 * 0.008856)) ? Math.Pow(fy_inv, 3) : newL / 903.3;
            double Zr =
                (Math.Pow(fz_inv, 3) > 0.008856)
                    ? Math.Pow(fz_inv, 3)
                    : (fz_inv - 16.0 / 116.0) / 7.787;

            double X_final = Xr * Xn;
            double Y_final = Yr * Yn;
            double Z_final = Zr * Zn;

            // Convert XYZ back to linear RGB.
            double r_lin = X_final * 3.2406 + Y_final * -1.5372 + Z_final * -0.4986;
            double g_lin = X_final * -0.9689 + Y_final * 1.8758 + Z_final * 0.0415;
            double b_lin = X_final * 0.0557 + Y_final * -0.2040 + Z_final * 1.0570;

            // Convert linear RGB back to sRGB.
            double R =
                (r_lin <= 0.0031308) ? 12.92 * r_lin : 1.055 * Math.Pow(r_lin, 1.0 / 2.4) - 0.055;
            double G =
                (g_lin <= 0.0031308) ? 12.92 * g_lin : 1.055 * Math.Pow(g_lin, 1.0 / 2.4) - 0.055;
            double B_out =
                (b_lin <= 0.0031308) ? 12.92 * b_lin : 1.055 * Math.Pow(b_lin, 1.0 / 2.4) - 0.055;

            R = Clamp(R, 0, 1);
            G = Clamp(G, 0, 1);
            B_out = Clamp(B_out, 0, 1);

            int redByte = (int)Math.Round(R * 255);
            int greenByte = (int)Math.Round(G * 255);
            int blueByte = (int)Math.Round(B_out * 255);

            return Color.FromArgb(redByte, greenByte, blueByte, alpha);
        }

        /// <summary>
        /// Returns a new Color whose perceptual lightness is adjusted by the given percentage (using the CIELAB space)
        /// while preserving hue and chroma.
        /// A positive percentage makes the color lighter, while a negative percentage makes it darker.
        /// </summary>
        /// <param name="color">The original Eto Color</param>
        /// <param name="percent">The percentage change in lightness (e.g., +20 makes the color 20% lighter, -20 makes it 20% darker).</param>
        /// <returns>A new Color with the adjusted lightness.</returns>
        public static Color AdjustLightness(this Color color, double percent)
        {
            // Get the original sRGB components normalized to 0..1.
            double r = color.Rb / 255.0;
            double g = color.Gb / 255.0;
            double b = color.Bb / 255.0;
            int alpha = color.Ab; // preserve alpha

            // Convert sRGB to linear RGB.
            r = (r <= 0.04045) ? (r / 12.92) : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = (g <= 0.04045) ? (g / 12.92) : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = (b <= 0.04045) ? (b / 12.92) : Math.Pow((b + 0.055) / 1.055, 2.4);

            // Convert linear RGB to XYZ.
            double X = r * 0.4124 + g * 0.3576 + b * 0.1805;
            double Y = r * 0.2126 + g * 0.7152 + b * 0.0722;
            double Z = r * 0.0193 + g * 0.1192 + b * 0.9505;

            // Use normalized D65 white point values.
            double Xn = 0.95047;
            double Yn = 1.0;
            double Zn = 1.08883;

            // Convert XYZ to CIELAB.
            double fx = F_xyz(X / Xn);
            double fy = F_xyz(Y / Yn);
            double fz = F_xyz(Z / Zn);

            double L = 116 * fy - 16; // original lightness (0 to 100)
            double a_lab = 500 * (fx - fy);
            double b_lab = 200 * (fy - fz);

            // Adjust lightness by scaling L. For example, a 20% increase multiplies L by 1.2.
            double newL = L * (1 + percent / 100.0);
            newL = Clamp(newL, 0, 100); // ensure lightness stays within [0, 100]

            // Convert the adjusted Lab (with the new L, same a_lab and b_lab) back to XYZ.
            double fy_inv = (newL + 16) / 116.0;
            double fx_inv = a_lab / 500.0 + fy_inv;
            double fz_inv = fy_inv - b_lab / 200.0;

            double Xr =
                (Math.Pow(fx_inv, 3) > 0.008856)
                    ? Math.Pow(fx_inv, 3)
                    : (fx_inv - 16.0 / 116.0) / 7.787;
            double Yr = (newL > (903.3 * 0.008856)) ? Math.Pow(fy_inv, 3) : newL / 903.3;
            double Zr =
                (Math.Pow(fz_inv, 3) > 0.008856)
                    ? Math.Pow(fz_inv, 3)
                    : (fz_inv - 16.0 / 116.0) / 7.787;

            // Scale XYZ by the white point.
            double X_final = Xr * Xn;
            double Y_final = Yr * Yn;
            double Z_final = Zr * Zn;

            // Convert XYZ back to linear RGB.
            double r_lin = X_final * 3.2406 + Y_final * -1.5372 + Z_final * -0.4986;
            double g_lin = X_final * -0.9689 + Y_final * 1.8758 + Z_final * 0.0415;
            double b_lin = X_final * 0.0557 + Y_final * -0.2040 + Z_final * 1.0570;

            // Convert linear RGB back to sRGB (applying gamma correction).
            double R =
                (r_lin <= 0.0031308) ? 12.92 * r_lin : 1.055 * Math.Pow(r_lin, 1.0 / 2.4) - 0.055;
            double G =
                (g_lin <= 0.0031308) ? 12.92 * g_lin : 1.055 * Math.Pow(g_lin, 1.0 / 2.4) - 0.055;
            double B_out =
                (b_lin <= 0.0031308) ? 12.92 * b_lin : 1.055 * Math.Pow(b_lin, 1.0 / 2.4) - 0.055;

            // Clamp each component to the [0,1] range.
            R = Clamp(R, 0, 1);
            G = Clamp(G, 0, 1);
            B_out = Clamp(B_out, 0, 1);

            // Convert to byte values.
            int redByte = (int)Math.Round(R * 255);
            int greenByte = (int)Math.Round(G * 255);
            int blueByte = (int)Math.Round(B_out * 255);

            // Return the newly created Color.
            return Color.FromArgb(redByte, greenByte, blueByte, alpha);
        }
    }
}
