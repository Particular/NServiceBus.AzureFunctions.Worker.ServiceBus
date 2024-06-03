namespace NServiceBus.AzureFunctions.Worker.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml;
    using Azure.Messaging.ServiceBus;

    static class MessageExtensions
    {
        public static Dictionary<string, string> GetNServiceBusHeaders(this ServiceBusReceivedMessage message)
        {
            var headers = new Dictionary<string, string>(message.ApplicationProperties.Count);

            foreach (var kvp in message.ApplicationProperties)
            {
                headers[kvp.Key] = kvp.Value?.ToString();
            }

            headers.Remove(TransportMessageHeaders.TransportEncoding);

            if (!string.IsNullOrWhiteSpace(message.ReplyTo))
            {
                headers[Headers.ReplyToAddress] = message.ReplyTo;
            }

            if (!string.IsNullOrWhiteSpace(message.CorrelationId))
            {
                headers[Headers.CorrelationId] = message.CorrelationId;
            }

            return headers;
        }

        public static string GetMessageId(this ServiceBusReceivedMessage message) => message.MessageId ?? Guid.NewGuid().ToString("N");

        public static BinaryData GetBody(this ServiceBusReceivedMessage message)
        {
            var body = message.Body ?? BinaryData.FromBytes(ReadOnlyMemory<byte>.Empty);
            var memory = body.ToMemory();

            if (memory.IsEmpty ||
                !message.ApplicationProperties.TryGetValue(TransportMessageHeaders.TransportEncoding, out var value) ||
                !value.Equals("wcf/byte-array"))
            {
                return body;
            }

            using var reader = XmlDictionaryReader.CreateBinaryReader(body.ToStream(), XmlDictionaryReaderQuotas.Max);
            var bodyBytes = (byte[])Deserializer.ReadObject(reader);
            return new BinaryData(bodyBytes);
        }

        static readonly DataContractSerializer Deserializer = new(typeof(byte[]));
    }
}