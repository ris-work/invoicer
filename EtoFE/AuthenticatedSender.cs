using Eto.Forms;
using RV.InvNew.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Net.Http;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

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
        public InteractiveAuthenticatedSender(string Endpoint, Ti message) {
            Message = message;
            
            this.Endpoint = Endpoint;
        }
        public void Send(string Username, string Password, bool Elevated = false) {
            LoginToken logint = null;
            string Terminal = (string)Config.modelDict.GetValueOrDefault("Terminal", "Default Terminal");
            string AuthURL = Elevated ? "/ElevatedLogin" : "/Login";
            LoginCredentials l = new(Username, Password, Terminal, null);
            var responseA = Program.client.PostAsJsonAsync("/Login", l);
            var resultA = responseA.GetAwaiter().GetResult();
            resultA.EnsureSuccessStatusCode();
            var logint_w = resultA.Content.ReadAsAsync<LoginToken>();
            logint = logint_w.GetAwaiter().GetResult();
            token = logint;
            MessageBox.Show(logint.TokenID);
            MessageBox.Show(logint.Token);
            MessageBox.Show(logint.Error);
            if (logint.Error != "") { Error = true; }
            bool IsSuccessful = false;
            AR = new AuthenticatedRequest<Ti>(Message, LoginTokens.token);
            
            while (!IsSuccessful) {
                var responseT = Program.client.PostAsJsonAsync(Endpoint, AR);
                var response = responseT.GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsAsync<To>().GetAwaiter().GetResult();
                    Result = result;
                }
                else if(response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Error = true;
                }
            }
        }
    }
    class AuthenticationForm<Ti, To>: Dialog
    {
        public string Username;
        public string Password;
        public bool Elevated = false;
        public Ti Request;
        public To Response;
        public bool Error = false;
        public AuthenticationForm(string Endpoint, Ti Request) {
            var LU = new Label();
            var LP = new Label();
            var TU = new TextBox();
            var TP = new TextBox();
            var TEndpoint = new TextArea() { ReadOnly = true, Size = new Eto.Drawing.Size(400, 100) };
            var TRequest = new TextArea() { ReadOnly = true, Size = new Eto.Drawing.Size(400, 200) };
            var ElevatedLoginButton = new Button() { Text = "⬆ Elevated Login" };
            var LoginButton = new Button() { Text = "📩 Login" };
            var Cancel = new Button() { Text = "✖ Cancel" };
            StackLayout U = new StackLayout(LU, TU) { Orientation = Orientation.Horizontal };
            StackLayout P = new StackLayout(LP, TP) { Orientation = Orientation.Horizontal };
            StackLayout RequestInfo = new StackLayout(TEndpoint, TRequest) { Orientation = Orientation.Vertical };
            StackLayout Actions = new StackLayout(ElevatedLoginButton, LoginButton) { Orientation = Orientation.Horizontal };
            Content = new StackLayout(U, P, RequestInfo, Actions);
            this.Request = Request;
            LoginButton.Click += (e, a) => {
                InteractiveAuthenticatedSender<Ti, To> Sender = new(Endpoint, Request);
                Sender.Send(TU.Text, TP.Text, false);
                Response = Sender.Result;
                Error = Sender.Error;
            };
            ElevatedLoginButton.Click += (e, a) => {
                InteractiveAuthenticatedSender<Ti, To> Sender = new(Endpoint, Request);
                Sender.Send(TU.Text, TP.Text, true);
                Response = Sender.Result;
                Elevated = true;
                Error = Sender.Error;
            };
        }
    }
}
