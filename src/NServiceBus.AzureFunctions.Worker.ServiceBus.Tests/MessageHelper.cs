namespace ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using Azure.Messaging.ServiceBus;
    using NServiceBus;

    public class MessageHelper
    {
        public static ServiceBusReceivedMessage CreateServiceBusReceivedMessage(object message)
        {
            return ServiceBusModelFactory.ServiceBusReceivedMessage(
                        body: GetBody(message),
                        messageId: Guid.NewGuid().ToString("N"),
                        properties: GetUserProperties(message),
                        deliveryCount: 1);
        }
        public static BinaryData GetBody(object message)
        {
            return BinaryData.FromObjectAsJson(message);
        }

        public static IDictionary<string, object> GetUserProperties(object message)
        {
            var dictionary = new Dictionary<string, object>
            {
                { Headers.EnclosedMessageTypes, message.GetType().FullName }
            };

            return dictionary;
        }
    }
}