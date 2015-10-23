using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Net;
using System.IO;
using PubSubApi;
using Newtonsoft.Json;
using System.Threading;

namespace PublisherCSharp
{
    public partial class Form1 : Form
    {
        PositionEvent data;

        public Form1()
        {
            InitializeComponent();

            this.Text = Assembly.GetExecutingAssembly().FullName.Split(new char[] { ' ', ',' })[0];
        }


        private void button1_Click(object sender, EventArgs e)
        {
            data = new PositionEvent()
            {
                Long = txtLong.Text,
                Lat = txtLat.Text
            };

            LogTextBox.Text += string.Format(">> Publishing event...\r\n");
            WebClient.PostJsonAsync(
                string.Format("http://localhost:{0}/publish", DefaultSettings.BrokerPort),
                data,
                OnPublishResponse);
        }

        private void OnPublishResponse(string responseText)
        {
            if (string.IsNullOrEmpty(responseText))
                return;

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new WaitCallback(obj => OnPublishResponse(obj.ToString())), new object[] { responseText });
                    return;
                }

                var response = JsonConvert.DeserializeObject<ResponseOK>(responseText);
                if (response != null && response.Success)
                {
                    LogTextBox.Text += string.Format(">> Event published: ({0},{1})\r\n", data.Long, data.Lat);
                    return;
                }
            }
            catch { }
        }
    }
}