using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;

namespace CommonUi
{
    class SinglePanelForm : Form
    {
        public SinglePanelForm((string, object) TupleIn)
        {
            Title = TupleIn.Item1;
            Content = (Control)(((ILoadOncePanel<object>)TupleIn.Item2).GetInnerAsObject());
            BackgroundColor = Eto.Drawing.Colors.Black;
            Padding = new Eto.Drawing.Padding(20);
        }
    }
}
