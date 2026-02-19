using Dafda.Consuming;
using Polly;

namespace Dafda.Resilience;

public static class ResilienceKeys
{
    public static readonly ResiliencePropertyKey<MessageExecutionContext> MessageExecutionContextKey = new("MessageExecutionContextKey");
}