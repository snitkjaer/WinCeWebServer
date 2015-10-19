using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace PubSubApi
{
    [PublishedEvent("position")]
    public class PositionEvent : PublishedEvent
    {
        public PositionEvent()
            : base("position")
        {
        }

        public string Long { get; set; }
        public string Lat { get; set; }
    }

    [PublishedEvent("position")]
    public class PositionResource : PublishedResource<PositionEvent>
    {
        public PositionResource(PositionEvent data)
        {
            EventData = data;
        }
    }
}