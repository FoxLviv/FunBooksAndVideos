using FunBooksAndVideos.TestKit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace FunBooksAndVideos.Api.Tests.Infrastructure;

/// <summary>
/// Boots the real API in-process with the demo data and a frozen clock. Every factory instance owns its
/// own in-memory store, so test classes that share a factory share state and must create their own customers.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public FakeTimeProvider Clock { get; } = TestClock.Create();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        // No console/EventLog providers: keeps test output quiet and avoids EventLog permission issues in sandboxes.
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }
}
