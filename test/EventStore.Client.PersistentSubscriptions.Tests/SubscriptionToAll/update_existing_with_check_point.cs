namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class update_existing_with_check_point
	: IClassFixture<update_existing_with_check_point.Fixture> {
	const string Group = "existing-with-check-point";

	readonly Fixture _fixture;

	public update_existing_with_check_point(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task resumes_from_check_point() {
		var resumedEvent = await _fixture.Resumed.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.True(resumedEvent.Event.Position > _fixture.CheckPoint);
	}

	public class Fixture : EventStoreClientFixture {
		readonly TaskCompletionSource<bool>          _appeared;
		readonly List<ResolvedEvent>                 _appearedEvents;
		readonly TaskCompletionSource<ResolvedEvent> _checkPointSource;

		readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> _droppedSource;
		readonly EventData[]                                                   _events;
		readonly TaskCompletionSource<ResolvedEvent>                           _resumedSource;

		StreamSubscription?     _checkPointSubscription;
		PersistentSubscription? _firstSubscription;
		PersistentSubscription? _secondSubscription;

		public Fixture() {
			_droppedSource    = new();
			_resumedSource    = new();
			_checkPointSource = new();
			_appeared         = new();
			_appearedEvents   = new();
			_events           = CreateTestEvents(5).ToArray();
		}

		public Task<ResolvedEvent> Resumed    => _resumedSource.Task;
		public Position            CheckPoint { get; private set; }

		protected override async Task Given() {
			foreach (var e in _events)
				await StreamsClient.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] { e });

			await Client.CreateToAllAsync(
				Group,
				new(
					checkPointLowerBound: 5,
					checkPointAfter: TimeSpan.FromSeconds(1),
					startFrom: Position.Start
				),
				userCredentials: TestCredentials.Root
			);

			var checkPointStream = $"$persistentsubscription-$all::{Group}-checkpoint";
			_checkPointSubscription = await StreamsClient.SubscribeToStreamAsync(
				checkPointStream,
				FromStream.Start,
				(_, e, _) => {
					_checkPointSource.TrySetResult(e);
					return Task.CompletedTask;
				},
				subscriptionDropped: (_, reason, ex) => {
					if (ex is not null)
						_checkPointSource.TrySetException(ex);
					else
						_checkPointSource.TrySetResult(default);
				},
				userCredentials: TestCredentials.Root
			);

			_firstSubscription = await Client.SubscribeToAllAsync(
				Group,
				async (s, e, r, ct) => {
					_appearedEvents.Add(e);

					if (_appearedEvents.Count == _events.Length)
						_appeared.TrySetResult(true);

					await s.Ack(e);
				},
				(subscription, reason, ex) => _droppedSource.TrySetResult((reason, ex)),
				TestCredentials.Root
			);

			await Task.WhenAll(_appeared.Task, _checkPointSource.Task).WithTimeout();

			CheckPoint = _checkPointSource.Task.Result.Event.Data.ParsePosition();
		}

		protected override async Task When() {
			// Force restart of the subscription
			await Client.UpdateToAllAsync(Group, new(), userCredentials: TestCredentials.Root);

			await _droppedSource.Task.WithTimeout();

			_secondSubscription = await Client.SubscribeToAllAsync(
				Group,
				async (s, e, r, ct) => {
					_resumedSource.TrySetResult(e);
					await s.Ack(e);
				},
				(_, reason, ex) => {
					if (ex is not null)
						_resumedSource.TrySetException(ex);
					else
						_resumedSource.TrySetResult(default);
				},
				TestCredentials.Root
			);

			foreach (var e in _events)
				await StreamsClient.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] { e });
		}

		public override Task DisposeAsync() {
			_firstSubscription?.Dispose();
			_secondSubscription?.Dispose();
			_checkPointSubscription?.Dispose();
			return base.DisposeAsync();
		}
	}
}
