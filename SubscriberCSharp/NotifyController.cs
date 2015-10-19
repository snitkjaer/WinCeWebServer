using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using CompactWebServer;
using PubSubApi;

namespace SubscriberCSharp
{
    public class NotifyController: BaseController
    {
        [Route(HttpMethod.POST, "/notify")]
        public void Notify(HttpRequest req, HttpResponse res)
        {
            var server = (SubscriberWebServer)req.Server;

            server.RaiseLogEvent("info", string.Format("received: {0}", req.RawBody));
            server.RaisePositionEvent(req.ParseJSON<PublishedResource<PositionEvent>>());

            res.SendJson(new { success = true });
        }
    }
}
