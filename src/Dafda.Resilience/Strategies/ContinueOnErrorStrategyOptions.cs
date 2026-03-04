using Dafda.Consuming;
using Polly;

namespace Dafda.Resilience.Strategies;

/// <summary>
/// Configuration options for the <see cref="ContinueOnErrorStrategy"/>.
/// Defines which exceptions should be handled and what action to take when an error is suppressed.
/// </summary>
public sealed class ContinueOnErrorStrategyOptions : ResilienceStrategyOptions
{
    /// <summary>
    /// Gets or sets the delegate that is invoked when an exception is handled and suppressed.
    /// The delegate receives both the exception that occurred and the message execution context.
    /// </summary>
    /// <value>
    /// The default value is a delegate that does nothing.
    /// </value>
    public Func<Exception, MessageExecutionContext, ValueTask> OnError = (ex, context) => ValueTask.CompletedTask;

    /// <summary>
    /// Gets or sets a predicate that determines whether an exception should be handled and suppressed.
    /// </summary>
    /// <value>
    /// The default value is a predicate that handles any exception except <see cref="OperationCanceledException"/>.
    /// </value>
    public Func<Exception, ValueTask<bool>> ShouldHandle = ex => ValueTask.FromResult(ex is not OperationCanceledException);

    /// <summary>
    /// Initializes a new instance of the <see cref="ContinueOnErrorStrategyOptions"/> class.
    /// </summary>
    public ContinueOnErrorStrategyOptions()
    {
        Name = "ContinueOnError";
    }
}
