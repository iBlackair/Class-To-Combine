using System;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;

namespace SimpleHttp
{
    internal static class WebService
    {
        private const int Port = 8000;
        private static readonly HttpListener Listener = new HttpListener
        {
            Prefixes = { $"http://192.168.1.100:{Port}/" }
        };

        private static volatile bool _keepGoing = true;
        private static Task _mainTask;

        public static void StartWebServer() {
            if (_mainTask != null && !_mainTask.IsCompleted)
                return;
            _mainTask = MainLoop();
        }

        public static void StopWebServer() {
            _keepGoing= false;
            lock (Listener)
                Listener.Stop();
            try            {
                _mainTask.Wait();
            } catch(Exception ex){ Trace.WriteLine(ex.Message); }
        }

        private static async Task MainLoop() {
            Listener.Start();
            while(_keepGoing) {
                try {
                    var context = await Listener.GetContextAsync();
                    lock (Listener) {
                        if (_keepGoing) ProcessRequest(context);
                    }
                } catch (HttpListenerException ex) { Trace.WriteLine($"Listener Stopped. {ex.Message}"); return; }
            }
        }

        private static void ProcessRequest(HttpListenerContext context) {
            using (var response = context.Response) {
                try {
                    var handled = false;
                    switch (context.Request.HttpMethod) {
                        case "POST":
                            using (var body = context.Request.InputStream) {
                                using (var reader = new StreamReader(body, context.Request.ContentEncoding)) {
                                    var json = reader.ReadToEnd();

                                    var JsonRes = JsonConvert.DeserializeObject<MacroDroid>(json);
                                    Trace.WriteLine(JsonRes.Content);
                                    handled = true;
                                }
                            }
                            break;
                    }
                    if (!handled){
                        response.StatusCode = 404;
                    }
                }
                catch (Exception ex) {
                    response.StatusCode = 500;
                    Trace.WriteLine(ex.Message);
                }
            }
        }
    }
    public class MacroDroid{
        public string Application { get; set; }
        public string Sender { get; set; }
        public string Content { get; set; }
    }
}
