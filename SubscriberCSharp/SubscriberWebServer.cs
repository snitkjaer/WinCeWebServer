using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using CompactWebServer;
using PubSubApi;

namespace SubscriberCSharp
{
    public class SubscriberWebServer : WebServer
    {
        public SubscriberWebServer(WebServerConfiguration config)
            : base(config)
        {
        }

        public event NotifyPosition OnNotifyPosition;

        public delegate void NotifyPosition(PublishedResource<PositionEvent> data);

        /// <summary>
        /// Raise event when something "loggable" happend
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="message">Message</param>
        public void RaisePositionEvent(PublishedResource<PositionEvent> data)
        {
            if (OnNotifyPosition != null)
            {
                OnNotifyPosition(data);
            }
        }
    }
}
