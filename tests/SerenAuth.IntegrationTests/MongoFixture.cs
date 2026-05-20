using Testcontainers.MongoDb;
using Xunit;

namespace SerenAuth.IntegrationTests;

/// <summary>
/// Spins up an ephemeral MongoDB container per test class so integration
/// tests run in isolation without touching a shared dev DB.
/// </summary>
public sealed class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
