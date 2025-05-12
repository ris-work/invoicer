#if WINDOWS
using System;
using System.Windows.Markup;
using System.Windows.Media;
using Eto.Drawing;

namespace CommonUi
{
    /// <summary>
    /// A markup extension that converts an Eto.Drawing.Color to a SolidColorBrush.
    /// </summary>
    public class EtoBrushExtension : MarkupExtension
    {
        public EtoBrushExtension() { }

        // Allow using this extension with a constructor parameter.
        public EtoBrushExtension(Eto.Drawing.Color color)
        {
            Color = color;
        }

        /// <summary>
        /// The color (typically provided via x:Static) used to create the brush.
        /// </summary>
        public Eto.Drawing.Color Color { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // Use the extension method on Eto.Drawing.Color (defined in CommonUi) to convert.
            return new SolidColorBrush(Color.ToMediaColor());
        }
    }
}
#endif
