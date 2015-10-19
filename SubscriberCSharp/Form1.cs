using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using CompactWebServer;
using System.Net;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using PubSubApi;

namespace SubscriberCSharp
{
    public partial class Form1 : Form
    {
        SubscriberWebServer webServer;
        WebServerConfiguration webConf;
        string clientId;

        public Form1()
        {
            InitializeComponent();
            // use filename as clientid e.g. SubscriberCSharp1.exe, SubscriberCSharp2.exe...
            clientId = System.AppDomain.CurrentDomain.FriendlyName.Trim().Replace(".exe", "");
            this.Text = clientId;
        }

        private void TryStartServer()
        {
            int t = 10;

            // for simplicity, try a random port
            while (!StartServer() && t > 0)
            {
                t--;
                Thread.Sleep(1000);
            }
        }

        private bool StartServer()
        {
            try
            {
                var rnd = new Random();

                webConf = new WebServerConfiguration();
                webConf.IPAddress = IPAddress.Any;
                webConf.Port = 8000 + rnd.Next(99);

                webServer = new SubscriberWebServer(webConf);
                webServer.OnLogEvent += Log;
                webServer.OnNotifyPosition += OnPostionUpdate;
                webServer.Start();

                return true;
            }
            catch { return false; };
        }

        private void SubscribePositionEvent()
        {
            try
            {
                var data = new Subscription() 
                { 
                    ClientId = clientId,
                    Endpoint = string.Format("http://localhost:{0}/notify", webConf.Port),
                    SubscribedEvent = "position"
                };

                WebClient.PostJsonAsync(
                    string.Format("http://localhost:{0}/subscribe", DefaultSettings.BrokerPort),
                    data,
                    OnSubscribed);
            }
            catch (Exception ex)
            {
                this.Log("info", string.Format("cannot subscribe to PositionEvent: {0}", ex.Message));
            }
        }

        private void OnSubscribed(string responseText)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new WaitCallback(obj => OnSubscribed(obj.ToString())), new object[] { responseText });
                    return;
                }

                var response = JsonConvert.DeserializeObject<ResponseOK>(responseText);
                if (response.Success)
                    this.Log("info", string.Format("subscribed to PositionEvent successfully!"));
            }
            catch { }
        }

        private void UnsubscribePositionEvent()
        {
            try
            {
                var data = new Subscription()
                {
                    ClientId = clientId,
                    SubscribedEvent = "position"
                };

                WebClient.PostJsonAsync(
                    string.Format("http://localhost:{0}/unsubscribe", DefaultSettings.BrokerPort),
                    data,
                    OnUnsubscribed);
            }
            catch (Exception ex)
            {
                this.Log("info", string.Format("cannot subscribe to PositionEvent: {0}", ex.Message));
            }
        }

        private void OnUnsubscribed(string responseText)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new WaitCallback(obj => OnSubscribed(obj.ToString())), new object[] { responseText });
                    return;
                }

                var response = JsonConvert.DeserializeObject<ResponseOK>(responseText);
                if (response.Success)
                    this.Log("info", string.Format("unsubscribed to PositionEvent successfully!"));
            }
            catch { }
        }

        private void Log(string type, string message)
        {
            if (type != "info")
                return;

            ReportProgress(string.Format(">> {0}\r\n", message));
        }

        private void OnPostionUpdate(object eventData)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new WaitCallback(OnPostionUpdate), new object[] { eventData });
                return;
            }

            var positionEvent = (PublishedResource<PositionEvent>)eventData;
            lblLat.Text = positionEvent.EventData.Lat;
            lblLong.Text = positionEvent.EventData.Long;
        }

        void ReportProgress(object message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new WaitCallback(ReportProgress), new object[] { message });
                return;
            }

            this.LogTextBox.Text += message;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TryStartServer();

            if (!webServer.Running)
                return;

            SubscribePositionEvent();
            // ... many events are welcome

            this.Log("info", string.Format("listening on port {0}...", webConf.Port));

            button1.Enabled = false;
            button2.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            webServer.Stop();

            UnsubscribePositionEvent();

            button1.Enabled = true;
            button2.Enabled = false;
        }

        private void Form1_Closing(object sender, CancelEventArgs e)
        {
            if (webServer != null)
                webServer.Stop();
        }
    }
}