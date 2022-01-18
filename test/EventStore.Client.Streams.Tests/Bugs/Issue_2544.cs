using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable 1998

namespace EventStore.Client.Bugs {
	public class Issue_2544 : IAsyncLifetime {
		private const int BatchSize = 18;
		private const int Batches = 4;
		private readonly Fixture _fixture;
		private readonly Dictionary<StreamPosition, bool> _seen;
		private readonly TaskCompletionSource<bool> _completed;

		public Issue_2544(ITestOutputHelper outputHelper) {
			_fixture = new Fixture();
			_fixture.CaptureLogs(outputHelper);
			_seen = Enumerable.Range(0, 1 + Batches * BatchSize)
				.Select(i => new StreamPosition((ulong)i))
				.ToDictionary(r => r, _ => false);
			_completed = new TaskCompletionSource<bool>();
		}

		public static IEnumerable<object?[]> TestCases() => Enumerable.Range(0, 5)
			.Select(i => new object[] {i});

		[Theory, MemberData(nameof(TestCases))]
		public async Task subscribe_to_stream(int iteration) {
			var streamName = $"{_fixture.GetStreamName()}_{iteration}";
			var startFrom = FromStream.Start;
			
			async Task<StreamSubscription> Subscribe() => await _fixture.Client
				.SubscribeToStreamAsync(streamName, startFrom,
					(_,e,_) => EventAppeared(e, streamName, out startFrom),
					resolveLinkTos: false,
					(s,r,e) => SubscriptionDropped(s, r, e, Subscribe));

			using var _ = await Subscribe();
			
			await AppendEvents(streamName);

			await _completed.Task.WithTimeout();
		}

		[Theory, MemberData(nameof(TestCases))]
		public async Task subscribe_to_all(int iteration) {
			var streamName = $"{_fixture.GetStreamName()}_{iteration}";
			var startFrom = FromAll.Start;
			
			async Task<StreamSubscription> Subscribe() => await _fixture.Client
				.SubscribeToAllAsync(startFrom,
					(_, e, _) => EventAppeared(e, streamName, out startFrom),
					resolveLinkTos: false,
					(s, r, e) => SubscriptionDropped(s, r, e, Subscribe));

			using var _ = await Subscribe();
			
			await AppendEvents(streamName);

			await _completed.Task.WithTimeout();
		}

		[Theory, MemberData(nameof(TestCases))]
		public async Task subscribe_to_all_filtered(int iteration) {
			var streamName = $"{_fixture.GetStreamName()}_{iteration}";
			var startFrom = FromAll.Start;

			async Task<StreamSubscription> Subscribe() => await _fixture.Client
				.SubscribeToAllAsync(startFrom,
					(_, e, _) => EventAppeared(e, streamName, out startFrom),
					resolveLinkTos: false,
					(s, r, e) => SubscriptionDropped(s, r, e, Subscribe),
					new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()));

			await Subscribe();
			
			await AppendEvents(streamName);

			await _completed.Task.WithTimeout();
		}

		private async Task AppendEvents(string streamName) {
			await Task.Delay(TimeSpan.FromMilliseconds(10));

			var expectedRevision = StreamRevision.None;

			for (var i = 0; i < Batches; i++) {
				if (expectedRevision == StreamRevision.None) {
					var result = await _fixture.Client.AppendToStreamAsync(streamName, StreamState.NoStream,
						_fixture.CreateTestEvents(BatchSize));
					expectedRevision = result.NextExpectedStreamRevision;
				} else {
					var result = await _fixture.Client.AppendToStreamAsync(streamName, expectedRevision,
						_fixture.CreateTestEvents(BatchSize));
					expectedRevision = result.NextExpectedStreamRevision;
				}

				await Task.Delay(TimeSpan.FromMilliseconds(10));
			}

			await _fixture.Client.AppendToStreamAsync(streamName, expectedRevision, new[] {
				new EventData(Uuid.NewUuid(), "completed", Array.Empty<byte>(), contentType: "application/octet-stream")
			});
		}

		private void SubscriptionDropped(StreamSubscription _, SubscriptionDroppedReason __, Exception? ex,
			Func<Task<StreamSubscription>> resubscribe) {

			if (ex == null) {
				return;
			}

			if (ex.Message.Contains("too slow") && ex.Message.Contains("resubscription required")) {
				resubscribe();
				return;
			}
			
			_completed.TrySetException(ex);
		}

		private Task EventAppeared(ResolvedEvent e, string streamName, out FromStream startFrom) {
			startFrom = FromStream.After(e.OriginalEventNumber);
			return EventAppeared(e, streamName);
		}

		private Task EventAppeared(ResolvedEvent e, string streamName, out FromAll startFrom) {
			startFrom = FromAll.After(e.OriginalPosition!.Value);
			return EventAppeared(e, streamName);
		}
		
		private Task EventAppeared(ResolvedEvent e, string streamName) {
			if (e.OriginalStreamId != streamName) {
				return Task.CompletedTask;
			}

			if (_seen[e.Event.EventNumber]) {
				throw new Exception($"Event {e.Event.EventNumber} was already seen");
			}

			_seen[e.Event.EventNumber] = true;
			if (e.Event.EventType == "completed") {
				_completed.TrySetResult(true);
			}

			return Task.CompletedTask;
		}

		public Task InitializeAsync() => _fixture.InitializeAsync();

		public Task DisposeAsync() => _fixture.DisposeAsync();

		public class Fixture : EventStoreClientFixture {
			public Fixture() : base(env: new Dictionary<string, string> {
				["EVENTSTORE_LOG_LEVEL"] = "Verbose"
			}) {
			}

			protected override Task Given() => Client.SetStreamMetadataAsync(SystemStreams.AllStream, StreamState.Any,
				new StreamMetadata(acl: new StreamAcl(SystemRoles.All)), userCredentials: TestCredentials.Root);

			protected override Task When() => Task.CompletedTask;
		}
	}
}
