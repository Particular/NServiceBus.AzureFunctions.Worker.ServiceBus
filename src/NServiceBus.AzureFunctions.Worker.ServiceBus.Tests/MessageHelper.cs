namespace ServiceBus.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;

    public class MessageHelper
    {
        static NewtonsoftSerializer serializer = new NewtonsoftSerializer();
        static IMessageSerializer messageSerializer = serializer.Configure(new SettingsHolder())(new MessageMapper());

        public static byte[] GetBody(object message)
        {
            using (var stream = new MemoryStream())
            {
                messageSerializer.Serialize(message, stream);
                return stream.ToArray();
            }
        }

        public static IDictionary<string, string> GetUserProperties(object message)
        {
            var dictionary = new Dictionary<string, string>();

            dictionary.Add(Headers.EnclosedMessageTypes, message.GetType().FullName);

            return dictionary;
        }
    }
}