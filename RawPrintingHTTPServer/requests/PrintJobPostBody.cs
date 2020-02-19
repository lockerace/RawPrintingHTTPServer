using System;

namespace RawPrintingHTTPServer.requests
{
    class PrintJobPostBody
    {
        public string printer;
        public string id;
        public string data;

        public byte[] DataToByteArray()
        {
            if (data != null)
            {
                return Convert.FromBase64String(data);
            }
            return null;
        }
    }
}
