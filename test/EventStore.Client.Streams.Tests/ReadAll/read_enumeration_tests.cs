namespace EventStore.Client.Streams.Tests;

[Trait("Category", "Network")]
[Trait("Category", "LongRunning")]
[Trait("Category", "AllStream")]
[Trait("Category", "Stream")]
[Trait("Category", "Read")]
public class read_enumeration_tests : IClassFixture<EventStoreFixture> {
	public read_enumeration_tests(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task all_referencing_messages_twice_does_not_throw() {
		var result = Fixture.Streams.ReadAllAsync(
			Direction.Forwards,
			Position.Start,
			32,
			userCredentials: TestCredentials.Root
		);

		_ = result.Messages;
		await result.Messages.ToArrayAsync();
	}

	[Fact]
	public async Task all_enumerating_messages_twice_throws() {
		var result = Fixture.Streams.ReadAllAsync(
			Direction.Forwards,
			Position.Start,
			32,
			userCredentials: TestCredentials.Root
		);

		await result.Messages.ToArrayAsync();

		await Assert.ThrowsAsync<InvalidOperationException>(
			async () =>
				await result.Messages.ToArrayAsync()
		);
	}

	[Fact]
	public async Task referencing_messages_twice_does_not_throw() {
		var result = Fixture.Streams.ReadStreamAsync(
			Direction.Forwards,
			"$users",
			StreamPosition.Start,
			32,
			userCredentials: TestCredentials.Root
		);

		_ = result.Messages;
		await result.Messages.ToArrayAsync();
	}

	[Fact]
	public async Task enumerating_messages_twice_throws() {
		var result = Fixture.Streams.ReadStreamAsync(
			Direction.Forwards,
			"$users",
			StreamPosition.Start,
			32,
			userCredentials: TestCredentials.Root
		);

		await result.Messages.ToArrayAsync();

		await Assert.ThrowsAsync<InvalidOperationException>(
			async () =>
				await result.Messages.ToArrayAsync()
		);
	}
}