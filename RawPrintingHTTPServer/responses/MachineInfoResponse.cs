using System.Collections.Generic;

namespace RawPrintingHTTPServer.responses
{
    class MachineInfoResponse
    {
        public string machineName;
        public List<string> printers { get; set; } = new List<string>();
    }
}
