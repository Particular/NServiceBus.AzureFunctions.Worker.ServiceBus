using Microsoft.Azure.Functions.Worker;

namespace test
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus;

    public class Function1
    {
        IFunctionEndpoint endpoint;

        public Function1(IFunctionEndpoint endpoint)
        {
            this.endpoint = endpoint;
        }


        [Function("Function1")]
        public async Task Run(
            [ServiceBusTrigger("ASBTriggerQueue")] byte[] messageBody,
            IDictionary<string, string> userProperties,
            FunctionContext context)
        {
            await endpoint.Process(messageBody, userProperties, context);
        }
    }
}
