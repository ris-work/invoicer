using Eto.Forms;
using RV.InvNew.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EtoFE
{
    internal class InteractiveAuthenticatedSender<Ti, To> : Eto.Forms.Dialog
    {
        bool Error = false;
        public LoginToken token;
        AuthenticatedRequest<Ti> AR;
        Ti Message;
        string Endpoint;
        To Result;
        public InteractiveAuthenticatedSender(string Endpoint, Ti message) {
            Message = message;
            
            this.Endpoint = Endpoint;
        }
        public void Send() {
            bool IsSuccessful = false;
            AR = new AuthenticatedRequest<Ti>(Message, LoginTokens.token);
            while (!IsSuccessful) {
                var response = Program.client.PostAsJsonAsync(Endpoint, AR).GetAwaiter().GetResult();
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
    class AuthenticationForm: Dialog
    {
        public string Username;
        public string Password;
        public bool Elevated;
        public AuthenticationForm(string Endpoint, string Request) {
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
        }
    }
}
