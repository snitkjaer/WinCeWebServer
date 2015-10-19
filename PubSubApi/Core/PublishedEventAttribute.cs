using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace PubSubApi
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class PublishedEventAttribute: Attribute
    {
        public string Name {get;set;}

        public PublishedEventAttribute(string name)
        {
            Name = name;
        }
    }
}
