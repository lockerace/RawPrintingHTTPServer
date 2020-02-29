using RawPrintingHTTPServer.handlers;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RawPrintingHTTPServer
{
    enum ResponseCode {
        OK = 0,
        NotFound = 1,
        Forbidden = 2
    }
    class RawPrintingHTTPServer
    {
        private HttpListener _listener;
        private int _requestCount = 0;
        public ServerConfig config;
        private Task _task;
        private PermissionsHandler _permissions;
        private HomeHandler _home;
        private PrintersHandler _printers;
        private SettingsHandler _settings;
        public bool IsListening { get; private set; } = false;

        public RawPrintingHTTPServer()
        {
            config = ServerConfig.load();
            _permissions = new PermissionsHandler(this);
            _home = new HomeHandler(this);
            _printers = new PrintersHandler(this);
            _settings = new SettingsHandler(this);
        }

        private string _getOrigin(HttpListenerRequest req)
        {
            string origin = config.URL;
            string idx = null;
            for (int i = 0; i < req.Headers.Keys.Count; i++)
            {
                string key = req.Headers.Keys[i];
                if (key.ToLower().Equals("origin"))
                {
                    idx = key;
                    break;
                }
            }
            if (idx != null)
            {
                origin = req.Headers[idx];
            }
            else if (req.UrlReferrer != null)
            {
                origin = req.UrlReferrer.Scheme + "://" + req.UrlReferrer.Host;
                if (req.UrlReferrer.Port != 80)
                {
                    origin += ":" + req.UrlReferrer.Port;
                }
            }
            return origin;
        }

        private Task _listen()
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    while (IsListening)
                    {
                        HttpListenerContext ctx = _listener.GetContext();
                        HttpListenerRequest req = ctx.Request;
                        HttpListenerResponse resp = ctx.Response;

                        try
                        {
                            string accesslog = DateTime.Now.ToString("o");
                            accesslog += "\t" + string.Format("Request #: {0}", ++_requestCount);
                            accesslog += "\t" + req.Url.ToString();
                            accesslog += "\t" + req.HttpMethod;
                            accesslog += "\t" + req.UserHostAddress;
                            accesslog += "\t" + req.UserAgent;

                            string origin = _getOrigin(req);
                            accesslog += "\t" + origin;

                            bool forbidden = false;
                            if (config.URL == origin || config.allowedDomains.Contains(origin))
                            {
                                resp.AppendHeader("Access-Control-Allow-Origin", origin);
                            }
                            else
                            {
                                if (req.Url.AbsolutePath == "/permissions" && req.HttpMethod == "GET")
                                {
                                    resp.AppendHeader("Access-Control-Allow-Origin", origin);
                                } else
                                {
                                    resp.AppendHeader("Access-Control-Allow-Origin", config.URL);
                                    forbidden = true;
                                }
                            }
                            if (req.HttpMethod == "OPTIONS")
                            {
                                resp.AddHeader("Access-Control-Allow-Headers", "*");
                                resp.StatusCode = (int)HttpStatusCode.OK;
                                resp.StatusDescription = "OK";
                                resp.Close();
                            }
                            else
                            {
                                if (forbidden)
                                {
                                    accesslog += "\tfailed\tsame origin";
                                    ServerConfig.appendLog(accesslog);
                                    responseForbidden(resp);
                                }
                                else
                                {
                                    ResponseCode respCode = ResponseCode.OK;
                                    if (req.Url.AbsolutePath == "/")
                                    {
                                        respCode = _home.handle(req, resp, accesslog);
                                    }
                                    else if (req.Url.AbsolutePath == "/permissions")
                                    {
                                        respCode = _permissions.handle(req, resp, accesslog, origin);
                                    }
                                    else if (req.Url.AbsolutePath == "/printers")
                                    {
                                        respCode = _printers.handle(req, resp, accesslog);
                                    }
                                    else if (req.Url.AbsolutePath == "/settings")
                                    {
                                        respCode = _settings.handle(req, resp, accesslog);
                                    }
                                    else
                                    {
                                        respCode = ResponseCode.NotFound;
                                    }

                                    switch(respCode)
                                    {
                                        case ResponseCode.NotFound:
                                            accesslog += "\tfailed\tnot found";
                                            ServerConfig.appendLog(accesslog);
                                            resp.StatusCode = (int)HttpStatusCode.NotFound;
                                            resp.StatusDescription = "NOT FOUND";
                                            resp.Close();
                                            break;
                                        case ResponseCode.Forbidden:
                                            responseForbidden(resp);
                                            break;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            ServerConfig.appendLog("Error: " + e.Message + "\n" + e.StackTrace);
                            resp.StatusCode = (int)HttpStatusCode.InternalServerError;
                            resp.StatusDescription = "INTERNAL SERVER ERROR";
                            resp.Close();
                        }
                    }
                } catch (Exception e)
                {
                    if (!(e is HttpListenerException && (e as HttpListenerException).ErrorCode == 995))
                    {
                        ServerConfig.appendLog(e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
                    }
                    return false;
                }
                return true;
            });
        }
        
        public void Start()
        {
            if (_listener == null)
            {
                IsListening = true;
                string url = config.URL + "/";
                _listener = new HttpListener();
                _listener.Prefixes.Add(url);
                _listener.Start();

                _task = _listen();

                ServerConfig.appendLog(string.Format("Listening for connections on {0}", url));
            }
        }

        public void Stop()
        {
            if (IsListening)
            {
                IsListening = false;
                // Close the listener
                if (_listener != null)
                {
                    _listener.Stop();
                    _listener.Close();
                    _listener = null;
                }
                config.save();
                if (_task != null)
                {
                    _task.Wait();
                    if (_task.IsFaulted)
                    {
                        Exception e = _task.Exception;
                        while (e != null)
                        {
                            ServerConfig.appendLog("ErrorStop: " + _task.Exception.Message + "\n" + _task.Exception.StackTrace);
                            e = e.InnerException;
                        }
                    }
                    _task.Dispose();
                    _task = null;
                }
            }
        }

        public void responseForbidden(HttpListenerResponse resp)
        {
            resp.StatusCode = (int)HttpStatusCode.Forbidden;
            resp.StatusDescription = "FORBIDDEN";
            resp.Close();
        }

        public void response(HttpListenerResponse resp, byte[] data, string contentType)
        {
            resp.ContentType = contentType;
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;
            resp.OutputStream.Write(data, 0, data.Length);
            resp.Close();
        }

        public void responseJSON(HttpListenerResponse resp, object obj)
        {
            byte[] data = Encoding.UTF8.GetBytes(ServerConfig.toJSON(obj));
            response(resp, data, "application/json");
        }

        public void responseHTML(HttpListenerResponse resp, string html)
        {
            byte[] data = Encoding.UTF8.GetBytes(html);
            response(resp, data, "text/html");
        }
    }
}
