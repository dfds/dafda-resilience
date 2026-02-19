using Dafda.Resilience;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Microsoft.Extensions.Logging;
using Dafda.Configuration;

Console.WriteLine("Hello, World!");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddResiliencePipeline("MyPipeline", (builder, context) =>
        {
            builder.AddContinueOnError(new()
            {
                OnError = (ex, msgContext) =>
                {
                    var logger = context.ServiceProvider.GetRequiredService<ILogger>();
                    logger.LogWarning(ex, "An error occurred while processing message, but it will be continued. Error: {ErrorMessage}", ex.Message);
                },
                ShouldHandle = ex => ex is InvalidOperationException
            })
            .AddRetry(new()
            {
                MaxRetryAttempts = 3,
                ShouldHandle = new PredicateBuilder().Handle<TimeoutException>()
            });
        });

        services.AddConsumer(options =>
        {
            options.WithResiliencePipeline("MyPipeline");
        });
    })
    .Build();

await host.RunAsync();