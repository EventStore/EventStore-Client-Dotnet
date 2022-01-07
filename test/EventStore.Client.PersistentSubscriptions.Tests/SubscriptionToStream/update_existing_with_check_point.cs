using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToStream {
	public class update_existing_with_check_point
		: IClassFixture<update_existing_with_check_point.Fixture> {
		private const string Stream = nameof(update_existing_with_check_point);
		private const string Group = "existing-with-check-point";
		private readonly Fixture _fixture;

		public update_existing_with_check_point(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task resumes_from_check_point() {
			var resumedEvent = await _fixture.Resumed.WithTimeout(TimeSpan.FromSeconds(10));
			Assert.Equal(_fixture.CheckPoint.Next(), resumedEvent.Event.EventNumber);
		}
		
		public class Fixture : EventStoreClientFixture {
			public Task<ResolvedEvent> Resumed => _resumedSource.Task;
			public StreamPosition CheckPoint { get; private set; }
			
			private readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception)> _droppedSource;
			private readonly TaskCompletionSource<ResolvedEvent> _resumedSource;
			private readonly TaskCompletionSource<ResolvedEvent> _checkPointSource;
			private PersistentSubscription _firstSubscription;
			private PersistentSubscription _secondSubscription;
			private readonly TaskCompletionSource<bool> _appeared;
			private readonly List<ResolvedEvent> _appearedEvents;
			private readonly EventData[] _events;

			public Fixture() {
				_droppedSource = new TaskCompletionSource<(SubscriptionDroppedReason, Exception)>();
				_resumedSource = new TaskCompletionSource<ResolvedEvent>();
				_checkPointSource = new TaskCompletionSource<ResolvedEvent>();
				_appeared = new TaskCompletionSource<bool>();
				_appearedEvents = new List<ResolvedEvent>();
				_events = CreateTestEvents(5).ToArray();
			}

			protected override async Task Given() {
				await StreamsClient.AppendToStreamAsync(Stream, StreamState.NoStream, _events);
				
				await Client.CreateAsync(Stream, Group,
					new PersistentSubscriptionSettings(
						minCheckPointCount: 5,
						checkPointAfter: TimeSpan.FromSeconds(1),
						startFrom: StreamPosition.Start),
					TestCredentials.Root);

				var checkPointStream = $"$persistentsubscription-{Stream}::{Group}-checkpoint";
				await StreamsClient.SubscribeToStreamAsync(checkPointStream,
					(s, e, ct) => {
						_checkPointSource.TrySetResult(e);
						return Task.CompletedTask;
					},
					userCredentials: TestCredentials.Root);
				
				_firstSubscription = await Client.SubscribeToStreamAsync(Stream, Group,
					eventAppeared: async (s, e, r, ct) => {
						_appearedEvents.Add(e);
						await s.Ack(e);

						if (_appearedEvents.Count == _events.Length)
							_appeared.TrySetResult(true);
					},
					(subscription, reason, ex) => _droppedSource.TrySetResult((reason, ex)),
					TestCredentials.Root);

				await Task.WhenAll(_appeared.Task, _checkPointSource.Task).WithTimeout();

				CheckPoint = _checkPointSource.Task.Result.Event.Data.ParseStreamPosition();
			}
			
			protected override async Task When() {
				// Force restart of the subscription
				await Client.UpdateAsync(Stream, Group, new PersistentSubscriptionSettings(), TestCredentials.Root);
				
				await _droppedSource.Task.WithTimeout();
				
				_secondSubscription = await Client.SubscribeToStreamAsync(Stream, Group,
					eventAppeared: async (s, e, r, ct) => {
						_resumedSource.TrySetResult(e);
						await s.Ack(e);
					},
					userCredentials: TestCredentials.Root);
				
				await StreamsClient.AppendToStreamAsync(Stream, StreamState.Any, CreateTestEvents(1));
			}

			public override Task DisposeAsync() {
				_firstSubscription?.Dispose();
				_secondSubscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
