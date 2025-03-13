namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using NServiceBus;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Serialization;
using NServiceBus.Settings;

public class MessageHelper
{
    static SystemJsonSerializer serializer = new SystemJsonSerializer();
    static IMessageSerializer messageSerializer = serializer.Configure(new SettingsHolder())(new MessageMapper());

    public static BinaryData GetBody(object message)
    {
        using var stream = new MemoryStream();
        messageSerializer.Serialize(message, stream);
        return new BinaryData(stream.ToArray());
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