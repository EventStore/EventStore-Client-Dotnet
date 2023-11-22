namespace EventStore.Client.Streams.Tests.Bugs; 

public class Issue_104 : IClassFixture<Issue_104.Fixture> {
	readonly Fixture _fixture;

	public Issue_104(Fixture fixture) => _fixture = fixture;

	[Fact]
	public async Task subscription_does_not_send_checkpoint_reached_after_disposal() {
		var streamName                   = _fixture.GetStreamName();
		var ignoredStreamName            = $"ignore_{streamName}";
		var subscriptionDisposed         = new TaskCompletionSource<bool>();
		var eventAppeared                = new TaskCompletionSource<bool>();
		var checkpointReachAfterDisposed = new TaskCompletionSource<bool>();

		await _fixture.Client.AppendToStreamAsync(streamName, StreamRevision.None, _fixture.CreateTestEvents());

		var subscription = await _fixture.Client.SubscribeToAllAsync(
			FromAll.Start,
			(_, _, _) => {
				eventAppeared.TrySetResult(true);
				return Task.CompletedTask;
			},
			false,
			(_, _, _) => subscriptionDisposed.TrySetResult(true),
			new(
				StreamFilter.Prefix(streamName),
				1,
				(_, _, _) => {
					if (!subscriptionDisposed.Task.IsCompleted)
						return Task.CompletedTask;

					checkpointReachAfterDisposed.TrySetResult(true);
					return Task.CompletedTask;
				}
			),
			new("admin", "changeit")
		);

		await eventAppeared.Task;

		subscription.Dispose();
		await subscriptionDisposed.Task;

		await _fixture.Client.AppendToStreamAsync(
			ignoredStreamName,
			StreamRevision.None,
			_fixture.CreateTestEvents(50)
		);

		var delay  = Task.Delay(300);
		var result = await Task.WhenAny(delay, checkpointReachAfterDisposed.Task);
		Assert.Equal(delay, result); // iow 300ms have passed without seeing checkpointReachAfterDisposed
	}

	public class Fixture : EventStoreClientFixture {
		protected override Task Given() => Task.CompletedTask;

		protected override Task When() => Task.CompletedTask;
	}
}