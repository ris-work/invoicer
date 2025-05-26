using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;

namespace CommonUi
{
    public static class PanelGenerators
    {
        public static Dictionary<string, Func<string[], TextBox?, ILookupSupportedChildPanel>> Defaults()
        {
            return new Dictionary<string, Func<string[], TextBox?, ILookupSupportedChildPanel>>()
            {
                {
                    "DiscountPanel",
                    (string[] i, TextBox? T) =>{
                        Console.WriteLine($"Discount Panel: {i[0]} {i[1]}");
                        return new DiscountMarkupPanel(T, "Discount", Orientation.Vertical, Mappings: i);
                        }
                },
            };
        }
    }
}
