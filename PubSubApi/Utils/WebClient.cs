using System;
using System.Text;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace PubSubApi
{
    public static class WebClient
    {
        public delegate void OnAsyncResponse (string result);

        public static void PostJsonAsync(string url, Object data, OnAsyncResponse onResponse)
        {
            string json = JsonConvert.SerializeObject(data);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentLength = bytes.Length;
            request.ContentType = "application/json";

            // start the asynchronous operation
            request.BeginGetRequestStream(new AsyncCallback((result) =>
            {
                HttpWebRequest req = (HttpWebRequest)result.AsyncState;

                Stream postStream = req.EndGetRequestStream(result);
                postStream.Write(bytes, 0, bytes.Length);
                postStream.Close();

                req.BeginGetResponse(new AsyncCallback(responseResult =>
                {
                    HttpWebRequest req1 = (HttpWebRequest)responseResult.AsyncState;

                    if (responseResult.IsCompleted)
                    {
                        var webResponse = req1.EndGetResponse(responseResult);
                        using (var stream = webResponse.GetResponseStream())
                        {
                            using (var read = new StreamReader(stream))
                            {
                                if (onResponse != null)
                                    onResponse.Invoke(read.ReadToEnd());
                            }
                        }
                    }
                }), req);
            }), request);
        }

        public static WebResponse PostJson(string url, Object data)
        {
            string json = JsonConvert.SerializeObject(data);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentLength = bytes.Length;
            request.ContentType = "application/json";

            // start the asynchronous operation
            using (var stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }

            return request.GetResponse();
        }

        public static WebResponse GetJson(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/json";

            return request.GetResponse();
        }
    }
}
