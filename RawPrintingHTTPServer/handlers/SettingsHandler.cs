using RawPrintingHTTPServer.requests;
using RawPrintingHTTPServer.responses;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace RawPrintingHTTPServer.handlers
{
    class SettingsHandler
    {
        private RawPrintingHTTPServer server;

        public SettingsHandler(RawPrintingHTTPServer server)
        {
            this.server = server;
        }

        public bool handle(HttpListenerRequest req, HttpListenerResponse resp, string accesslog)
        {
            if (req.HttpMethod == "POST")
            {
                return _handlePost(req, resp, accesslog);
            }
            else
            {
                return true;
            }
        }

        private bool _handlePost(HttpListenerRequest req, HttpListenerResponse resp, string accesslog)
        {
            if (req.HasEntityBody)
            {
                SettingsResponse settingsresp = new SettingsResponse();
                try
                {
                    using (Stream body = req.InputStream)
                    {
                        Encoding encoding = req.ContentEncoding;
                        using (StreamReader reader = new StreamReader(body, encoding))
                        {
                            string json = reader.ReadToEnd();
                            SettingsPostBody newSettings = ServerConfig.fromJSON<SettingsPostBody>(json);
                            body.Close();
                            reader.Close();

                            if (newSettings.testingMode != server.config.testingMode) {
                                server.config.testingMode = newSettings.testingMode;
                            }

                            accesslog += "\tsuccess";
                            ServerConfig.appendLog(accesslog);
                            settingsresp.success = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    settingsresp.success = false;
                    accesslog += "\tfailed";
                    ServerConfig.appendLog(accesslog);
                }
                server.responseJSON(resp, settingsresp);
            }
            else
            {
                return true;
            }
            return false;
        }
    }
}
