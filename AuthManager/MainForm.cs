using Eto.Drawing;
using Eto.Forms;
using System;
using Tomlyn;

namespace AuthManager
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            Title = "My Eto Form";
            MinimumSize = new Size(200, 200);

            Content = new StackLayout
            {
                Padding = 10,
                Items =
                {
                    "Hello World!",
					// add more controls here
				}
            };

            
        }
    }
}
