using RawPrintingHTTPServer.responses;
using System.Net;
using System.Text;

namespace RawPrintingHTTPServer.handlers
{
    class PrintersHandler
    {
        private RawPrintingHTTPServer server;

        public PrintersHandler(RawPrintingHTTPServer server)
        {
            this.server = server;
        }

        public ResponseCode handle(HttpListenerRequest req, HttpListenerResponse resp, string accesslog)
        {
            if (req.HttpMethod == "GET")
            {
                return _handleGet(req, resp, accesslog);
            }
            else
            {
                return ResponseCode.NotFound;
            }
        }
        
        private ResponseCode _handleGet(HttpListenerRequest req, HttpListenerResponse resp, string accesslog)
        {
            MachineInfoResponse packet = new MachineInfoResponse
            {
                machineName = System.Environment.MachineName
            };
            packet.printers = ServerConfig.listPrinters();

            ServerConfig.appendLog(accesslog);
            server.responseJSON(resp, packet);
            return ResponseCode.OK;
        }
    }
}
