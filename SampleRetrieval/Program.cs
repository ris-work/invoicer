using Eto.Forms;
using RV.LabelRetriever;

Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

new Application(Eto.Platforms.Wpf).Run(new MainForm());

public class MainForm : Form
{
    public MainForm()
    {
        var Btn = new Button() { Text = "See the Message Box" };
        Btn.Click += (_, _) =>
        {
            MessageBox.Show(
                null,
                $"{LabelRetriever.LookupTamil(1)}, {LabelRetriever.LookupSinhala(1)}",
                "Looked up label 1",
                MessageBoxType.Information
            );
            MessageBox.Show(
                null,
                $"{LabelRetriever.LookupTamil("003")}, {LabelRetriever.LookupSinhala("0003")}",
                "Looked up label 3",
                MessageBoxType.Information
            );
            MessageBox.Show(
                null,
                $"{LabelRetriever.LookupTamil("1111003")}, {LabelRetriever.LookupSinhala("11110003")}",
                "Looked up label 3",
                MessageBoxType.Information
            );
        };
        Content = Btn;
        Width = 200;
        Height = 200;
        WindowStyle = WindowStyle.Utility;
    }
}

partial class Program { }
