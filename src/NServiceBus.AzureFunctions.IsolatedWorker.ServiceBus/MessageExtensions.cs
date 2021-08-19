namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Functions.Worker;

    static class MessageExtensions
    {
        public static Dictionary<string, string> GetHeaders(this BindingContext bindingContext)
        {
            var userProperties = (bindingContext.BindingData["UserProperties"] as IDictionary<string, object>) ?? new Dictionary<string, object>(0);
            var headers = new Dictionary<string, string>();

            foreach (var kvp in userProperties)
            {
                headers[kvp.Key] = kvp.Value?.ToString();
            }

            headers.Remove("NServiceBus.Transport.Encoding");

            if(bindingContext.BindingData.TryGetValue("ReplyTo", out var replyTo)
               && !string.IsNullOrWhiteSpace(replyTo as string))
            {
                headers[Headers.ReplyToAddress] = (string)replyTo;
            }

            if (bindingContext.BindingData.TryGetValue("CorrelationId", out var correlationId)
                && !string.IsNullOrWhiteSpace(correlationId as string))
            {
                headers[Headers.CorrelationId] = (string)correlationId;
            }

            return headers;
        }

        public static string GetMessageId(this BindingContext bindingContext)
        {
            string messageId;
            if (string.IsNullOrEmpty(messageId = bindingContext.BindingData["MessageId"] as string))
            {
                // assume native message w/o message ID
                return Guid.NewGuid().ToString("N");
            }

            return messageId;
        }
    }
}