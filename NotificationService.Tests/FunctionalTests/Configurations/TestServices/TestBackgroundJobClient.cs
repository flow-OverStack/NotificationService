using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;

namespace NotificationService.Tests.FunctionalTests.Configurations.TestServices;

/// <summary>
///     Runs Hangfire jobs synchronously, inline, instead of on a background server - so functional tests can await
///     their effect without polling.
/// </summary>
internal class TestBackgroundJobClient(IServiceProvider provider) : IBackgroundJobClient
{
    public string Create(Job job, IState state)
    {
        object? instance = null;
        AsyncServiceScope? scope = null;

        try
        {
            if (!job.Method.IsStatic)
            {
                scope = provider.CreateAsyncScope();
                instance = scope.Value.ServiceProvider.GetService(job.Type) ??
                           ActivatorUtilities.CreateInstance(provider, job.Type);
            }

            var result = job.Method.Invoke(instance, job.Args.ToArray());

            switch (result)
            {
                case Task task:
                    task.GetAwaiter().GetResult();
                    break;
                case ValueTask valueTask:
                    valueTask.AsTask().GetAwaiter().GetResult();
                    break;
            }
        }
        finally
        {
            scope?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        return "test-job-id";
    }

    public bool ChangeState(string jobId, IState state, string expectedState)
    {
        return true;
    }
}
