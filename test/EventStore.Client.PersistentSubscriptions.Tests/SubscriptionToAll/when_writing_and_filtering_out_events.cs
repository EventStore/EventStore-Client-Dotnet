using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class when_writing_and_filtering_out_events
		: IClassFixture<when_writing_and_filtering_out_events.Fixture> {
		private const string Group = "filtering-out-events";
		private readonly Fixture _fixture;

		public when_writing_and_filtering_out_events(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task it_should_write_a_check_point() {
			await _fixture.SecondCheckPoint.WithTimeout();
			var secondCheckPoint = _fixture.SecondCheckPoint.Result.Event.Data.ParsePosition();
			Assert.True(secondCheckPoint > _fixture.FirstCheckPoint);
			Assert.Equal(_fixture.Events.Select(e => e.EventId),
				_fixture.AppearedEvents.Select(e => e.Event.EventId));
		}

		public class Fixture : EventStoreClientFixture {
			public Task<ResolvedEvent> SecondCheckPoint => _secondCheckPointSource.Task;
			public Position FirstCheckPoint { get; private set; }
			public EventData[] Events => _events.ToArray();
			public ResolvedEvent[] AppearedEvents => _appearedEvents.ToArray();

			private readonly TaskCompletionSource<ResolvedEvent> _firstCheckPointSource, _secondCheckPointSource;
			private PersistentSubscription _subscription;
			private StreamSubscription _checkPointSubscription;
			private readonly TaskCompletionSource<bool> _appeared;
			private readonly List<ResolvedEvent> _appearedEvents, _checkPoints;
			private readonly EventData[] _events;
			private readonly string _checkPointStream = $"$persistentsubscription-$all::{Group}-checkpoint";

			public Fixture() {
				_firstCheckPointSource = new TaskCompletionSource<ResolvedEvent>();
				_secondCheckPointSource = new TaskCompletionSource<ResolvedEvent>();
				_appeared = new TaskCompletionSource<bool>();
				_appearedEvents = new List<ResolvedEvent>();
				_checkPoints = new List<ResolvedEvent>();
				_events = CreateTestEvents(5).ToArray();
			}

			protected override async Task Given() {
				foreach (var e in _events) {
					await StreamsClient.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] {e});
				}

				await Client.CreateToAllAsync(Group,
					StreamFilter.Prefix("test"),
					new PersistentSubscriptionSettings(
						minCheckPointCount: 5,
						checkPointAfter: TimeSpan.FromSeconds(1),
						startFrom: Position.Start),
					TestCredentials.Root);

				_checkPointSubscription = await StreamsClient.SubscribeToStreamAsync(_checkPointStream,
					FromStream.Start,
					(_, e, _) => {
						if (_checkPoints.Count == 0) {
							_firstCheckPointSource.TrySetResult(e);
						} else {
							_secondCheckPointSource.TrySetResult(e);
						}

						_checkPoints.Add(e);
						return Task.CompletedTask;
					},
					userCredentials: TestCredentials.Root);

				_subscription = await Client.SubscribeToAllAsync(Group,
					eventAppeared: async (s, e, r, ct) => {
						_appearedEvents.Add(e);

						if (_appearedEvents.Count == _events.Length)
							_appeared.TrySetResult(true);
						await s.Ack(e);
					},
					userCredentials: TestCredentials.Root);

				await Task.WhenAll(_appeared.Task, _firstCheckPointSource.Task).WithTimeout();

				FirstCheckPoint = _firstCheckPointSource.Task.Result.Event.Data.ParsePosition();
			}

			protected override async Task When() {
				foreach (var e in _events) {
					await StreamsClient.AppendToStreamAsync("filtered-out-stream-" + Guid.NewGuid(),
						StreamState.Any, new[] {e});
				}
			}

			public override Task DisposeAsync() {
				_subscription?.Dispose();
				_checkPointSubscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
