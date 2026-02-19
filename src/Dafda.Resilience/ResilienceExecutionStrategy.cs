using Dafda.Consuming;
using Polly;
using Polly.Registry;

namespace Dafda.Resilience;

/// <summary>
/// An implementation of <see cref="IMessageHandlerExecutionStrategy"/> that executes the Message Handler within a Polly resilience pipeline.
/// </summary>
/// <param name="pipelineName">The unique identifier of the resilience pipeline to use for executing actions.</param>
/// <param name="pipelineProvider">The provider used to retrieve the configured resilience pipeline.</param>
public sealed class ResilienceExecutionStrategy(
    string pipelineName,
    ResiliencePipelineProvider<string> pipelineProvider) : IMessageHandlerExecutionStrategy
{
    /// <summary>
    /// Executes the specified action within the configured resilience pipeline.
    /// </summary>
    /// <param name="action">The consumer action to execute with resilience strategies applied.</param>
    /// <returns>A task that represents the asynchronous execution of the action within the resilience pipeline.</returns>
    public async Task Execute(Func<CancellationToken, Task> action, MessageExecutionContext context, CancellationToken cancellationToken)
    {
        var resilienceContext = ResilienceContextPool.Shared.Get(new ResilienceContextCreationArguments(null, false, cancellationToken));

        resilienceContext.Properties.Set(ResilienceKeys.MessageExecutionContextKey, context);

        var pipeline = pipelineProvider.GetPipeline(pipelineName);
        await pipeline.ExecuteAsync(async ct => await action(ct), resilienceContext.CancellationToken);

        ResilienceContextPool.Shared.Return(resilienceContext);
    }
}
