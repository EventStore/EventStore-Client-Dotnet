using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class happy_case_filtered_with_start_from_set : IClassFixture<happy_case_filtered_with_start_from_set.Fixture> {
		private readonly Fixture _fixture;

		public happy_case_filtered_with_start_from_set(Fixture fixture) {
			_fixture = fixture;
		}
		
		public static IEnumerable<object?[]> FilterCases() => Filters.All.Select(filter => new object[] {filter});

		[SupportsPSToAll.Theory, MemberData(nameof(FilterCases))]
		public async Task reads_all_existing_filtered_events_from_specified_start(string filterName) {
			var streamPrefix = $"{filterName}-{_fixture.GetStreamName()}";
			var (getFilter, prepareEvent) = Filters.GetFilter(filterName);
			var filter = getFilter(streamPrefix);
			
			var appeared = new TaskCompletionSource<bool>();
			var appearedEvents = new List<EventRecord>();
			var events = _fixture.CreateTestEvents(20).Select(e => prepareEvent(streamPrefix, e)).ToArray();
			var eventsToSkip = events.Take(10).ToArray();
			var eventsToCapture = events.Skip(10).ToArray();
			IWriteResult? eventToCaptureResult = null;

			foreach (var e in eventsToSkip) {
				await _fixture.StreamsClient.AppendToStreamAsync($"{streamPrefix}_{Guid.NewGuid():n}",
					StreamState.NoStream, new[] {e});
			}
			foreach (var e in eventsToCapture) {
				var result = await _fixture.StreamsClient.AppendToStreamAsync($"{streamPrefix}_{Guid.NewGuid():n}",
					StreamState.NoStream, new[] {e});
				eventToCaptureResult ??= result;
			}
			
			await _fixture.Client.CreateToAllAsync(filterName, filter,
				new PersistentSubscriptionSettings(startFrom: eventToCaptureResult!.LogPosition),
				userCredentials: TestCredentials.Root);
			
			using var subscription = await _fixture.Client.SubscribeToAllAsync(filterName,
				eventAppeared: async (s, e, r, ct) => {
					appearedEvents.Add(e.Event);
					if (appearedEvents.Count >=  eventsToCapture.Length) {
						appeared.TrySetResult(true);
					}
					await s.Ack(e);
				},
				userCredentials: TestCredentials.Root)
				.WithTimeout();

			await Task.WhenAll(appeared.Task).WithTimeout();
			
			Assert.Equal(eventsToCapture.Select(x => x.EventId), appearedEvents.Select(x => x.EventId));
		}
		
		public class Fixture : EventStoreClientFixture {
			protected override async Task Given() {
				await StreamsClient.AppendToStreamAsync(Guid.NewGuid().ToString(), StreamState.NoStream,
					CreateTestEvents(256));

				await StreamsClient.SetStreamMetadataAsync(SystemStreams.AllStream, StreamState.Any,
					new StreamMetadata(acl: new StreamAcl(SystemRoles.All)), userCredentials: TestCredentials.Root);
			}
			
			protected override Task When() => Task.CompletedTask;
		}
	}
}
