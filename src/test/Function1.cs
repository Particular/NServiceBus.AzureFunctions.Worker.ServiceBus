//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Microsoft.Azure.Functions.Worker;
//using NServiceBus;

//public class Function1
//{
//    IFunctionEndpoint endpoint;

//    public Function1(IFunctionEndpoint endpoint)
//    {
//        this.endpoint = endpoint;
//    }


//    [Function("Function1")]
//    public async Task Run(
//        [ServiceBusTrigger("ASBTriggerQueue")] byte[] messageBody,
//        IDictionary<string, string> userProperties,
//        string messageId,
//        int deliveryCount,
//        string replyTo,
//        string correlationId,
//        FunctionContext context)
//    {
//        await endpoint.Process(messageBody, userProperties, messageId, deliveryCount, replyTo, correlationId, context);
//    }
//}