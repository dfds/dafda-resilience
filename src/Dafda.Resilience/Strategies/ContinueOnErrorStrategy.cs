using Dafda.Consuming;
using Polly;

namespace Dafda.Resilience.Strategies;

/// <summary>
/// A resilience strategy that continues execution on handled errors.
/// </summary>
internal sealed class ContinueOnErrorStrategy : ResilienceStrategy
{
    private readonly Action<Exception, MessageExecutionContext> _onError;
    private readonly Predicate<Exception> _shouldHandle;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ContinueOnErrorStrategy"/> class.
    /// </summary>
    /// <param name="options">The options that configure the behavior of the strategy.</param>
    internal ContinueOnErrorStrategy(ContinueOnErrorStrategyOptions options)
    {
        _onError = options.OnError;
        _shouldHandle = options.ShouldHandle;
    }

    /// <summary>
    /// Executes the core logic of the resilience strategy, handling exceptions based on the configured options.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the callback.</typeparam>
    /// <typeparam name="TState">The type of state passed to the callback.</typeparam>
    /// <param name="callback">The callback to execute.</param>
    /// <param name="context">The resilience context.</param>
    /// <param name="state">The state object passed to the callback.</param>
    /// <returns>An outcome containing either the result or a default value if an exception was handled.</returns>
    protected override async ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
        Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback, ResilienceContext context, TState state)
    {
        var outcome = await callback(context, state);

        if (outcome.Exception is null || !_shouldHandle(outcome.Exception))
        {
            return outcome;
        }

        try
        {
            var messageContext = context.Properties.GetValue(ResilienceKeys.MessageExecutionContextKey, new(null, null, null));    
            _onError.Invoke(outcome.Exception, messageContext);
        }
        catch (Exception ex)
        {
            return Outcome.FromException<TResult>(ex);
        }

        return Outcome.FromResult<TResult>(default);
    }
}
