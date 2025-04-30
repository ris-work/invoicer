using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Eto.Forms;
using RV.InvNew.Common;
#if WINDOWS
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
#endif

namespace EtoFE
{
    internal class InteractiveAuthenticatedSender<Ti, To> : Eto.Forms.Dialog
    {
        public bool Error = false;
        public LoginToken token;
        AuthenticatedRequest<Ti> AR;
        Ti Message;
        string Endpoint;
        public To Result;
        bool Bearer;

        public InteractiveAuthenticatedSender(string Endpoint, Ti message, bool Bearer = false)
        {
            Message = message;
            this.Bearer = Bearer;
            this.Endpoint = Endpoint;
        }

        public void Send(
            string Username,
            string Password,
            bool Elevated = false,
            bool RetryOnFailure = true
        )
        {
            LoginToken logint = null;
            string Terminal = (string)
                Program.ConfigDict.GetValueOrDefault("Terminal", "Default Terminal");
            string AuthURL = Elevated ? "/ElevatedLogin" : "/Login";
            LoginCredentials l = new(Username, Password, Terminal, null);
            var responseA = Program.client.PostAsJsonAsync(AuthURL, l);
            var resultA = responseA.GetAwaiter().GetResult();
            resultA.EnsureSuccessStatusCode();
            var logint_w = resultA.Content.ReadAsAsync<LoginToken>();
            logint = logint_w.GetAwaiter().GetResult();
            token = logint;
            MessageBox.Show(
                $"{logint.TokenID}, {logint.Token}, {logint.Error}",
                "Login information",
                MessageBoxType.Information
            );
            if (logint.Error != "")
            {
                Error = true;
            }
            bool IsSuccessful = false;
            if (Bearer == false)
            {
                AR = new AuthenticatedRequest<Ti>(Message, token);

                var responseT = Program.client.PostAsJsonAsync(Endpoint, AR);
                var response = responseT.GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsAsync<To>().GetAwaiter().GetResult();
                    Result = result;
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Error = true;
                }
                else
                {
                    Error = true;
                }
            }
            else
            {
                var JR = JsonSerializer.Serialize(Message);
                var Request = new HttpRequestMessage()
                {
                    Content = new StringContent(JR),
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(Program.client.BaseAddress, Endpoint),
                };
                Request.Headers.Add(
                    "Authorization",
                    $"Bearer {Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(logint)))}"
                );
                Console.WriteLine(JR);
                var Posted = Program.client.SendAsync(Request);
                var Response = Posted.GetAwaiter().GetResult();
                Response.EnsureSuccessStatusCode();
                var result = Response.Content.ReadAsAsync<To>().GetAwaiter().GetResult();
                Result = result;
            }
        }
    }

    public static class SendAuthenticatedRequest<Ti, To>
    {
        public static (To Out, bool Error) Send(
            Ti Message,
            string Endpoint,
            bool RetryInteractively = false
        )
        {
            var JR = JsonSerializer.Serialize(Message);
            var Request = new HttpRequestMessage()
            {
                Content = new StringContent(JR),
                Method = HttpMethod.Post,
                RequestUri = new Uri(Program.client.BaseAddress, Endpoint),
            };
            Request.Headers.Add(
                "Authorization",
                $"Bearer {Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(LoginTokens.token)))}"
            );
            Console.WriteLine(JR);
            var Posted = Program.client.SendAsync(Request);
            var Response = Posted.GetAwaiter().GetResult();
            Response.EnsureSuccessStatusCode();
            var result = Response.Content.ReadAsAsync<To>().GetAwaiter().GetResult();
            return (result, false);
        }
    }

    class AuthenticationForm<Ti, To> : Dialog
    {
        public string Username;
        public string Password;
        public bool Elevated = false;
        public Ti Request;
        public To Response;
        public bool Error = false;
        public bool Bearer = false;

        public AuthenticationForm(string Endpoint, Ti Request, bool Bearer = false)
        {
            var LU = new Label() { Text = "Username" };
            var LP = new Label() { Text = "Password" };
            var TU = new TextBox();
            var TP = new PasswordBox();
            var TEndpoint = new TextArea()
            {
                ReadOnly = true,
                Size = new Eto.Drawing.Size(400, 100),
                Text = Endpoint,
            };
            var TRequest = new TextArea()
            {
                ReadOnly = true,
                Size = new Eto.Drawing.Size(400, 200),
                Text = JsonSerializer.Serialize<Ti>(Request),
            };
            var ElevatedLoginButton = new Button() { Text = "⬆ Elevated Login" };
            var LoginButton = new Button() { Text = "📩 Login" };
            var CancelButton = new Button() { Text = "✖ Cancel" };
            TableRow U = new TableRow(LU, null, TU) { };
            TableRow P = new TableRow(LP, null, TP) { };
            StackLayout RequestInfo = new StackLayout(TEndpoint, TRequest)
            {
                Orientation = Orientation.Vertical,
                Padding = 10,
                Spacing = 10,
            };
            StackLayout Actions = new StackLayout(ElevatedLoginButton, LoginButton, CancelButton)
            {
                Orientation = Orientation.Horizontal,
                Padding = 10,
                Spacing = 10,
            };
            Content = new StackLayout(new TableLayout(U, P) { Padding = 10 }, RequestInfo, Actions)
            {
                Padding = 10,
                Spacing = 10,
            };
            this.Request = Request;
            this.Bearer = Bearer;
            LoginButton.Click += (e, a) =>
            {
                InteractiveAuthenticatedSender<Ti, To> Sender = new(Endpoint, Request, Bearer);
                Sender.Send(TU.Text, TP.Text, false);
                Response = Sender.Result;
                Error = Sender.Error;
                this.Close();
            };
            ElevatedLoginButton.Click += (e, a) =>
            {
                InteractiveAuthenticatedSender<Ti, To> Sender = new(Endpoint, Request, Bearer);
                Sender.Send(TU.Text, TP.Text, true);
                Response = Sender.Result;
                Elevated = true;
                Error = Sender.Error;
                this.Close();
            };
            TU.KeyDown += (e, a) =>
            {
                if (a.Key == Keys.Enter)
                {
                    TP.Focus();
                }
            };
            TP.KeyDown += (e, a) =>
            {
                if (a.Key == Keys.Enter)
                {
                    LoginButton.Focus();
                }
            };
        }
    }
}
