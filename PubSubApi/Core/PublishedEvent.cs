using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace PubSubApi
{
    public class PublishedEvent
    {
        public PublishedEvent(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}