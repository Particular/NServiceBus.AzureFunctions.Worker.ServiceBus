namespace NServiceBus
{
    using Microsoft.Azure.Functions.Worker;

    /// <summary>
    /// Contains specific context information of the current function invocation.
    /// </summary>
    public class FunctionExecutionContext
    {
        /// <summary>
        /// Creates a new <see cref="FunctionExecutionContext"/>.
        /// </summary>
        public FunctionExecutionContext(FunctionContext executionContext)
        {
            ExecutionContext = executionContext;
        }

        /// <summary>
        /// The <see cref="ExecutionContext"/> associated with the current function invocation.
        /// </summary>
        public FunctionContext ExecutionContext { get; }
    }
}