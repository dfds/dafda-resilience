using Dafda.Consuming;
using Polly;

namespace Dafda.Resilience;

internal static class ResilienceKeys
{
    internal static readonly ResiliencePropertyKey<MessageExecutionContext> MessageExecutionContextKey = new("MessageExecutionContextKey");
}