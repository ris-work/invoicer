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
        public static Dictionary<string, Func<string[], TextBox?, Panel>> Defaults()
        {
            return new Dictionary<string, Func<string[], TextBox?, Panel>>(){{
            "DiscountPanel", (string[] i, TextBox? T) => new DiscountMarkupPanel(T,"Discount", Orientation.Vertical)
            }
            };
        }
    }
}
