using RawPrintingHTTPServer.responses;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace RawPrintingHTTPServer.handlers
{
    class PermissionsHandler
    {
        private RawPrintingHTTPServer server;

        public PermissionsHandler(RawPrintingHTTPServer server)
        {
            this.server = server;
        }

        public ResponseCode handle(HttpListenerRequest req, HttpListenerResponse resp, string accesslog, string origin = null)
        {
            if (req.HttpMethod == "POST")
            {
                return _handlePost(req, resp, accesslog, origin);
            }
            else if (req.HttpMethod == "GET")
            {
                return _handleGet(req, resp, accesslog, origin);
            }
            else
            {
                return ResponseCode.NotFound;
            }
        }

        private ResponseCode _handlePost(HttpListenerRequest req, HttpListenerResponse resp, string accesslog, string origin)
        {
            if (origin != server.config.URL)
            {
                accesslog += "\tfailed\torigin not " + server.config.URL;
                ServerConfig.appendLog(accesslog);
                return ResponseCode.Forbidden;
            }
            if (!req.HasEntityBody)
            {
                accesslog += "\tfailed\tbody required";
                ServerConfig.appendLog(accesslog);
                return ResponseCode.NotFound;
            }

            try
            {
                using (Stream body = req.InputStream)
                {
                    Encoding encoding = req.ContentEncoding;
                    using (StreamReader reader = new StreamReader(body, encoding))
                    {
                        string host = null;
                        string status = null;
                        string[] keyvalues = reader.ReadLine().Split('&');
                        foreach (string query in keyvalues)
                        {
                            string[] keyvalue = query.Split('=');
                            if (keyvalue.Length > 0)
                            {
                                if (keyvalue[0].ToLower() == "host")
                                {
                                    host = Uri.UnescapeDataString(keyvalue[1]);
                                }
                                else if (keyvalue[0].ToLower() == "status")
                                {
                                    status = Uri.UnescapeDataString(keyvalue[1]);
                                }
                            }
                        }
                        body.Close();
                        reader.Close();

                        string html = "<html><body>";
                        if (host != null)
                        {
                            accesslog += "\tsuccess";
                            if (status == "allow")
                            {
                                if (!server.config.allowedDomains.Contains(host))
                                {
                                    server.config.allowedDomains.Add(host);
                                    server.config.save();
                                }
                            }
                            else if (status == "remove")
                            {
                                if (server.config.allowedDomains.Contains(host))
                                {
                                    server.config.allowedDomains.Remove(host);
                                    server.config.save();
                                }
                            }
                            html += "<script>window.opener.postMessage('success', '" + host + "'); window.close();</script>";
                        }
                        else
                        {
                            accesslog += "\tfailed";
                            html += "<script>window.opener.postMessage('failed', '" + host + "'); window.close();</script>";
                        }
                        html += "</body></html>";

                        ServerConfig.appendLog(accesslog);
                        server.responseHTML(resp, html);
                    }
                }
            }
            catch (Exception e)
            {
                ServerConfig.appendLog("Error: " + e.Message + "\n" + e.StackTrace);
                accesslog += "\tfailed";
                ServerConfig.appendLog(accesslog);
            }
            return ResponseCode.OK;
        }

        private ResponseCode _handleGet(HttpListenerRequest req, HttpListenerResponse resp, string accesslog, string origin)
        {
            if (req.QueryString.Count > 0)
            {
                string host = req.QueryString["h"];

                if (host == null || string.IsNullOrEmpty(host.Trim()))
                {
                    return ResponseCode.NotFound;
                }

                string form = "<form method=post onsubmit=\"result = 'submit'\"><h2>Do you want to allow {0} to print?</h2><input type=hidden name=\"host\" value=\"{0}\" /><input type=hidden name=\"status\" value=\"allow\" /><button >Allow</button><button type=button onclick=\"result = 'blocked'; window.close()\">Block</button></form>";
                form = string.Format(form, host);

                string html = "<html><body>";
                html += "<script>var result = 'closed'; window.addEventListener('beforeunload', (event) => { if (result !== 'submit') window.opener.postMessage(result, '" + host + "'); });</script>";
                html += form;
                html += "</body></html>";
                ServerConfig.appendLog(accesslog);
                server.responseHTML(resp, html);
            }
            else
            {
                PermissionResponse json = new PermissionResponse();
                json.allowed = server.config.isAllowed(origin);

                ServerConfig.appendLog(accesslog);
                server.responseJSON(resp, json);
            }
            return ResponseCode.OK;
        }
    }
}
