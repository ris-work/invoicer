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
                    new Button((e, a) => MessageBox.Show("Hello")){  },

                }

            };
            KeyUp += ((e, a) => MessageBox.Show(a.Key.ToString()));

            
        }
    }
}
