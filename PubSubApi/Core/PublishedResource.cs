using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace PubSubApi
{
    public class PublishedResource
    {
        public string Id { get; set; }

        public string ClientId { get; set; }

        public string EventName { get; set; }
    }

    public class PublishedResource<T> : PublishedResource where T : PublishedEvent
    {
        public T EventData { get; set; }
    }
}