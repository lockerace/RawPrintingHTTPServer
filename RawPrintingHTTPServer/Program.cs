using System;
using System.Windows.Forms;

namespace RawPrintingHTTPServer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form1 frm = new Form1();
            frm.Hide();
            frm.start();
            Application.Run();
        }
    }
}
