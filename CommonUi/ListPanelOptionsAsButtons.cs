using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;

namespace CommonUi
{
    public class ListPanelOptionsAsButtons : Form
    {
        public ListPanelOptionsAsButtons((string, object)[] A)
        {
            Dictionary<string, object> Panels = new Dictionary<string, object>();
            foreach (var panel in A)
            {
                Panels.Add(panel.Item1, panel.Item2);
            }
            IReadOnlyDictionary<string, object> ROD = Panels;
            List<Button> ButtonsList = new List<Button>();
            foreach (var panel in A)
            {
                Button B = new Button()
                {
                    Text = panel.Item1,
                    BackgroundColor = ColorSettings.BackgroundColor,
                    TextColor = Eto.Drawing.Colors.DarkGoldenrod,
                };
                B.ConfigureForPlatform();
                B.MinimumSize = new Eto.Drawing.Size(30, 35);

                B.Click += (_, _) =>
                {
                    (
                        new SinglePanelForm(
                            (
                                B.Text,
                                (
                                    (ILoadOncePanel<object>)
                                        ROD.GetValueOrDefault<string, object?>(
                                            (string)(B.Text),
                                            null
                                        )
                                )
                            )
                        )
                    ).Show();
                };
                ButtonsList.Add(B);
            }
            StackLayout ButtonsListLayout = new StackLayout(
                ButtonsList.Select(a => new StackLayoutItem(a)).ToArray()
            )
            {
                Spacing = 10,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
            Title = "RV Accessibility Services ♿👓";
            Content = ButtonsListLayout;
            BackgroundColor = ColorSettings.BackgroundColor;
            Padding = 10;
            Resizable = false;
            BackgroundColor = Eto.Drawing.Colors.DarkSlateGray;
            //(Control)((ILoadOncePanel<object>)ROD.GetValueOrDefault<string, object?>((string)((string)ClickedLabel.Text), null)).GetInnerAsObject();
        }
    }
}
