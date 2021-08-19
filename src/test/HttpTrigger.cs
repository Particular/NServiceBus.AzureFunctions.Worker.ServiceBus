namespace test
{
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;
    using Microsoft.Extensions.Logging;
    using NServiceBus;

    public class HttpTrigger
    {
        readonly IFunctionEndpoint functionEndpoint;

        public HttpTrigger(IFunctionEndpoint functionEndpoint)
        {
            this.functionEndpoint = functionEndpoint;
        }

        [Function("HttpSender")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req, 
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger<HttpTrigger>();
            logger.LogInformation("C# HTTP trigger function received a request.");

            var sendOptions = new SendOptions();
            sendOptions.RouteToThisEndpoint();

            await functionEndpoint.Send(new TriggerMessage(), sendOptions, executionContext);

            var r = req.CreateResponse(HttpStatusCode.OK);
            r.WriteString("Message sent");
            return r;
        }
    }
}