using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using CompactWebServer;
using System.Threading;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using PubSubApi;

namespace BrokerCSharp
{
    public class PubSubController : BaseController
    {
        static List<Subscription> _subscriptions = new List<Subscription>();
        static List<PublishedResource> _resources = new List<PublishedResource>();

        [Route(HttpMethod.GET, "/ping")]
        public void Ping(HttpRequest req, HttpResponse res)
        {
            req.Server.RaiseLogEvent("info", string.Format("ping success"));
            res.SendText("OK");
        }

        [Route(HttpMethod.POST, "/subscribe")]
        public void Subscribe(HttpRequest req, HttpResponse res)
        {
            var data = req.ParseJSON<Subscription>();
            if (data == null || string.IsNullOrEmpty(data.ClientId) || string.IsNullOrEmpty(data.SubscribedEvent))
            {
                res.SendError(StatusCode.BadRequest, "Invalid data");
                return;
            }

            if (!_subscriptions.Any(a => a.ClientId == data.ClientId && a.SubscribedEvent == a.SubscribedEvent))
            {
                _subscriptions.Add(data);
            }

            req.Server.RaiseLogEvent("info", string.Format("subsriber registered: {0}", req.RawBody));
            
            res.SendJson(new { success = true });
        }

        [Route(HttpMethod.POST, "/unsubscribe")]
        public void Unsubscribe(HttpRequest req, HttpResponse res)
        {
            var data = req.ParseJSON<Subscription>();
            if (data == null || string.IsNullOrEmpty(data.ClientId) || string.IsNullOrEmpty(data.SubscribedEvent))
            {
                res.SendError(StatusCode.BadRequest, "Invalid data");
                return;
            }

            _subscriptions.RemoveAll(s => s.ClientId == data.ClientId && s.SubscribedEvent == data.SubscribedEvent);

            req.Server.RaiseLogEvent("info", string.Format("subsriber removed: {0}", req.RawBody));

            res.SendJson(new { success = true });
        }

        [Route(HttpMethod.POST, "/publish")]
        public void Publish(HttpRequest req, HttpResponse res)
        {
            var evt = req.ParseJSON<PublishedEvent>();
            var eventType = EventFactory.GetPublishedEventType(evt.Name);
            var evtData = req.ParseJSON(eventType) as PublishedEvent;

            if (evtData == null || eventType == null || evtData == null)
            {
                res.SendError(StatusCode.BadRequest, "Invalid data");
                return;
            }

            var subs = _subscriptions.Where(sub => sub.SubscribedEvent == evt.Name).ToList();
            req.Server.RaiseLogEvent("info", string.Format("event published: {0}", req.RawBody));

            // loop for all subscribers have registered for push notification
            foreach (var sub in subs)
            {
                var resource = EventFactory.CreatePublishedResource(evt.Name, sub.ClientId, evtData);

                if (!string.IsNullOrEmpty(sub.Endpoint))
                {
                    try
                    {
                        WebClient.PostJsonAsync(sub.Endpoint, resource, (responseText) =>
                        {
                            req.Server.RaiseLogEvent("info", string.Format("forwarded to subscriber {0}", sub.ClientId));
                        });
                    }
                    catch { }
                }
                else
                    _resources.Add(resource);
            }

            res.SendJson(new { success = true });
        }

        [Route(HttpMethod.POST, "/pull")]
        public void Pull(HttpRequest req, HttpResponse res)
        {
            var data = req.ParseJSON<Subscription>();
            if (data == null || string.IsNullOrEmpty(data.ClientId) || string.IsNullOrEmpty(data.SubscribedEvent))
            {
                res.SendError(StatusCode.BadRequest, "Invalid data");
                return;
            }

            var pulled = _resources.Where(r => r.ClientId == data.ClientId && r.EventName == data.SubscribedEvent);
            _resources.RemoveAll(r => r.ClientId == data.ClientId && r.EventName == data.SubscribedEvent);

            req.Server.RaiseLogEvent("info", string.Format("client {0} pulling...", data.ClientId));

            res.SendJson(pulled);
        }

        // no need for this api in demo
        // temporarily removed resource instantly after pushed/pulled,
        // later the client may have to acknowledge their receiving
        // before server will remove the resource completely
        [Route(HttpMethod.POST, "/acknowledge-receive")]
        public void Acknowledge(HttpRequest req, HttpResponse res)
        {
            var data = req.QueryParameters;
            if (data.ContainsKey("id"))
            {
                res.SendError(StatusCode.BadRequest, "Invalid data");
                return;
            }

            var ids = data["id"].Split(',');

            _resources.RemoveAll(a => ids.Contains(a.Id));

            res.SendJson(new { success = true });
        }
    }
}
