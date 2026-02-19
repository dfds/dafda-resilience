using Dafda.Resilience.Strategies;
using Polly;

namespace Dafda.Resilience.Tests.Strategies;

public sealed class ContinueOnErrorStrategyTests
{
    [Fact]
    public void ExecutePipeline_NoException_ResultIsReturned()
    {
        var onErrorCalls = 0;
        var pipeline = CreatePipeline(options => options.OnError = (_, __) => onErrorCalls++);

        var result = pipeline.Execute(() => "expected");

        Assert.Equal("expected", result);
        Assert.Equal(0, onErrorCalls);
    }

    [Fact]
    public void ExecutePipeline_ThrowsHandledException_ExceptionIsSwallowedAndOnErrorIsInvoked()
    {
        Exception? capturedException = null;
        var exceptionToThrow = new InvalidOperationException("boom");

        var pipeline = CreatePipeline(options =>
        {
            options.OnError = (ex, _) => capturedException = ex;
            options.ShouldHandle = ex => ex is InvalidOperationException;
        });

        var result = pipeline.Execute<string>(() => throw exceptionToThrow);

        Assert.Null(result);
        Assert.Same(exceptionToThrow, capturedException);
    }

    [Fact]
    public void ExecutePipeline_ThrowsUnhandledException_ExceptionIsPropagated()
    {
        var onErrorCalls = 0;
        var exceptionToThrow = new InvalidOperationException("boom");

        var pipeline = CreatePipeline(options =>
        {
            options.OnError = (_, __) => onErrorCalls++;
            options.ShouldHandle = _ => false;
        });

        var thrown = Assert.Throws<InvalidOperationException>(() => pipeline.Execute(() => throw exceptionToThrow));

        Assert.Same(exceptionToThrow, thrown);
        Assert.Equal(0, onErrorCalls);
    }

    [Fact]
    public void ExecutePipeline_OnErrorThrowsException_ExceptionIsPropagated()
    {
        var onErrorException = new ApplicationException("handler failure");

        var pipeline = CreatePipeline(options =>
        {
            options.OnError = (_, __) => throw onErrorException;
            options.ShouldHandle = _ => true;
        });

        var thrown = Assert.Throws<ApplicationException>(() => pipeline.Execute(() => throw new InvalidOperationException("boom")));

        Assert.Same(onErrorException, thrown);
    }

    private static ResiliencePipeline CreatePipeline(Action<ContinueOnErrorStrategyOptions>? configure = null)
    {
        var options = new ContinueOnErrorStrategyOptions();
        configure?.Invoke(options);

        var builder = new ResiliencePipelineBuilder();
        builder.AddContinueOnError(options);

        return builder.Build();
    }
}
