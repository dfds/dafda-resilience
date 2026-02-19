# Dafda.Resilience

Add resilience and fault tolerance to your Kafka consumers with [Polly](https://github.com/App-vNext/Polly)-powered strategies for [Dafda](https://github.com/dfds/dafda).

## Overview

Dafda.Resilience is a lightweight extension library that brings enterprise-grade resilience patterns to Dafda's Kafka message consumers. Built on top of Polly, it enables you to handle transient failures, prevent cascading errors, and maintain high availability without blocking your message pipeline.

## Features

It provides the existing features from Polly, such as retry, timeout, and circuit breaker, but also includes a unique "Continue on Error" strategy that allows your consumer to keep processing messages even when individual handlers fail.

- 🔄 **Retry Strategies** — Automatically recover from transient failures with configurable backoff
- ⏱️ **Timeout Control** — Prevent long-running handlers from blocking your message pipeline
- 🔌 **Circuit Breaker** — Fail fast and protect downstream services when errors exceed thresholds
- ✅ **Continue on Error** — Keep processing messages even when individual handlers fail
- 🎯 **Composable Pipelines** — Combine multiple Polly strategies for sophisticated error handling
- 🔧 **Seamless Integration** — Works naturally with Dafda's existing configuration API

## Installation

```bash
dotnet add package Dafda.Resilience
```

**Requirements:**
- .NET 8.0 or later
- Dafda 1.1.0 or later
- Polly.Core 8.6.5 or later

## Quick Start

### Basic Retry Configuration

Add retry logic to handle transient failures:

```csharp
// Register the resilience pipeline
services.AddResiliencePipeline("retry-pipeline", builder =>
{
    builder.AddRetry(new()
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1)
    });
});

// Use it in your consumer
services.AddConsumer(options =>
{
    options.WithResiliencePipeline("retry-pipeline");
});
```

### Continue on Error

Keep processing messages even when handlers fail:

```csharp
// Register the resilience pipeline
services.AddResiliencePipeline("continue-on-error", builder =>
{
    builder.AddContinueOnError(new()
    {
        OnError = (ex, context) => Console.WriteLine($"Failed: {ex.Message}"),
        ShouldHandle = ex => ex is not OperationCanceledException
    });
});

// Use it in your consumer
services.AddConsumer(options =>
{
    options.WithResiliencePipeline("continue-on-error");
});
```

### Combining Strategies

Combine retry with continue-on-error to ensure your consumer keeps processing messages:

```csharp
// Register the resilience pipeline
services.AddResiliencePipeline("robust-pipeline", builder =>
{
    builder.AddContinueOnError(new()
    {
        OnError = (ex, context) => logger.LogError(ex, "Processing failed"),
        ShouldHandle = ex => ex is not OperationCanceledException
    })
    .AddRetry(new()
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1)
    });
});

// Use it in your consumer
services.AddConsumer(options =>
{
    options.WithResiliencePipeline("robust-pipeline");
});
```

> ⚠️ **Strategy Order Matters:** Add `AddContinueOnError` **first** (outermost), then retry strategies. This ensures retries execute before the exception is swallowed.

### Reusing Pipelines Across Consumers

Register a resilience pipeline once and use it for multiple consumers:

```csharp
// Register the resilience pipeline once
services.AddResiliencePipeline("shared-pipeline", builder =>
{
    builder.AddContinueOnError(new())
    .AddRetry(new())
    .AddTimeout(new());
});

// Use the same pipeline for multiple consumers
services.AddConsumer(options =>
{
    options.WithResiliencePipeline("shared-pipeline");
});

services.AddConsumer(options =>
{
    options.WithResiliencePipeline("shared-pipeline");
});
```

## Resilience Strategies

This only provides a brief overview of the available strategies. For detailed configuration options and examples, please refer to the [Polly Documentation](https://www.pollydocs.org/strategies/index.html).

### Continue on Error

The `ContinueOnError` strategy allows your consumer to keep processing subsequent messages even when a handler fails. This is critical for high-throughput scenarios where one bad message shouldn't block the entire pipeline.

**Basic Configuration:**

```csharp
builder.AddContinueOnError(new()
{
    OnError = (ex, context) => logger.LogError(ex, "Message processing failed"),
    ShouldHandle = ex => ex is not OperationCanceledException
});
```

**Advanced Configuration:**

```csharp
services.AddResiliencePipeline("MyPipeline", (builder, context) =>
{
    builder.AddContinueOnError(new()
    {
        OnError = (ex, msgContext) => 
        {
            var logger = context.GetRequiredService<ILogger>();
            var deadLetterQueue = context.GetRequiredService<IDeadLetterQueue>();

            logger.LogError(ex, "Message processing failed");
            deadLetterQueue.SendAsync(failedMessage, ex);
        },
        ShouldHandle = ex => 
        {
            // Return true to swallow and continue
            // Return false to propagate and stop
            return ex is not OperationCanceledException;
        }
    });
}
```

### Retry

Automatically retry failed message processing:

```csharp
builder.AddRetry(new()
{
    MaxRetryAttempts = 3,
    Delay = TimeSpan.FromSeconds(1)
});
```

**With exponential backoff:**

```csharp
builder.AddRetry(new()
{
    MaxRetryAttempts = 5,
    BackoffType = DelayBackoffType.Exponential,
    Delay = TimeSpan.FromSeconds(2)
});
```

**Handle specific exceptions:**

```csharp
builder.AddRetry(new()
{
    MaxRetryAttempts = 3,
    Delay = TimeSpan.FromSeconds(1),
    ShouldHandle = new PredicateBuilder()
        .Handle<HttpRequestException>()
        .Handle<TimeoutException>()
});
```

### Timeout

Prevent long-running message handlers from blocking your consumer:

```csharp
builder.AddTimeout(TimeSpan.FromSeconds(30));
```

### Circuit Breaker

Fail fast and protect downstream services when error rates exceed thresholds:

```csharp
builder.AddCircuitBreaker(new()
{
    FailureRatio = 0.5,          // Break after 50% failure rate
    MinimumThroughput = 10,      // At least 10 operations before breaking
    BreakDuration = TimeSpan.FromMinutes(1)
});
```

### Multiple Strategies

Combine multiple strategies for robust error handling:

```csharp
// Register a comprehensive resilience pipeline
services.AddResiliencePipeline("comprehensive-pipeline", builder =>
{
    builder.AddContinueOnError(new ContinueOnErrorStrategyOptions
    {
        OnError = (ex, context) => logger.LogError(ex, "Processing failed after all retries")
    })
    .AddRetry(new()
    {
        MaxRetryAttempts = 5,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(2)
    })
    .AddTimeout(TimeSpan.FromSeconds(30))
    .AddCircuitBreaker(new()
    {
        FailureRatio = 0.5,
        MinimumThroughput = 10,
        BreakDuration = TimeSpan.FromMinutes(1)
    });
});

// Use it in your consumer
services.AddConsumer(options =>
{
    options.WithResiliencePipeline("comprehensive-pipeline");
});
```

## How It Works

### Pipeline Execution Flow

```
┌─────────────────┐
│ Message Received│
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ ContinueOnError │ ← Swallows final exceptions
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│     Retry       │ ← Retries on transient failures
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│    Timeout      │ ← Cancels slow operations
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Circuit Breaker │ ← Breaks circuit on threshold
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Message Handler │
└─────────────────┘
```

## Best Practices

### Strategy Ordering

Order strategies from **outermost to innermost** based on your desired behavior:

```csharp
// ✅ Correct order
builder.AddContinueOnError(options);  // Outermost - handles final failures
builder.AddRetry(retryOptions);       // Middle - attempts recovery
builder.AddTimeout(timeout);          // Innermost - controls individual attempts
```

```csharp
// ❌ Wrong order - retry will never execute
builder.AddRetry(retryOptions);    
builder.AddContinueOnError(options);  // Swallows exceptions before retry
```

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This project is maintained by [DFDS](https://github.com/dfds).

## Related Projects

- [Dafda](https://github.com/dfds/dafda) — Lightweight Kafka library for .NET
- [Polly](https://github.com/App-vNext/Polly) — Resilience and transient-fault-handling library
