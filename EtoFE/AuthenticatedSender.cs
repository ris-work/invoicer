using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            out To Output,
            out bool ErrorOccured,
            bool RetryInteractively = false
        )
        {
            var Result = Send(Message, Endpoint, RetryInteractively);
            Output = Result.Out;
            ErrorOccured = Result.Error;
            return Result;
        }

        public static (To Out, bool Error) Send(
            Ti Message,
            string Endpoint,
            bool RetryInteractively = false
        )
        {
            bool success = true;
            string errorMessage = "";
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
            To result = default(To);
            result = Response.Content.ReadAsAsync<To>().GetAwaiter().GetResult();
            RequestLogger.SaveRequest(Endpoint, Message, result, success, errorMessage);
            return (result, false);
        }

        public static (To Out, bool Error) Send(
    RequestLogEntry logEntry,
    bool RetryInteractively = false
)
        {
            try
            {
                Ti message = JsonSerializer.Deserialize<Ti>(logEntry.SerializedRequest);
                return Send(message, logEntry.Endpoint, RetryInteractively);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error replaying request: {ex.Message}");
                return (default(To), true);
            }
        }

        // Add this method to your AuthenticatedRequest class
        // Add this method to your AuthenticatedRequest class
        // Replace the existing ReplayRequest method with this:
        public static (object Out, bool Error) ReplayRequest(RequestLogEntry logEntry, bool retryInteractively = false)
        {
            try
            {
                // Check if we have the type names
                if (string.IsNullOrEmpty(logEntry.RequestTypeName) || string.IsNullOrEmpty(logEntry.ResponseTypeName))
                {
                    return (null, true);
                }

                // Get the types from the type names
                var requestType = Type.GetType(logEntry.RequestTypeName);
                var responseType = Type.GetType(logEntry.ResponseTypeName);

                if (requestType == null || responseType == null)
                {
                    return (null, true);
                }

                // Deserialize the request
                var requestJson = logEntry.SerializedRequest;
                var request = JsonSerializer.Deserialize(requestJson, requestType);

                // Use reflection to call the generic Send method
                var sendMethod = typeof(SendAuthenticatedRequest<,>).MakeGenericType(requestType, responseType).GetMethod("Send", new[] { typeof(RequestLogEntry), typeof(bool) });
                var result = sendMethod.Invoke(null, new object[] { logEntry, retryInteractively });

                // Extract the result from the tuple
                var resultProperty = result.GetType().GetProperty("Out");
                var errorProperty = result.GetType().GetProperty("Error");

                return (resultProperty.GetValue(result), (bool)errorProperty.GetValue(result));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error replaying request: {ex.Message}");
                return (null, true);
            }
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

    // Helper class for storing request/response data
    public class RequestLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Endpoint { get; set; } = string.Empty;
        public string SerializedRequest { get; set; } = string.Empty;
        public string SerializedResponse { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        // Store the actual Type objects
        [JsonIgnore]  // Don't serialize these directly
        public Type RequestType { get; set; }

        [JsonIgnore]  // Don't serialize these directly
        public Type ResponseType { get; set; }

        // Store the type names for serialization
        public string RequestTypeName
        {
            get => RequestType?.AssemblyQualifiedName;
            set => RequestType = string.IsNullOrEmpty(value) ? null : Type.GetType(value);
        }

        public string ResponseTypeName
        {
            get => ResponseType?.AssemblyQualifiedName;
            set => ResponseType = string.IsNullOrEmpty(value) ? null : Type.GetType(value);
        }
    }

    // Static class for handling request logging
    public static class RequestLogger
    {
        private static string LogFilePath = "PastRequests.json";
        private static List<RequestLogEntry> _requestLogs = new List<RequestLogEntry>();

        static RequestLogger()
        {
            LoadLogsFromFile();
        }

        public static void Initialize()
        {
            LoadLogsFromFile();
        }

        private static void LoadLogsFromFile()
        {
            try
            {
                if (!File.Exists(LogFilePath))
                {
                    _requestLogs = new List<RequestLogEntry>();
                    PersistLogsToFile();
                    return;
                }

                var content = File.ReadAllText(LogFilePath);
                _requestLogs = JsonSerializer.Deserialize<List<RequestLogEntry>>(content) ?? new List<RequestLogEntry>();
            }
            catch
            {
                // Rename invalid file and create new one
                string invalidPath = $"{LogFilePath}.invalid_{DateTime.Now:yyyyMMddHHmmss}";
                File.Move(LogFilePath, invalidPath);
                _requestLogs = new List<RequestLogEntry>();
                PersistLogsToFile();
            }
        }

        private static void PersistLogsToFile()
        {
            try
            {
                File.WriteAllText(LogFilePath, JsonSerializer.Serialize(_requestLogs, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RequestLogger] Error persisting logs to file: {ex.Message}");
            }
        }

        public static void SaveRequest<TRequest, TResponse>(
    string endpoint,
    TRequest request,
    TResponse response,
    bool success,
    string errorMessage = "")
        {
            try
            {
                _requestLogs.Add(new RequestLogEntry
                {
                    Endpoint = endpoint,
                    SerializedRequest = JsonSerializer.Serialize(request),
                    SerializedResponse = JsonSerializer.Serialize(response),
                    Success = success,
                    ErrorMessage = errorMessage,
                    RequestType = typeof(TRequest),
                    ResponseType = typeof(TResponse)
                });

                PersistLogsToFile();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RequestLogger] Error saving request log: {ex.Message}");
            }
        }

        public static List<RequestLogEntry> GetRequestLogs()
        {
            return _requestLogs;
        }

        public static void ClearLogs()
        {
            _requestLogs = new List<RequestLogEntry>();
            PersistLogsToFile();
        }
    }
    // Helper class for converting between tuples and serializable objects
    public static class RequestResultHelper
    {
        public static RequestResult<T> ToResult<T>(T data, bool error = false, string errorMessage = "")
        {
            return new RequestResult<T>
            {
                Data = data,
                Error = error,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.Now
            };
        }

        public static (T Out, bool Error) FromResult<T>(RequestResult<T> result)
        {
            return (result.Data, result.Error);
        }
    }

    // Serializable result object
    public class RequestResult<T>
    {
        public T Data { get; set; }
        public bool Error { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
