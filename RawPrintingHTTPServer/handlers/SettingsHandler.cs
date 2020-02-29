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

        public ResponseCode handle(HttpListenerRequest req, HttpListenerResponse resp, string accesslog)
        {
            if (req.HttpMethod == "POST")
            {
                return _handlePost(req, resp, accesslog);
            }
            else
            {
                return ResponseCode.NotFound;
            }
        }

        private ResponseCode _handlePost(HttpListenerRequest req, HttpListenerResponse resp, string accesslog)
        {
            if (!req.HasEntityBody)
            {
                return ResponseCode.NotFound;
            }

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

                        if (newSettings.testingMode != server.config.testingMode)
                        {
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
                ServerConfig.appendLog("Error: " + e.Message + "\n" + e.StackTrace);
                settingsresp.success = false;
                accesslog += "\tfailed";
                ServerConfig.appendLog(accesslog);
            }
            server.responseJSON(resp, settingsresp);
            return ResponseCode.OK;
        }
    }
}
