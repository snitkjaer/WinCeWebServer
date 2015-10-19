using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace PubSubApi
{
    public class EventFactory
    {
        public static Type GetPublishedEventType(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var assembly = typeof(PublishedEvent).Assembly;

            return assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(PublishedEvent)) && 
                    t.GetCustomAttributes(typeof(PublishedEventAttribute), false)
                    .Any(c => ((PublishedEventAttribute)c).Name == name))
                .FirstOrDefault();
        }

        public static PublishedEvent CreatePublishedEvent(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            var type = GetPublishedEventType(name);
            if (type == null)
                return null;

            return (PublishedEvent)type.GetConstructor(new Type[]{ typeof(string) }).Invoke(new object[]{name});
        }

        public static PublishedResource CreatePublishedResource(string name, string clientId, PublishedEvent eventData)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            
            var eventType = GetPublishedEventType(name);
            var assembly = typeof(PublishedResource).Assembly;
            var resourceType = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(PublishedResource)) && 
                    t.GetCustomAttributes(typeof(PublishedEventAttribute), false)
                    .Any(c => ((PublishedEventAttribute)c).Name == name))
                .FirstOrDefault();

            var resource = (PublishedResource)resourceType.GetConstructor(new Type[] {eventType}).Invoke(new object[] {eventData});
            if (resource == null)
                return null;

            resource.Id = Guid.NewGuid().ToString();
            resource.ClientId = clientId;
            resource.EventName = name;

            return resource;
        }
    }
}
