using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace PubSubApi
{
    public class Subscription
    {
        public string SubscribedEvent { get; set; }

        public string ClientId { get; set; }

        public string Endpoint { get; set; }
    }
}