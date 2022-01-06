/*
Copyright 2018 Intel Corporation

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace MeshCentralDiscovery
{
    public partial class MainForm : Form
    {
        bool forceClose = false;
        bool currentlyVisible = false;
        private MeshDiscovery scanner = null;
        private DateTime closeTime = DateTime.Now;
        private DateTime refreshTime = DateTime.Now;
        private string discoverykey = null;

        public MainForm(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.Length > 5 && arg.Substring(0, 5).ToLower() == "-key:") { discoverykey = arg.Substring(5); }
            }

            InitializeComponent();
        }

        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private void refocus()
        {
            SetForegroundWindow(this.Handle);
            ShowWindow(this.Handle, int.Parse("9"));
        }

        private void mainNotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (DateTime.Now.Subtract(closeTime).Ticks < 1000000) { return; } // If this is another click within a very short time, ignore it.
            if (e == null || e.Button == MouseButtons.Left) { updateView(true); closeTime = DateTime.Now; }
        }

        private void updateView(bool show)
        {
            // Size the vertial
            int count = 0;
            foreach (Control c in mainPanel.Controls) { if ((c.GetType() == typeof(ServerUserControl))) { count++; } }
            if (count < 1) { count = 1; }
            this.Height = refreshButton.Height + (count * 56);

            // Move the window to the bottom right of the mail display
            Rectangle r = Screen.PrimaryScreen.WorkingArea;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width - 8, Screen.PrimaryScreen.WorkingArea.Height - this.Height);

            if (show)
            {
                // Show the dialog box
                Opacity = 100;
                currentlyVisible = Visible = true;
                refocus();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = !forceClose;
            currentlyVisible = Visible = false;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;
            Opacity = 0;

            // Start the multicast scanner
            scanner = new MeshDiscovery(discoverykey);
            scanner.OnNotify += Scanner_OnNotify;
            scanner.MulticastPing();
        }

        private void Scanner_OnNotify(MeshDiscovery sender, System.Net.IPEndPoint source, System.Net.IPEndPoint local, string agentCertHash, string url, string name, string info)
        {
            if (InvokeRequired) { Invoke(new MeshDiscovery.NotifyHandler(Scanner_OnNotify), sender, source, local, agentCertHash, url, name, info); return; }
            AddServer(agentCertHash, name, info, url);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            forceClose = true;
            Application.Exit();
        }

        private void AddServer(string key, string name, string info, string url)
        {
            ServerUserControl match = null;
            foreach (Control c in mainPanel.Controls) { if ((c.GetType() == typeof(ServerUserControl)) && (((ServerUserControl)c).key == key)) { match = (ServerUserControl)c; } }
            if (match == null)
            {
                noServerFoundLabel.Visible = false;
                UserControl cc = new ServerUserControl(this, key, name, info, url);
                mainPanel.Controls.Add(cc);
                cc.Dock = DockStyle.Top;

                /*
                if (name == null) { name = "MeshCentral"; }
                if (info == null) { info = url; }
                mainNotifyIcon.ShowBalloonTip(2000, "MeshCentral", url, ToolTipIcon.None);
                */

                if (Visible == false) { hideTimer.Enabled = true; }
                updateView(true);
            }
            else
            {
                if (match.Update(name, info, url) == true) {
                    if (Visible == false) { hideTimer.Enabled = true; }
                    updateView(true);
                }
            }
        }

        private void RemoveServer(string key)
        {
            ServerUserControl match = null;
            foreach (Control c in mainPanel.Controls) { if ((c.GetType() == typeof(ServerUserControl)) && (((ServerUserControl)c).key == key)) { match = (ServerUserControl)c; } }
            if (match != null) { mainPanel.Controls.Remove(match); }
            if (mainPanel.Controls.Count == 1) { noServerFoundLabel.Visible = true; }
            updateView(false);
        }

        private void RemoveAllServers()
        {
            ArrayList keys = new ArrayList();
            foreach (Control c in mainPanel.Controls) { if (c.GetType() == typeof(ServerUserControl)) { ServerUserControl ctrl = (ServerUserControl)c; keys.Add(ctrl.key); } }
            foreach (string key in keys) { RemoveServer(key); }
        }

        private void RemoveOldServers(DateTime time)
        {
            ArrayList keys = new ArrayList();
            foreach (Control c in mainPanel.Controls) {
                if (c.GetType() == typeof(ServerUserControl)) {
                    ServerUserControl ctrl = (ServerUserControl)c;
                    if (time.Subtract(ctrl.lastUpdate).Ticks > 0) { keys.Add(ctrl.key); }
                }
            }
            foreach (string key in keys) { RemoveServer(key); }
        }

        public void serverClick(ServerUserControl child)
        {
            System.Diagnostics.Process.Start(child.url);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainNotifyIcon_MouseDoubleClick(this, null);
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            refreshTime = DateTime.Now;
            scanner.MulticastPing();
            refreshTimer.Enabled = true;
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            currentlyVisible = Visible = false;
            closeTime = DateTime.Now;
        }

        private void hideTimer_Tick(object sender, EventArgs e)
        {
            currentlyVisible = Visible = false;
            hideTimer.Enabled = false;
        }

        private void refreshTimer_Tick(object sender, EventArgs e)
        {
            RemoveOldServers(refreshTime);
            refreshTimer.Enabled = false;
        }
    }
}
