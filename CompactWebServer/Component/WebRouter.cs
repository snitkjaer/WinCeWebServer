using System.Collections.Generic;
using System;
using System.Reflection;

namespace CompactWebServer
{
    public class WebRouter
    {
        private Dictionary<string, MethodInfo> _routes = new Dictionary<string, MethodInfo>();

        public Dictionary<string, MethodInfo> Routes
        {
            get { return _routes; }
            set { _routes = value; }
        }

        public void Add(HttpMethod method, string path, MethodInfo handler)
        {
            string key = generateKey(method, path);

            if (!this.Routes.ContainsKey(key))
                this.Routes.Add(key, handler);
        }

        public void Add(string path, MethodInfo handler)
        {
            this.Add(HttpMethod.ANY, path, handler);
        }

        public bool Contain(HttpMethod method, string path)
        {
            if (this.Routes.ContainsKey(generateKey(method, path)))
                return true;

            return this.Routes.ContainsKey(generateKey(HttpMethod.ANY, path));
        }

        public MethodInfo Get(HttpMethod method, string path)
        {
            string key1 = generateKey(method, path);

            if (this.Routes.ContainsKey(key1))
                return this.Routes[key1];

            string key2 = generateKey(HttpMethod.ANY, path);

            return this.Routes.ContainsKey(key2) ? this.Routes[key2] : null;
        }

        private string generateKey(HttpMethod method, string path)
        {
            return method.ToString() + path.ToLower();
        }
    }
}