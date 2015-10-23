using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Threading;

namespace CompactWebServer
{
    /// <summary>
    /// Class implements web request to the web server
    /// </summary>
    public class HttpRequest
    {
        public string Port
        {
            get;
            set;
        }


        public string ClientIP
        {
            get;
            set;
        }

        /// <summary>
        /// Query string
        /// </summary>
        public string QueryString
        {
            get;
            set;
        }

        /// <summary>
        /// HttpMethod
        /// </summary>
        public HttpMethod Method
        {
            get;
            set;
        }

        /// <summary>
        /// Request url
        /// </summary>
        public string Url
        {
            get;
            set;
        }

        /// <summary>
        /// Protocol
        /// </summary>
        public HttpProtocol Protocol
        {
            get;
            set;
        }

        /// <summary>
        /// HostName
        /// </summary>
        public string Host
        {
            get;
            set;
        }

        /// <summary>
        /// Path
        /// </summary>
        public string Path
        {
            get;
            set;
        }

        public Dictionary<string, string> Header
        {
            get;
            set;
        }

        public byte[] Body
        {
            get;
            set;
        }

        public Dictionary<string, string> FormData
        {
            get;
            set;
        }

        public string RawBody
        {
            get;
            set;
        }

        public Dictionary<string, string> QueryParameters
        {
            get;
            set;
        }

        public string ContentType
        {
            get
            {
                return this.Header.ContainsKey("Content-Type") ? this.Header["Content-Type"].ToLower() : "";
            }
        }

        public WebServer Server
        {
            get;
            private set;
        }

        public HttpRequest()
        {
        }

        public HttpRequest(WebServer server, TcpClient tcpClient)
        {
            this.Server = server;
            ParseRequest(tcpClient);
            this.ClientIP = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
            this.Port = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port.ToString();
        }

        public T ParseJSON<T>()
        {
            if (ContentType != "application/json" || this.Body == null)
                return default(T);

            T result = default(T);

            try { result = JsonConvert.DeserializeObject<T>(this.RawBody.Trim(new char[] { '\0' })); }
            catch { };

            return result;
        }

        public object ParseJSON(Type type)
        {
            if (ContentType != "application/json" || this.Body == null)
                return null;

            object result = null;

            try { result = JsonConvert.DeserializeObject(this.RawBody.Trim(new char[] { '\0' }), type); }
            catch { };

            return result;
        }

        /// <summary>
        /// Parse request string into WebRequest
        /// </summary>
        /// <param name="request">Request string</param>
        /// <returns>WebRequest</returns>
        private void ParseRequest(TcpClient tcpClient)
        {
            var buffer = new byte[tcpClient.ReceiveBufferSize];
            int byteRead = 0;
 
            NetworkStream ns = tcpClient.GetStream();

            string method = "";
            string url = "";
            string version = "";
            var finishReadHeader = false;
            var finishReading = false;
            int bodyIndex = 0;
            int contentLength = 0;
            this.Header = new Dictionary<string, string>();
            int timeout = 30;

            // avoid split over and over a big string
            do
            {
                // data is not always available on TCP stream when we call Read, 
                // especially for HttpRequest POST when header and body come as separated chunks
                // sometimes we have to wait a little while before data arrives
                if (ns.DataAvailable)
                    byteRead = ns.Read(buffer, 0, buffer.Length);            
                else
                {
                    Thread.Sleep(300);
                    timeout--;
                    continue;
                }

                int index = 0;
                string temp = "";

                while (index < byteRead)
                {
                    if (string.IsNullOrEmpty(method))
                    {
                        if (buffer[index] != ' ')
                            temp += (char)buffer[index];
                        else
                        {
                            method = temp;
                            temp = "";
                        }

                        index++;
                        continue;
                    }

                    if (string.IsNullOrEmpty(url))
                    {
                        if (buffer[index] != ' ')
                            temp += (char)buffer[index];
                        else
                        {
                            url = URLDecode(temp);
                            temp = "";
                        }

                        index++;
                        continue;
                    }

                    if (string.IsNullOrEmpty(version))
                    {
                        if (buffer[index] != '\r' && buffer[index] != '\n')
                            temp += (char)buffer[index];
                        else if (buffer[index] == '\n')
                        {
                            version = temp;
                            temp = "";
                        }

                        index++;
                        continue;
                    }

                    // read header
                    if (!finishReadHeader)
                    {
                        if (buffer[index] != '\r' && buffer[index] != '\n')
                            temp += (char)buffer[index];
                        else if (buffer[index] == '\n')
                        {
                            finishReadHeader = string.IsNullOrEmpty(temp);

                            if (!finishReadHeader)
                            {
                                this.ParseHeader(temp);
                                temp = "";
                            }
                            else if (this.Header.ContainsKey("Content-Length"))
                            {
                                contentLength = Convert.ToInt32(this.Header["Content-Length"]);
                                this.Body = new byte[contentLength];
                            }

                            temp = "";
                        }

                        index++;
                        if (!finishReadHeader)
                            continue;   
                    }

                    // if it makes this far, which meanis all header is read
                    if (this.Body != null)
                    {
                        Array.Copy(buffer, index, this.Body, bodyIndex, Math.Min(byteRead - index, contentLength - bodyIndex));
                        bodyIndex += byteRead - index;
                        finishReading = bodyIndex >= contentLength;
                    }
                    else // no body e.g. GET request
                        finishReading = true;

                    break;
                }
            } while (!finishReading && timeout > 0);

            this.Method = ParseMethod(method);
            this.Url = url;
            this.Path = ParsePath(url);
            this.QueryString = ParseQueryString(url);
            if (!string.IsNullOrEmpty(this.QueryString))
                this.QueryParameters = ParseQueryParameters(this.QueryString);
            this.Protocol = ParseProtocol(version);

            if (this.Body != null)
            {
                this.RawBody = Encoding.UTF8.GetString(this.Body, 0, this.Body.Length);
                switch (this.ContentType)
                {
                    case "application/x-www-form-urlencoded":
                        this.FormData = new Dictionary<string, string>();
                        ParseBodyUrlencoded(this.RawBody);
                        break;
                }
            }
        }

        private Dictionary<string, string> ParseBodyUrlencoded(string line)
        {
            Dictionary<string, string> body = new Dictionary<string, string>();
            string[] queries = line.Split('&');
            foreach (var query in queries)
            {
                var temp = query.Split('=');
                body.Add(temp[0], temp[1]);
            }
            return body;
        }

        private void ParseHeader(string line)
        {
            string[] temp = line.Split(':');
            string key = temp[0];
            string value = temp[1].TrimStart();
            this.Header.Add(key, value);
        }

        private string ParsePath(string path)
        {
            int index = path.IndexOf('?');
            if (index != -1)
                path = path.Substring(0, index);
            return path;
        }

        private string ParseQueryString(string path)
        {
            int index = path.IndexOf('?');
            string queryString = "";
            if (index != -1)
                queryString = path.Remove(0, index + 1);
            return queryString;
        }

        private Dictionary<string, string> ParseQueryParameters(string queryString)
        {
            Dictionary<string, string> queryCollection = new Dictionary<string, string>();
            string[] queries = queryString.Split('&');
            foreach(var query in queries)
            {
                var temp = query.Split('=');
                queryCollection.Add(temp[0], URLDecode(temp[1]));
            }
            return queryCollection;
        }

        /// <summary>
        /// Parse and get the protocol in request's first line
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>HttpMethod if protocol exists</returns>
        private HttpProtocol ParseProtocol(string protocol)
        {
            switch (protocol)
            {
                case "HTTP/1.1":
                    return HttpProtocol.HTTP;
                default:
                    return HttpProtocol.HTTP;
            }
        }

        /// <summary>
        /// Parse and get the method in request's first line
        /// </summary>
        /// <param name="request">string method</param>
        /// <returns>HttpMethod if method exists</returns>
        private HttpMethod ParseMethod(string method)
        {
            switch (method)
            {
                case "OPTIONS":
                    return HttpMethod.OPTIONS;
                case "GET":
                    return HttpMethod.GET;
                case "HEAD":
                    return HttpMethod.HEAD;
                case "POST":
                    return HttpMethod.POST;
                case "PUT":
                    return HttpMethod.PUT;
                case "DELETE":
                    return HttpMethod.DELETE;
                case "TRACE":
                    return HttpMethod.TRACE;
                case "CONNECT":
                    return HttpMethod.CONNECT;
                default:
                    return HttpMethod.GET;
            }
        }

        /// <summary>
        /// Decodes the URL query string into string
        /// </summary>
        /// <param name="encodedString">Encoded QueryString</param>
        /// <returns>Plain string</returns>
        private string URLDecode(string encodedString)
        {
            string outStr = string.Empty;

            int i = 0;
            while (i < encodedString.Length)
            {
                switch (encodedString[i])
                {
                    case '+': outStr += " "; break;
                    case '%':
                        string tempStr = encodedString.Substring(i + 1, 2);
                        outStr += Convert.ToChar(int.Parse(tempStr, System.Globalization.NumberStyles.AllowHexSpecifier));
                        i = i + 2;
                        break;
                    default:
                        outStr += encodedString[i];
                        break;
                }
                i++;
            }
            return outStr;
        }
    }
}
