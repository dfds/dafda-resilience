using Dafda.Consuming;
using Polly;

namespace Dafda.Resilience.Strategies;

/// <summary>
/// A resilience strategy that continues execution after handling errors, allowing message processing to proceed even when exceptions occur.
/// This strategy evaluates whether an exception should be handled based on the configured predicate, and invokes an error handler with both the exception and message context before suppressing the error.
/// </summary>
internal sealed class ContinueOnErrorStrategy : ResilienceStrategy
{
    private readonly Func<Exception, MessageExecutionContext, ValueTask> _onError;
    private readonly Func<Exception, ValueTask<bool>> _shouldHandle;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ContinueOnErrorStrategy"/> class with the specified options.
    /// </summary>
    /// <param name="options">The options that configure the error handling behavior, including which exceptions to handle and what action to take when an error occurs.</param>
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
        var outcome = await callback(context, state).ConfigureAwait(context.ContinueOnCapturedContext);

        if (outcome.Exception is null ||
            !await _shouldHandle(outcome.Exception).ConfigureAwait(context.ContinueOnCapturedContext))
        {
            return outcome;
        }

        try
        {
            var messageContext = context.Properties.GetValue(ResilienceKeys.MessageExecutionContextKey, new(null, null, null));    
            await _onError.Invoke(outcome.Exception, messageContext).ConfigureAwait(context.ContinueOnCapturedContext);
        }
        catch (Exception ex)
        {
            return Outcome.FromException<TResult>(ex);
        }

        return Outcome.FromResult<TResult>(default);
    }
}
