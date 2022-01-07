using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class update_existing_with_check_point
		: IClassFixture<update_existing_with_check_point.Fixture> {

		private const string Group = "existing-with-check-point";
		private readonly Fixture _fixture;

		public update_existing_with_check_point(Fixture fixture) {
			_fixture = fixture;
		}

		[Fact]
		public async Task resumes_from_check_point() {
			var resumedEvent = await _fixture.Resumed.WithTimeout(TimeSpan.FromSeconds(10));
			Assert.True(resumedEvent.Event.Position > _fixture.CheckPoint);
		}

		public class Fixture : EventStoreClientFixture {
			public Task<ResolvedEvent> Resumed => _resumedSource.Task;
			public Position CheckPoint { get; private set; }
			
			private readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception)> _droppedSource;
			private readonly TaskCompletionSource<ResolvedEvent> _resumedSource;
			private readonly TaskCompletionSource<ResolvedEvent> _checkPointSource;
			private PersistentSubscription _firstSubscription;
			private PersistentSubscription _secondSubscription;
			private StreamSubscription _checkPointSubscription;
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
				foreach (var e in _events) {
					await StreamsClient.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] {e});
				}
				
				await Client.CreateToAllAsync(Group,
					new PersistentSubscriptionSettings(
						minCheckPointCount: 5,
						checkPointAfter: TimeSpan.FromSeconds(1),
						startFrom: Position.Start),
					TestCredentials.Root);

				var checkPointStream = $"$persistentsubscription-$all::{Group}-checkpoint";
				_checkPointSubscription = await StreamsClient.SubscribeToStreamAsync(checkPointStream,
					(s, e, ct) => {
						_checkPointSource.TrySetResult(e);	
						return Task.CompletedTask;
					},
					userCredentials: TestCredentials.Root);
				
				_firstSubscription = await Client.SubscribeToAllAsync(Group,
					eventAppeared: async (s, e, r, ct) => {
						_appearedEvents.Add(e);

						if (_appearedEvents.Count == _events.Length)
							_appeared.TrySetResult(true);
						await s.Ack(e);
					},
					(subscription, reason, ex) => _droppedSource.TrySetResult((reason, ex)),
					TestCredentials.Root);

				await Task.WhenAll(_appeared.Task, _checkPointSource.Task).WithTimeout();

				CheckPoint = _checkPointSource.Task.Result.Event.Data.ParsePosition();
			}

			protected override async Task When() {
				// Force restart of the subscription
				await Client.UpdateToAllAsync(Group, new PersistentSubscriptionSettings(), TestCredentials.Root);

				await _droppedSource.Task.WithTimeout();
				
				_secondSubscription = await Client.SubscribeToAllAsync(Group,
					eventAppeared: async (s, e, r, ct) => {
						_resumedSource.TrySetResult(e);
						await s.Ack(e);
					},
					userCredentials: TestCredentials.Root);
				
				foreach (var e in _events) {
					await StreamsClient.AppendToStreamAsync("test-" + Guid.NewGuid(), StreamState.Any, new[] {e});
				}
			}

			public override Task DisposeAsync() {
				_firstSubscription?.Dispose();
				_secondSubscription?.Dispose();
				_checkPointSubscription?.Dispose();
				return base.DisposeAsync();
			}
		}
	}
}
