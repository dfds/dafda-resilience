using Dafda.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Registry;

namespace Dafda.Resilience;

/// <summary>
/// Provides extension methods for configuring resilience capabilities on Dafda consumer options.
/// </summary>
public static class ConsumerOptionExtensions
{
    /// <summary>
    /// Configures the Dafda Consumer to use an existing named resilience pipeline.
    /// The pipeline must be registered in the service collection using AddResiliencePipeline.
    /// </summary>
    /// <param name="options">The consumer options to configure.</param>
    /// <param name="pipelineName">The name of the resilience pipeline to use.</param>
    /// <returns>The same instance of <see cref="ConsumerOptions"/> for method chaining.</returns>
    public static ConsumerOptions WithResiliencePipeline(this ConsumerOptions options, string pipelineName)
    {
        options.WithMessageHandlerExecutionStrategyFactory(sp =>
        {
            return new ResilienceExecutionStrategy(
                pipelineName,
                sp.GetRequiredService<ResiliencePipelineProvider<string>>());
        });

        return options;
    }
}