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
            frm = new Form1();
            frm.Hide();
            frm.start();
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            Application.Run();
        }

        private static Form1 frm;

        private static void OnApplicationExit(object sender, EventArgs e)
        {
            try
            {
                if (frm != null && !frm.IsDisposed)
                {
                    frm.stop();
                    frm.Close();
                }
            }
            catch { }
        }
    }
}
