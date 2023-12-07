namespace EventStore.Client.Streams.Tests; 

[Trait("Category", "LongRunning")]
[Trait("Category", "Stream")]
[Trait("Category", "Append")]
public class appending_to_implicitly_created_stream(ITestOutputHelper output, EventStoreFixture fixture) : EventStoreTests<EventStoreFixture>(output, fixture) {
	[Fact]
	public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0em1_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(6).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[Fact]
	public async Task sequence_0em1_1e0_2e1_3e2_4e3_4e4_0any_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(6).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[Fact]
	public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e5_non_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(6).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(5), events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 2).CountAsync();

		Assert.Equal(events.Length + 1, count);
	}

	[Fact]
	public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e6_throws_wev() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(6).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		await Assert.ThrowsAsync<WrongExpectedVersionException>(() => Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(6), events.Take(1)));
	}

	[Fact]
	public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e6_returns_wev() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(6).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			new StreamRevision(6),
			events.Take(1),
			options => options.ThrowOnAppendFailure = false
		);

		Assert.IsType<WrongExpectedVersionResult>(writeResult);
	}

	[Fact]
	public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e4_throws_wev() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(6).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		await Assert.ThrowsAsync<WrongExpectedVersionException>(() => Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(4), events.Take(1)));
	}

	[Fact]
	public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e4_returns_wev() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(6).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			new StreamRevision(4),
			events.Take(1),
			options => options.ThrowOnAppendFailure = false
		);

		Assert.IsType<WrongExpectedVersionResult>(writeResult);
	}

	[Fact]
	public async Task sequence_0em1_0e0_non_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents().ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(0), events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 2).CountAsync();

		Assert.Equal(events.Length + 1, count);
	}

	[Fact]
	public async Task sequence_0em1_0any_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents().ToArray();

		await Task.Delay(TimeSpan.FromSeconds(30));
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[Fact]
	public async Task sequence_0em1_0em1_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents().ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[Fact]
	public async Task sequence_0em1_1e0_2e1_1any_1any_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(3).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events.Skip(1).Take(1));
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events.Skip(1).Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[Fact]
	public async Task sequence_S_0em1_1em1_E_S_0em1_E_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(2).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[Fact]
	public async Task sequence_S_0em1_1em1_E_S_0any_E_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(2).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[Fact]
	public async Task sequence_S_0em1_1em1_E_S_1e0_E_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(2).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		await Fixture.Streams.AppendToStreamAsync(stream, new StreamRevision(0), events.Skip(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[Fact]
	public async Task sequence_S_0em1_1em1_E_S_1any_E_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(2).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events.Skip(1).Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[Fact]
	public async Task sequence_S_0em1_1em1_E_S_0em1_1em1_2em1_E_idempotancy_fail_throws() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(3).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(2));

		await Assert.ThrowsAsync<WrongExpectedVersionException>(
			() => Fixture.Streams.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				events
			)
		);
	}

	[Fact]
	public async Task sequence_S_0em1_1em1_E_S_0em1_1em1_2em1_E_idempotancy_fail_returns() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(3).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(2));

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			events,
			options => options.ThrowOnAppendFailure = false
		);

		Assert.IsType<WrongExpectedVersionResult>(writeResult);
	}
}