using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.TransactionalSession;

public class TransactionalSessionMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // brutally hardcoded for now
        if (context.FunctionDefinition.Name != "HttpSenderV4Transactional")
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        // this is a little weird, but it works
        var transactionalSession = context.InstanceServices.GetRequiredKeyedService<ITransactionalSession>("SenderEndpoint");
        await using var _ = transactionalSession.ConfigureAwait(false);
        await transactionalSession.Open(new MongoOpenSessionOptions(), context.CancellationToken).ConfigureAwait(false);

        await next(context).ConfigureAwait(false);

        await transactionalSession.Commit(context.CancellationToken).ConfigureAwait(false);
    }
}