﻿using System;
using System.Drawing;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace repatriator_client
{
    public partial class MainWindow : Form
    {
        private ConnectionManager connectionManager;
        private int retryFailures = 0;

        private ButterflyControl butterflyControl;

        public MainWindow()
        {
            InitializeComponent();

            connectionManager = ConnectionManager.load();
            connectionManager.connectionUpdate += new Action<ConnectionStatus>(connectionManager_connectionUpdate);
            connectionManager.loginFinished += new Action<LoginStatus>(connectionManager_loginFinished);

            butterflyControl = new ButterflyControl();
            butterflyControl.AnglesMoved += new Action(butterflyControl_AnglesMoved);
            butterflyElemtnHost.Child = butterflyControl;

            // auto connect if we're good to go
            maybeStartConnectionManager();
        }

        private void maybeStartConnectionManager()
        {
            bool goodToGo = connectionManager.hasValidSettings();
            updateConnectionWidgets(goodToGo);
            if (!goodToGo)
                return;
            retryFailures = 0;
            connectionManager.start();
        }
        private void updateConnectionWidgets(bool connecting)
        {
            updateStatus_safe(connecting ? "connecting" : "not connected");
            serverText.Enabled = !connecting;
            userNameText.Enabled = !connecting;
            passwordText.Enabled = !connecting;
            downloadDirectoryText.Enabled = !connecting;
            connectButton.Enabled = !connecting;
        }
        private void updateStatus_safe(string message)
        {
            updateStatus_safe(message, LogLevel.Debug);
        }
        private void updateStatus_safe(string message, LogLevel logLevel)
        {
            Logging.log(message, logLevel);
            Action action = new Action(delegate()
            {
                statusLabel.Text = message;
            });
            if (InvokeRequired)
                BeginInvoke(action);
            else
                action();
        }

        private void connectionManager_connectionUpdate(ConnectionStatus status)
        {
            switch (status)
            {
                case ConnectionStatus.Trouble:
                    retryFailures++;
                    updateStatus_safe("connection trouble" + ".".Repeat(retryFailures));
                    break;
                case ConnectionStatus.Success:
                    updateStatus_safe("connected");
                    break;
                default:
                    throw new Exception();
            }
        }
        private void connectionManager_loginFinished(LoginStatus status)
        {
            switch (status)
            {
                case LoginStatus.ConnectionTrouble:
                    updateStatus_safe("connection trouble. i give up.", LogLevel.Warning);
                    break;
                case LoginStatus.ServerIsBogus:
                    updateStatus_safe("bad server specified", LogLevel.Warning);
                    break;
                case LoginStatus.LoginIsInvalid:
                    updateStatus_safe("invalid login", LogLevel.Warning);
                    break;
                case LoginStatus.InsufficientPrivileges:
                    updateStatus_safe("insufficient privileges", LogLevel.Warning);
                    break;
                case LoginStatus.Cancelled:
                    updateStatus_safe("cancelled");
                    break;
                case LoginStatus.Success:
                    updateStatus_safe("logged in");
                    break;
                default:
                    throw new Exception();
            }
            BeginInvoke(new Action(delegate()
            {
                if (status == LoginStatus.Success)
                    setupPanel.Visible = false;
                else
                    updateConnectionWidgets(false);
            }));
        }

        private void butterflyControl_AnglesMoved()
        {
            butterflySliderX.Value = butterflyControl.AngleX;
            // invert the Y. maybe we shouldn't do this.
            butterflySliderY.Value = butterflySliderY.Maximum - butterflyControl.AngleY;
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            string serverString = serverText.Text;
            string[] nameAndPort = serverString.Split(":");
            if (nameAndPort.Length != 2)
                return;
            string serverName = nameAndPort[0];
            int serverPort;
            if (!int.TryParse(nameAndPort[1], out serverPort))
                return;
            string userName = userNameText.Text;
            string password = passwordText.Text;
            string downloadDirectory = downloadDirectoryText.Text;
            connectionManager.setEverything(serverName, serverPort, userName, password, downloadDirectory);
            maybeStartConnectionManager();
        }

        private void cancelConncetionButton_Click(object sender, EventArgs e)
        {
            connectionManager.cancel();
        }
    }
}
