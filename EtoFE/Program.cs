// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using EtoFE;
using Microsoft.EntityFrameworkCore.Scaffolding;
using RV.InvNew.Common;
using Tomlyn;
using Tomlyn.Model;

public static class Mock
{
    public static List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SearchCatalogue;
    public static List<(string, TextAlignment, bool)> HeaderEntries;
}

public static class LoginTokens
{
    public static LoginToken token;
    public static LoginToken ElevatedLoginToken;
    public static string LoginTokenForBearerAuth()
    {
        return JsonSerializer.Serialize(token);
    }
}

public class Program
{
    public static HttpClient client; // = new HttpClient();
    public static Tomlyn.Model.TomlTable Config;
    public static IReadOnlyDictionary<string, object?> ConfigDict;

    [STAThread]
    public static void Main()
    {
        var CH = new HttpClientHandler();
        CH.AutomaticDecompression = DecompressionMethods.All;
        client = new HttpClient(CH);
        var ConfigFile = System.IO.File.ReadAllText("config.toml");
        Config = Toml.ToModel(ConfigFile);
        ConfigDict = Config.ToDictionary();
        Console.WriteLine("Hello, World!");
        client.BaseAddress = new Uri((string)Config["BaseAddress"]);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")
        );
        new Application(Eto.Platforms.Wpf).Run(new MyForm());
        Terminal.Gui.Application.Init();
        Terminal.Gui.Application.Run(
            new CommonUi.SearchDialogTUI(Mock.SearchCatalogue, Mock.HeaderEntries)
        );
    }
}

public class MyForm : Form
{
    public bool Login(String Username, String Password, String Terminal)
    {
        LoginToken logint = null;
        LoginCredentials l = new(Username, Password, Terminal, null);
        var response = Program.client.PostAsJsonAsync("/Login", l);
        response.Wait();
        var result = response.Result;
        result.EnsureSuccessStatusCode();
        var logint_w = result.Content.ReadAsAsync<LoginToken>();
        logint_w.Wait();
        logint = logint_w.Result;
        MessageBox.Show($"{logint.TokenID}, {logint.Token}, {logint.Error}", MessageBoxType.Information);
        if (logint.Error != "")
        {
            return false;
        }
        else
        {
            LoginTokens.token = logint;
            return true;
        }
    }

    public string TryEcho(string Message)
    {
        AuthenticatedRequest<string> request = new AuthenticatedRequest<string>(
            $"Hello {Message}",
            LoginTokens.token
        );
        var response = Program.client.PostAsJsonAsync("/AuthenticatedEcho", request);
        //MessageBox.Show(JsonSerializer.Serialize(request));
        response.Wait();
        var result = response.Result;

        result.EnsureSuccessStatusCode();
        var t_res = result.Content.ReadAsStringAsync();
        t_res.Wait();
        string echoed = t_res.Result;
        return echoed;
    }

    public MyForm()
    {
        Eto.Style.Add<Label>(
            "mono",
            label =>
            {
                label.Font = new Font(new Eto.Drawing.FontFamily("Monospace"), 15);
            }
        );
        Eto.Style.Add<TextBox>(
            "mono",
            box =>
            {
                box.Font = new Font(new Eto.Drawing.FontFamily("Monospace"), 15);
            }
        );
        Eto.Style.Add<PasswordBox>(
            "mono",
            box =>
            {
                box.Font = new Font(new Eto.Drawing.FontFamily("Monospace"), 15);
            }
        );
        Eto.Style.Add<Button>(
            "large",
            label =>
            {
                label.Font = new Font(new Eto.Drawing.FontFamily("Sans-serif"), 15);
            }
        );
        Title = "My Cross-Platform App";
        ClientSize = new Size(800, 600);
        var layout = new TableLayout();
        TextBox UsernameBox,
            TerminalBox;
        PasswordBox PasswordBox;

        var ModelDict = Program.ConfigDict;
        string LogoPath = (string)ModelDict.GetValueOrDefault("LogoPath", "logo.png");
        string TermLogoPath = (string)
            ModelDict.GetValueOrDefault("TermLogo", "posprogram_export.png");

        Eto.Forms.ImageView Logo = new ImageView();
        Eto.Forms.ImageView TermLogo = new ImageView();
        if (System.IO.File.Exists(LogoPath))
        {
            Uri LogoUri = new Uri(new Uri(Config.GetCWD()), LogoPath);
            System.Console.WriteLine(LogoUri.AbsoluteUri);
            Logo.Image = new Eto.Drawing.Bitmap(LogoUri.AbsoluteUri);
        }

        if (System.IO.File.Exists(TermLogoPath))
        {
            Uri TermLogoUri = new Uri(new Uri(Config.GetCWD()), TermLogoPath);
            System.Console.WriteLine(TermLogoUri.AbsoluteUri);
            TermLogo.Image = new Eto.Drawing.Bitmap(TermLogoUri.AbsoluteUri);
        }

        layout.Rows.Add(null);
        layout.Spacing = new Size(5, 5);
        layout.Padding = new Padding(10, 10, 10, 10);
        layout.Rows.Add(new TableRow(null, new TableCell(Logo), TermLogo, null));
        layout.Rows.Add(
            new TableRow(
                null,
                new Label() { Text = "Username : ", Style = "mono" },
                UsernameBox = new TextBox() { PlaceholderText = "Username", Style = "mono" },
                null
            )
        );
        layout.Rows.Add(
            new TableRow(
                null,
                new Label() { Text = "Password : ", Style = "mono" },
                PasswordBox = new PasswordBox() { Style = "mono" },
                null
            )
        );
        layout.Rows.Add(
            new TableRow(
                null,
                new Label() { Text = "Terminal : ", Style = "mono" },
                TerminalBox = new TextBox()
                {
                    PlaceholderText = "1",
                    Enabled = false,
                    Text = "1",
                    Style = "mono",
                    TextAlignment = TextAlignment.Right,
                },
                null
            )
        );
        layout.Rows.Add(
            new TableRow(
                null,
                new Button(
                    (sender, e) =>
                    {
                        if (Login(UsernameBox.Text, PasswordBox.Text, TerminalBox.Text) != true)
                        {
                            MessageBox.Show("Cannot login: Network Error or Wrong Creds");
                        }
                        else
                        {
                            MessageBox.Show(TryEcho(UsernameBox.Text), MessageBoxType.Information);
                            (new PosTerminal()).Show();
                        }
                        ;
                    }
                )
                {
                    Text = "Login",
                    Size = new Size(200, 50),
                    Style = "large",
                },
                new TableRow(
                    new Button(
                        (sender, e) =>
                        {
                            Application.Instance.Quit();
                        }
                    )
                    {
                        Text = "Exit",
                        Style = "large",
                        Size = new Size(300, 50),
                    }
                ),
                null
            )
        );
        layout.Rows.Add(null);
        Content = layout;
    }
}
