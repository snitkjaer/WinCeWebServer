namespace CompactWebServer
{
    /// <summary>
    /// HTTPS Method
    /// http://www.w3.org/Protocols/rfc2616/rfc2616-sec9.html#sec9.1
    /// </summary>
    public enum HttpMethod
    {
        OPTIONS,
        GET,
        HEAD,
        POST,
        PUT,
        DELETE,
        TRACE,
        CONNECT,

        // support for Routing only
        ANY
    };
}
