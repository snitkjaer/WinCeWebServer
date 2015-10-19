using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System;
using Newtonsoft.Json;

namespace CompactWebServer
{
    public class HttpResponse
    {
        private Dictionary<string, string> _header;

        public Dictionary<string, string> Header
        {
            get { return _header; }
            set { _header = value; }
        }

        WebServer _app;
        TcpClient _tcpClient;

        public HttpResponse(WebServer app, TcpClient tcpClient)
        {
            this._app = app;
            this._tcpClient = tcpClient;
            this.Header = new Dictionary<string, string>();
        }

        public void AddHeader(string key, string value)
        {
            if(!this.Header.ContainsKey(key))
                this.Header.Add(key, value);
        }

        public void RemoveHeader(string key)
        {
            if(this.Header.ContainsKey(key))
                this.Header.Remove(key);
        }

        /// <summary>
        /// Sends text/plain to client
        /// </summary>
        /// <param name="text">string text</param>
        public void SendText(string text)
        {
            SendHeader("text/plain", Encoding.UTF8.GetBytes(text).Length, StatusCode.OK);
            SendToClient(text);
        }

        /// <summary>
        /// Sends text/html to client
        /// </summary>
        /// <param name="text">string html</param>
        public void SendHtml(string html)
        {
            SendHeader("text/html", html.Length, StatusCode.OK);
            SendToClient(html);
            End();
        }

        /// <summary>
        /// Sends formatted json to client
        /// </summary>
        /// <param name="data">object data</param>
        public void SendJson(object data)
        {
            //TODO: try to json encode and sent it to client
            string json = JsonConvert.SerializeObject(data);
            SendHeader("application/json", json.Length, StatusCode.OK);
            SendToClient(json);
            End();
        }

        /// <summary>
        /// Sends error page to the client
        /// </summary>
        /// <param name="statusCode">Status code</param>
        /// <param name="message">Error message</param>
        public void SendError(StatusCode statusCode, string message)
        {
            string page = GetErrorPage(statusCode, message);
            SendHeader("text/html", page.Length, statusCode);
            SendToClient(page);
            End();
        }

        /// <summary>
        /// Sends file to the client
        /// </summary>
        /// <param name="filePath">File Path</param>
        public void SendFile(string filePath)
        {
            StatusCode statusCode = StatusCode.OK;
            if (File.Exists(filePath))
            {
                FileStream fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                SendHeader(null, fStream.Length, StatusCode.OK);
                SendStream(fStream);

                fStream.Close();
            }
            else
            {
                statusCode = StatusCode.NotFound;
                GetErrorPage(statusCode, "File not found");
            }

            End();
        }

        /// <summary>
        /// Redirect to url with status code 302
        /// </summary>
        /// <param name="url">Redirect URL</param>
        public void Redirect(string url)
        {
            //TODO: Redirect to url with status code 302
        }

        /// <summary>
        /// Generates error page
        /// </summary>
        /// <param name="statusCode">StatusCode</param>
        /// <param name="message">Message</param>
        /// <returns>ErrorPage</returns>
        private string GetErrorPage(StatusCode statusCode, string message)
        {
            string status = GetStatusCode(statusCode);

            StringBuilder errorMessage = new StringBuilder();
            errorMessage.Append("<html>\n");
            errorMessage.Append("<head>\n");
            errorMessage.Append(string.Format("<title>{0}</title>\n", status));
            errorMessage.Append("</head>\n");
            errorMessage.Append("<body>\n");
            errorMessage.Append(string.Format("<h1>{0}</h1>\n", status));
            errorMessage.Append(string.Format("<p>{0}</p>\n", message));
            errorMessage.Append("<hr>\n");
            errorMessage.Append(string.Format("<address>{0} Server at {1} Port {2} </address>\n", 
                this._app.Configuration.ServerName, 
                this._app.Configuration.IPAddress, 
                this._app.Configuration.Port));
            errorMessage.Append("</body>\n");
            errorMessage.Append("</html>\n");
            return errorMessage.ToString();
        }


        /// <summary>
        /// Sends HTTP header
        /// </summary>
        /// <param name="mimeType">Mime Type</param>
        /// <param name="totalBytes">Length of the response</param>
        /// <param name="statusCode">Status code</param>
        private void SendHeader(string mimeType, long totalBytes, StatusCode statusCode)
        {
            if (string.IsNullOrEmpty(mimeType))
            {
                mimeType = "text/html";
            }

            StringBuilder header = new StringBuilder();
            header.Append(string.Format("HTTP/1.1 {0}\r\n", GetStatusCode(statusCode)));
            header.Append(string.Format("Content-Type: {0}\r\n", mimeType));
            header.Append(string.Format("Accept-Ranges: bytes\r\n"));
            header.Append(string.Format("Server: {0}\r\n", _app.Configuration.ServerName));
            header.Append(string.Format("Connection: close\r\n"));
            header.Append(string.Format("Content-Length: {0}\r\n", totalBytes));

            foreach (var h in this.Header)
            {
                header.Append(string.Format("{0}: {1}\r\n", h.Key, h.Value));
            }
            header.Append("\r\n");

            SendToClient(header.ToString());
        }

        /// <summary>
        /// Sends stream to the client
        /// </summary>
        /// <param name="stream"></param>
        private void SendStream(Stream stream)
        {
            byte[] buffer = new byte[10240];
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0) SendToClient(buffer, bytesRead);
                else break;
            }
        }

        /// <summary>
        /// Send string data to client
        /// </summary>
        /// <param name="data">String data</param>
        private void SendToClient(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            SendToClient(bytes, bytes.Length);
        }

        /// <summary>
        /// Sends byte array to client
        /// </summary>
        /// <param name="data">Data array</param>
        /// <param name="bytesTosend">Data length</param>
        private void SendToClient(byte[] data, int bytesTosend)
        {
            try
            {
                Socket socket = this._tcpClient.Client;

                if (socket.Connected)
                {
                    int sentBytes = socket.Send(data, 0, bytesTosend, 0);
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine("Error: " + e.ToString());
            }
        }

        public void End()
        {
            _tcpClient.Client.Close();
            _tcpClient.Close();
        }

        /// <summary>
        /// Gets string representation for the status code
        /// </summary>
        /// <param name="statusCode">Status code</param>
        /// <returns>Status code as HTTP string</returns>
        private string GetStatusCode(StatusCode statusCode)
        {
            string code;

            switch (statusCode)
            {
                case StatusCode.OK: code = "200 OK"; break;
                case StatusCode.BadRequest: code = "400 Bad Request"; break;
                case StatusCode.Forbiden: code = "403 Forbidden"; break;
                case StatusCode.NotFound: code = "404 Not Found"; break;
                case StatusCode.InternalServerError: code = "500 Server error"; break;
                default: code = "202 Accepted"; break;
            }

            return code;
        }
    }
}