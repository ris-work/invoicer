// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CommonUi;
using Eto.Drawing;
using Eto.Forms;
using EtoFE;
using Microsoft.EntityFrameworkCore.Scaffolding;
using RV.InvNew.Common;
using Tomlyn;
using Tomlyn.Model;
//using SharpDX.Direct2D1.Effects;
#if WINDOWS
//using Eto.WinUI;
#endif

public static class Mock
{
    public static List<(string[], Eto.Drawing.Color?, Eto.Drawing.Color?)> SearchCatalogue;
    public static List<(string, TextAlignment, bool)> HeaderEntries;
}

public static class LoginTokens
{
    public static LoginToken token;
    public static LoginToken ElevatedLoginToken;
    public static string Username = "";

    public static string LoginTokenForBearerAuth()
    {
        return JsonSerializer.Serialize(token);
    }
}

public class Program
{
    public static FontFamily UIFont = null; //= FontFamilies.Serif; //Don't do this before platform init!
    public static FontFamily UIFontMono = null;
    public static bool IsWpf = false;
    public static HttpClient client; // = new HttpClient();
    public static Tomlyn.Model.TomlTable Config;
    public static IReadOnlyDictionary<string, object?> ConfigDict;
    public static string lang = "en";
    public static bool UseCustomFonts = false;
    public static int? PanelWidth = null;
    public static int? InnerPanelButtonWidth = null;
    public static int? InnerPanelButtonHeight = null;
    public static int? InnerPanelButtonContainerHeight = null;
    public static int? InnerPanelButtonContainerWidth = null;
    public static int? InnerLabelHeight = null;
    public static int? InnerLabelWidth = null;

    [STAThread]
    public static void Main()
    {
        // Enable Unix support for System.Drawing.Common programmatically.
        // This must be called before any code that uses System.Drawing.
        AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);

        Console.WriteLine("Unix support for System.Drawing.Common is now enabled!");

        // Your application code goes here.
        // For example, you could perform image processing tasks here.
        ResourceExtractor.MainAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        ResourceExtractor.EnsureAllResources();
        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        string[] resourceNames = currentAssembly.GetManifestResourceNames();
        Console.WriteLine("Embedded resources found:");
        // Loop through all embedded resources

        foreach (string resourceName in resourceNames)
        {
            // Strip the assembly prefix from resource name if present.
            string prefix = currentAssembly.GetName().Name + ".";
            string fileName = resourceName.StartsWith(prefix)
                ? resourceName.Substring(prefix.Length)
                : resourceName;

            // Only extract if file doesn't exist in the current working directory.
            if (File.Exists(fileName))
            {
                Console.WriteLine($"Skipping {fileName}; already exists.");
                continue;
            }

            using (Stream stream = currentAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Console.WriteLine($"ERROR: Could not load {resourceName}.");
                    continue;
                }

                using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fs);
                }
            }
            Console.WriteLine($"Extracted {fileName}.");
        }
        var CH = new HttpClientHandler();
        CH.AutomaticDecompression = DecompressionMethods.All;
        client = new HttpClient(CH);
        var ConfigFile = System.IO.File.ReadAllText("config.toml");
        Config = Toml.ToModel(ConfigFile);
        ConfigDict = Config.ToDictionary();
        double HueRotationDegrees = (double)
            ConfigDict.GetValueOrDefault("HueRotationDegrees", 0.0d);
        UseCustomFonts = (bool)ConfigDict.GetValueOrDefault("UseCustomFonts", false);
        PanelWidth = (int?)(long?)ConfigDict.GetValueOrDefault("InnerPanelWidth", null);
        InnerPanelButtonWidth = (int?)
            (long?)ConfigDict.GetValueOrDefault("InnerPanelButtonWidth", null);
        InnerPanelButtonHeight = (int?)
            (long?)ConfigDict.GetValueOrDefault("InnerPanelButtonHeight", null);
        InnerPanelButtonContainerWidth = (int?)
            (long?)ConfigDict.GetValueOrDefault("InnerPanelButtonContainerWidth", null);
        InnerPanelButtonContainerHeight = (int?)
            (long?)ConfigDict.GetValueOrDefault("InnerPanelButtonContainerHeight", null);
        InnerLabelWidth = (int?)(long?)ConfigDict.GetValueOrDefault("InnerLabelWidth", null);
        InnerLabelHeight = (int?)(long?)ConfigDict.GetValueOrDefault("InnerLabelHeight", null);
        Console.WriteLine("Hello, World!");
        CommonUi.ColorSettings.Initialize(ConfigDict);
        CommonUi.ColorSettings.InnerLabelWidth = InnerLabelWidth;
        CommonUi.ColorSettings.InnerLabelHeight = InnerLabelHeight;
        ColorSettings.RotateAll(HueRotationDegrees);
        ColorSettings.Dump();
        ResourceExtractor.EnsureTranslationsFile("translations.toml");

        client.BaseAddress = new Uri((string)Config["BaseAddress"]);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")
        );
        // For debugging: List all manifest resources:
#if WINDOWS
        ResourceDebugger.ListManifestResourceNames();
        ResourceDebugger.ListGResources();
#endif
        //string CurrentUI = Eto.Platforms.WinForms; //Eto.Platforms.Wpf;
        string CurrentUI = Eto.Platforms.Wpf;
        Console.WriteLine(Eto.Platforms.WinForms);
        Console.WriteLine(Eto.Platforms.Wpf);
        TranslationHelper.LoadTranslations("translations.toml");
        TranslationHelper.LoadTranslations("terminal.toml");
        Program.lang = (string)ConfigDict.GetValueOrDefault("Language", "en");
        TranslationHelper.Lang = Program.lang;
        string CurrentUIConfigured = (string)
            ConfigDict.GetValueOrDefault("EtoBackend", Eto.Platforms.Wpf);
        ColorSettings.Dump();
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            CurrentUI = Eto.Platforms.Gtk;
        if (CurrentUIConfigured.ToLowerInvariant() == ("winforms"))
            CurrentUI = Eto.Platforms.WinForms;
        if (CurrentUIConfigured.ToLowerInvariant() == ("gtk"))
            CurrentUI = Eto.Platforms.Gtk;
        if (CurrentUIConfigured.ToLowerInvariant() == ("direct2d"))
            CurrentUI = Eto.Platforms.Direct2D;
        if (CurrentUIConfigured.ToLowerInvariant() == ("winui"))
            CurrentUI = Eto.Platforms.Wpf;

        bool EnableTUI = (bool)ConfigDict.GetValueOrDefault("EnableTUI", false);

        (new Application(CurrentUI)).Run(new MyForm());

        if (EnableTUI)
        {
            Terminal.Gui.Application.Init();
            Terminal.Gui.Application.Run(
                new CommonUi.SearchDialogTUI(Mock.SearchCatalogue, Mock.HeaderEntries)
            );
        }
    }
}

public class MyForm : Form
{
    public bool Login(String Username, String Password, String Terminal)
    {
        //CommonUi.DisableDefaults.ApplyGlobalScrollBarThumbStyle();
        //CommonUi.DisableDefaults.ApplyGlobalScrollBarPageButtonStyle();
        LoginToken logint = null;
        LoginCredentials l = new(Username, Password, Terminal, null);
        try
        {
            var response = Program.client.PostAsJsonAsync("/Login", l);
            response.Wait();
            var result = response.Result;
            result.EnsureSuccessStatusCode();
            var logint_w = result.Content.ReadAsAsync<LoginToken>();
            logint_w.Wait();
            logint = logint_w.Result;
            MessageBox.Show(
                $"{logint.TokenID}, {logint.Token}, {logint.Error}",
                MessageBoxType.Information
            );
            if (logint.Error != "")
            {
                return false;
            }
            else
            {
                LoginTokens.token = logint;
                LoginTokens.Username = Username;
                return true;
            }
        }
        catch (Exception E)
        {
            MessageBox.Show(
                $"Endpoint: {Program.client.BaseAddress}\r\nCheck PORT (backend instance) and ADDRESS reachability\r\n{E.Message}\r\n{E.StackTrace}",
                "An error has occured, Login failed or likely backend failed",
                MessageBoxButtons.OK,
                MessageBoxType.Error
            );
            return false;
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
        if (Program.UseCustomFonts)
        {
            try
            {
                Program.UIFontMono = FontFamily.FromFiles("Gourier.ttf");
                Program.UIFontMono = FontFamily.FromFiles("FluentEmoji.ttf");
            }
            catch (Exception E)
            {
                Console.WriteLine($"{E.ToString()}, {E.StackTrace}");
                Program.UIFont = FontFamilies.Serif;
            }
            try
            {
                Program.UIFont = FontFamily.FromFiles("NotoSans-Regular.ttf");
            }
            catch (Exception E)
            {
                Console.WriteLine($"{E.ToString()}, {E.StackTrace}");
                Program.UIFontMono = FontFamilies.Monospace;
            }
            Console.WriteLine("Font initialization is complete!");
        }
        else
        {
            Console.WriteLine("Custom fonts are disabled");
            Program.UIFont = FontFamilies.Sans;

            Program.UIFontMono = FontFamilies.Monospace;
        }

        ColorSettings.UIFont = Program.UIFont;
        ColorSettings.UIFontMono = Eto.Drawing.FontFamilies.Monospace;
        Console.WriteLine($"Fonts: {ColorSettings.UIFont.Name}, {ColorSettings.UIFontMono.Name}!");

        var platform = Eto.Forms.Application.Instance.Platform;
        System.Console.WriteLine($"Platform: {platform}");
        if (
            platform != null
            && platform.ToString().Equals("Eto.Wpf.Platform", StringComparison.OrdinalIgnoreCase)
        )
        {
            Program.IsWpf = true;
            // Execute WPF-specific logic here.
        }
#if WINDOWS
        try
        {
            if (Program.IsWpf)
            {
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(
                    new System.Windows.ResourceDictionary
                    {
                        Source = new Uri(
                            "pack://application:,,,/EtoFE;component/theming/WpfPlus/WpfPlus/DarkTheme.xaml",
                            UriKind.Absolute
                        ),
                    }
                );
                //System.Windows.Application.Current.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/DynamicAero2;component/Brushes/Dark.xaml", UriKind.RelativeOrAbsolute) });
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(
                    new System.Windows.ResourceDictionary
                    {
                        Source = new Uri(
                            "pack://application:,,,/EtoFE;component/theming/WpfPlus/WpfPlus/Colors/DarkColors.xaml",
                            UriKind.Absolute
                        ),
                    }
                );
            }
            else
            {
                Console.WriteLine("Not WPF");
            }
        }
        catch (Exception E)
        {
            System.Console.WriteLine($"{E.ToString()}, {E.StackTrace}");
        }
#endif
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
        BackgroundColor = ColorSettings.BackgroundColor;

        var ModelDict = Program.ConfigDict;
        string LogoPath = (string)ModelDict.GetValueOrDefault("LogoPath", "logo.png");
        string TermLogoPath = (string)
            ModelDict.GetValueOrDefault("TermLogo", "posprogram_export.png");
        static Uri GetCwdX() =>
            new Uri(
                "file:///" + System.IO.Directory.GetCurrentDirectory().Replace("\\", "/") + "/"
            );

        Eto.Forms.ImageView Logo = new ImageView();
        Eto.Forms.ImageView TermLogo = new ImageView();
        try
        {
            if (System.IO.File.Exists(LogoPath))
            {
                Uri LogoUri = new Uri(GetCwdX(), LogoPath);
                System.Console.WriteLine(LogoUri.LocalPath);
                Logo.Image = new Eto.Drawing.Bitmap(LogoUri.LocalPath);
            }
        }
        catch (Exception E)
        {
            Console.WriteLine($"{E.ToString()}, {E.StackTrace}");
        }

        try
        {
            if (System.IO.File.Exists(TermLogoPath))
            {
                Uri LogoUri = new Uri(new Uri(Config.GetCWD()), LogoPath);
                Uri TermLogoUri = new Uri(GetCwdX(), TermLogoPath);
                System.Console.WriteLine(TermLogoUri.LocalPath);
                //TermLogo.Image = new Eto.Drawing.Bitmap(TermLogoUri.LocalPath);
                TermLogo.Image = new Eto.Drawing.Bitmap(TermLogoUri.LocalPath);
            }
        }
        catch (Exception E)
        {
            Console.WriteLine($"{E.ToString()},{E.StackTrace}");
        }

        layout.Rows.Add(null);
        layout.Spacing = new Size(5, 5);
        layout.Padding = new Padding(10, 10, 10, 10);
        layout.Rows.Add(new TableRow(null, new TableCell(Logo), TermLogo, null));
        layout.Rows.Add(
            new TableRow(
                null,
                new Label()
                {
                    Text = TranslationHelper.Translate(
                        "Username",
                        "Username",
                        TranslationHelper.Lang
                    ),
                    //Style = "mono",
                    Width = 20,
                    TextColor = ColorSettings.ForegroundColor,
                    BackgroundColor = ColorSettings.BackgroundColor,
                    Font = new Font(Program.UIFont, 14),
                },
                UsernameBox = new TextBox()
                {
                    PlaceholderText = "Username",
                    //Style = "mono",
                    Width = 20,
                    TextColor = ColorSettings.ForegroundColor,
                    BackgroundColor = ColorSettings.LesserBackgroundColor,
                    ShowBorder = false,
                    Font = new Font(Program.UIFont, 14),
                },
                null
            )
        );
        layout.Rows.Add(
            new TableRow(
                null,
                new Label()
                {
                    Text = TranslationHelper.Translate(
                        "Password",
                        "Password",
                        TranslationHelper.Lang
                    ),
                    //Style = "mono",
                    Width = 20,
                    TextColor = ColorSettings.ForegroundColor,
                    BackgroundColor = ColorSettings.BackgroundColor,
                    Font = new Font(Program.UIFont, 14),
                },
                PasswordBox = new PasswordBox()
                {
                    //Style = "mono",
                    Width = 20,
                    TextColor = ColorSettings.ForegroundColor,
                    BackgroundColor = ColorSettings.LesserBackgroundColor,
                    Font = new Font(Program.UIFont, 14),
                },
                null
            )
        );
        layout.Rows.Add(
            new TableRow(
                null,
                new Label()
                {
                    Text = TranslationHelper.Translate(
                        "Terminal",
                        "Terminal",
                        TranslationHelper.Lang
                    ),
                    //Style = "mono",
                    Width = 20,
                    BackgroundColor = ColorSettings.BackgroundColor,
                    TextColor = ColorSettings.ForegroundColor,
                    Font = new Font(Program.UIFont, 14),
                },
                TerminalBox = new TextBox()
                {
                    PlaceholderText = "1",
                    Enabled = false,
                    Text = "1",
                    //Style = "mono",
                    TextAlignment = TextAlignment.Right,
                    Width = 20,
                    BackgroundColor = ColorSettings.LesserBackgroundColor,
                    TextColor = ColorSettings.ForegroundColor,
                    Font = new Font(Program.UIFont, 14),
                },
                null
            )
        );
        var LoginButton = new Button(
            (sender, e) =>
            {
                if (Login(UsernameBox.Text, PasswordBox.Text, TerminalBox.Text) != true)
                {
                    MessageBox.Show("Cannot login: Network Error or Wrong Creds");
                }
                else
                {
                    MessageBox.Show(TryEcho(UsernameBox.Text), MessageBoxType.Information);
                    bool FullMode = (bool)
                        Program.ConfigDict.GetValueOrDefault("EnableFullMode", false);

                    if (!FullMode)
                    {
                        (new NavigableListForm()).Show();
                    }
                    else
                    {
                        (new PosTerminal()).Show();
                    }
                    //(new PosTerminal()).Show();
                }
                ;
            }
        )
        {
            Text = "Login",
            Size = new Size(200, 50),
            Style = "large",
            BackgroundColor = ColorSettings.BackgroundColor,
            TextColor = ColorSettings.LesserForegroundColor,
            Font = new Font(Program.UIFont, 14),
        };
        layout.Rows.Add(
            new TableRow(
                null,
                LoginButton,
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
                        BackgroundColor = ColorSettings.BackgroundColor,
                        TextColor = ColorSettings.LesserForegroundColor,
                        Font = new Font(Program.UIFont, 14),
                    }
                ),
                null
            )
        );
        UsernameBox.KeyDown += (e, a) =>
        {
            if (a.Key == Keys.Enter)
                PasswordBox.Focus();
        };
        PasswordBox.KeyDown += (e, a) =>
        {
            if (a.Key == Keys.Enter)
                LoginButton.Focus();
        };
        layout.Rows.Add(null);
        Content = layout;
        //var app = Application.Current;
#if WINDOWS
        ResourceHelper.PrintAllApplicationResourceDictionaries();
#endif
#if WINDOWS
        try
        {
            if (Program.IsWpf)
            {
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(
                    new System.Windows.ResourceDictionary
                    {
                        Source = new Uri(
                            "pack://application:,,,/EtoFE;component/theming/WpfPlus/WpfPlus/DarkTheme.xaml",
                            UriKind.Absolute
                        ),
                    }
                );
            }
            else
            {
                Console.WriteLine("Not WPF");
            }
        }
        catch (Exception E)
        {
            System.Console.WriteLine($"{E.ToString()}, {E.StackTrace}");
        }
        //System.Windows.Application.Current.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/DynamicAero2;component/Brushes/Dark.xaml", UriKind.RelativeOrAbsolute) });
#endif
        Shown += (_, _) =>
        {
            this.Invalidate(true);
            this.UpdateLayout();
            this.Invalidate();
        };
        layout.Invalidate();
        layout.UpdateLayout();
        MinimumSize = new Eto.Drawing.Size(1280, 720);
        Size = new Eto.Drawing.Size(1280, 720);
        //BackgroundColor = ColorSettings.ForegroundColor;
        (
            new Thread(() =>
            {
                Thread.Sleep(200);
                Application.Instance.Invoke(() =>
                {
                    this.UpdateLayout();
                    this.Invalidate(true);
                });
            })
        ).Start();

        this.Invalidate(true);
    }
}
