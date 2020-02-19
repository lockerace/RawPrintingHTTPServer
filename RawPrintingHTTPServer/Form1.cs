using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace RawPrintingHTTPServer
{
    public partial class Form1 : Form
    {
        private string title = "Raw Printing HTTP Server";
        private RawPrintingHTTPServer server;
        private bool isStarted = false;
        private bool isChanging = false;

        public Form1()
        {
            InitializeComponent();
            server = new RawPrintingHTTPServer();
            notifyIcon1.Text = title;
            Text = title;
        }

        public void start()
        {
            if (!isStarted)
            {
                isChanging = true;
                enableStartStop();
                server.Start();
                isStarted = true;
                string text = "Stop";
                startStopToolStripMenuItem.Text = text;
                button1.Text = text;
                isChanging = false;
                enableStartStop();
            }
        }

        public void stop()
        {
            if (isStarted)
            {
                isChanging = true;
                enableStartStop();
                server.Stop();
                isStarted = false;
                string text = "Start";
                startStopToolStripMenuItem.Text = text;
                button1.Text = text;
                isChanging = false;
                enableStartStop();
            }
        }

        private void enableStartStop()
        {
            startStopToolStripMenuItem.Enabled = !isChanging;
            button1.Enabled = !isChanging;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            WindowState = FormWindowState.Normal;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (isStarted)
            {
                stop();
            } else
            {
                start();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.ApplicationExitCall || e.CloseReason == CloseReason.WindowsShutDown || e.CloseReason == CloseReason.TaskManagerClosing)
            {
                stop();
            } else
            {
                WindowState = FormWindowState.Minimized;
                e.Cancel = true;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            start();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Process.Start(ServerConfig.basePath);
        }

    }
}
