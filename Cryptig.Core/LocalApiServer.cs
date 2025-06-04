using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.IO;

namespace Cryptig.Core
{
    public class LocalApiServer
    {
        private readonly HttpListener _listener = new();
        private readonly MistigVault _vault;
        private readonly string _token;
        private bool _running;

        public string Token => _token;
        public int Port { get; }

        public LocalApiServer(MistigVault vault, int port = 5005)
        {
            _vault = vault;
            Port = port;
            _token = Guid.NewGuid().ToString();
            _listener.Prefixes.Add($"http://localhost:{port}/");
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _listener.Start();
            _listener.BeginGetContext(ProcessRequest, null);
        }

        public void Stop()
        {
            _running = false;
            if (_listener.IsListening)
                _listener.Stop();
        }

        private void ProcessRequest(IAsyncResult ar)
        {
            if (!_running) return;
            HttpListenerContext context;
            try
            {
                context = _listener.EndGetContext(ar);
            }
            catch
            {
                return;
            }
            _listener.BeginGetContext(ProcessRequest, null);

            var authHeader = context.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || authHeader != $"Bearer {_token}")
            {
                context.Response.StatusCode = 401;
                context.Response.Close();
                return;
            }

            if (context.Request.HttpMethod == "GET" && context.Request.Url?.AbsolutePath == "/entries")
            {
                var json = JsonSerializer.Serialize(_vault.Data.Entries);
                byte[] data = Encoding.UTF8.GetBytes(json);
                context.Response.ContentType = "application/json";
                context.Response.OutputStream.Write(data, 0, data.Length);
                context.Response.Close();
                return;
            }

            if (context.Request.HttpMethod == "POST" && context.Request.Url?.AbsolutePath == "/entries")
            {
                using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                var body = reader.ReadToEnd();
                var entry = JsonSerializer.Deserialize<VaultEntry>(body);
                if (entry != null)
                {
                    _vault.AddEntry(entry);
                    _vault.Save();
                    context.Response.StatusCode = 200;
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
                context.Response.Close();
                return;
            }

            context.Response.StatusCode = 404;
            context.Response.Close();
        }
    }
}
