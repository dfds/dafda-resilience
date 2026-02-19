using Dafda.Resilience.Strategies;
using Polly;

namespace Dafda.Resilience;

/// <summary>
/// Extension methods for <see cref="ResiliencePipelineBuilder"/> that add custom resilience strategies.
/// </summary>
public static class PollyResiliencePipelineBuilderExtensions
{
    /// <summary>
    /// Adds a strategy that continues execution on handled errors using default options.
    /// </summary>
    /// <param name="builder">The resilience pipeline builder.</param>
    /// <returns>The same instance of the <see cref="ResiliencePipelineBuilder"/> for chaining.</returns>
    /// <remarks>
    /// The order of chaining when building the resilience pipeline is important. This strategy swallows exceptions,
    /// so if added after a retry strategy, it will prevent retries from executing. Consider adding this strategy
    /// before retry and circuitbreaker strategies in the pipeline.
    /// </remarks>
    public static ResiliencePipelineBuilder ContinueOnError(this ResiliencePipelineBuilder builder)
    {
        var options = new ContinueOnErrorStrategyOptions();
        return builder.AddContinueOnError(options);
    }

    /// <summary>
    /// Adds a strategy that continues execution on handled errors using the specified options.
    /// </summary>
    /// <param name="builder">The resilience pipeline builder.</param>
    /// <param name="options">The options that control the behavior of the continue-on-error strategy.</param>
    /// <returns>The same instance of the <see cref="ResiliencePipelineBuilder"/> for chaining.</returns>
    /// <remarks>
    /// The order of chaining when building the resilience pipeline is important. This strategy swallows exceptions,
    /// so if added after a retry strategy, it will prevent retries from executing. Consider adding this strategy
    /// before retry and circuitbreaker strategies in the pipeline.
    /// </remarks>
    public static ResiliencePipelineBuilder AddContinueOnError(this ResiliencePipelineBuilder builder, ContinueOnErrorStrategyOptions options)
    {
        builder.AddStrategy(context => new ContinueOnErrorStrategy(options), options);

        return builder;
    }
}
