namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "Network")]
public class read_enumeration_tests : IClassFixture<read_enumeration_tests.Fixture> {
	readonly Fixture _fixture;

	public read_enumeration_tests(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task all_referencing_messages_twice_does_not_throw() {
		var result = _fixture.Client.ReadAllAsync(
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
		var result = _fixture.Client.ReadAllAsync(
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
		var result = _fixture.Client.ReadStreamAsync(
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
		var result = _fixture.Client.ReadStreamAsync(
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

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;
		protected override Task When()  => Task.CompletedTask;
	}
}