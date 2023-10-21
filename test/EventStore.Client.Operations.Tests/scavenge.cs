namespace EventStore.Client;

public class @scavenge : IClassFixture<EventStoreClientsFixture> {
    public scavenge(EventStoreClientsFixture fixture, ITestOutputHelper output) {
        Fixture = fixture.With(f => f.CaptureLogs(output));
        // runInMemory: false
    }

    EventStoreClientsFixture Fixture { get; }

    [Fact]
    public async Task start() {
        var result = await Fixture.Operations.StartScavengeAsync(userCredentials: TestCredentials.Root);
        
        result.ShouldBe(DatabaseScavengeResult.Started(result.ScavengeId));
    }

    [Fact]
    public async Task start_without_credentials_throws() {
        await Fixture.Operations
            .StartScavengeAsync()
            .ShouldThrowAsync<AccessDeniedException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task start_with_thread_count_le_one_throws(int threadCount) {
        var ex = await Fixture.Operations
            .StartScavengeAsync(threadCount)
            .ShouldThrowAsync<ArgumentOutOfRangeException>();

        ex.ParamName.ShouldBe(nameof(threadCount));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    [InlineData(int.MinValue)]
    public async Task start_with_start_from_chunk_lt_zero_throws(int startFromChunk) {
        var ex = await Fixture.Operations
            .StartScavengeAsync(startFromChunk: startFromChunk)
            .ShouldThrowAsync<ArgumentOutOfRangeException>();

        ex.ParamName.ShouldBe(nameof(startFromChunk));
    }

    [Fact(Skip = "Scavenge on an empty database finishes too quickly")]
    public async Task stop() {
        var startResult = await Fixture.Operations
            .StartScavengeAsync(userCredentials: TestCredentials.Root);
        
        var stopResult = await Fixture.Operations
            .StopScavengeAsync(startResult.ScavengeId, userCredentials: TestCredentials.Root);

        stopResult.ShouldBe(DatabaseScavengeResult.Stopped(startResult.ScavengeId));
    }

    [Fact]
    public async Task stop_when_no_scavenge_is_running() {
        var scavengeId = Guid.NewGuid().ToString();
        
        var ex = await Fixture.Operations
            .StopScavengeAsync(scavengeId, userCredentials: TestCredentials.Root)
            .ShouldThrowAsync<ScavengeNotFoundException>();

        ex.ScavengeId.ShouldBeNull();
    }

    [Fact]
    public async Task stop_without_credentials_throws() =>
        await Fixture.Operations
            .StopScavengeAsync(Guid.NewGuid().ToString())
            .ShouldThrowAsync<AccessDeniedException>();
}