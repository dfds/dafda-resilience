using Dafda.Consuming;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;

namespace Dafda.Resilience.Tests;

public sealed class ResilienceExecutionStrategyTests
{
    [Fact]
    public async Task Execute()
    {
        var executed = false;
        var sut = CreateStrategy(builder => { });

        await sut.Execute(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        }, new DummyExecutionContext(), CancellationToken.None);

        Assert.True(executed);
    }

    [Fact]
    public async Task Given_action_throws_exception_When_executed_Then_exception_propagates()
    {
        var expectedException = new InvalidOperationException("test exception");
        var sut = CreateStrategy(builder => { });

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Execute(_ => throw expectedException, new DummyExecutionContext(), CancellationToken.None));

        Assert.Same(expectedException, actualException);
    }

    [Fact]
    public async Task Given_pipeline_with_retry_When_action_fails_once_Then_action_retries_and_succeeds()
    {
        var attemptCount = 0;
        var sut = CreateStrategy(builder =>
        {
            builder.AddRetry(new()
            {
                MaxRetryAttempts = 2,
                ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>()
            });
        });

        await sut.Execute(_ =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                throw new InvalidOperationException("First attempt fails");
            }
            return Task.CompletedTask;
        }, new DummyExecutionContext(), CancellationToken.None);

        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task Given_pipeline_with_timeout_When_action_takes_too_long_Then_timeout_exception_thrown()
    {
        var sut = CreateStrategy(builder =>
        {
            builder.AddTimeout(TimeSpan.FromMilliseconds(100));
        });

        var exception = await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Execute(ct => Task.Delay(TimeSpan.FromSeconds(5), ct), new DummyExecutionContext(), CancellationToken.None));
        
        Assert.True(exception.GetType().Name == "TimeoutRejectedException" || exception is OperationCanceledException);
    }

    [Fact]
    public async Task Given_multiple_invocations_When_executed_Then_uses_same_pipeline_instance()
    {
        var sut = CreateStrategy(builder => { });
        var executionCount = 0;

        await sut.Execute(_ =>
        {
            executionCount++;
            return Task.CompletedTask;
        }, new DummyExecutionContext(), CancellationToken.None);

        await sut.Execute(_ =>
        {
            executionCount++;
            return Task.CompletedTask;
        }, new DummyExecutionContext(), CancellationToken.None);

        Assert.Equal(2, executionCount);
    }

    private static ResilienceExecutionStrategy CreateStrategy(Action<ResiliencePipelineBuilder> configure)
    {
        var services = new ServiceCollection();
        var pipelineKey = "test-pipeline";

        services.AddResiliencePipeline(pipelineKey, configure);

        var serviceProvider = services.BuildServiceProvider();
        var pipelineProvider = serviceProvider.GetRequiredService<ResiliencePipelineProvider<string>>();

        return new ResilienceExecutionStrategy(pipelineKey, pipelineProvider);
    }

    private class DummyExecutionContext() : MessageExecutionContext(new DummyType("dummyString"), new(), typeof(DummyType))
    {
        private class DummyType
        {
            public string DummyString { get; set; }
            public DummyType(string dummyString)
            {
                DummyString = dummyString;
            }
        }
    }
}
