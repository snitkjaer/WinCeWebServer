using CompactWebServer;

[System.AttributeUsage(System.AttributeTargets.Method)]

public class RouteAttribute : System.Attribute
{
    private HttpMethod _method;
    private string _path;

    public string Path
    {
        get { return _path; }
        set { _path = value; }
    }

    public HttpMethod Method
    {
        get { return _method; }
        set { _method = value; }
    }

    public RouteAttribute(HttpMethod method, string path)
    {
        this.Method = method;
        this.Path = path;
    }
}
