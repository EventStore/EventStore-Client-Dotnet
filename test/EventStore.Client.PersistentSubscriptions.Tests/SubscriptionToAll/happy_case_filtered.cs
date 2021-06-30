using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class happy_case_filtered : IClassFixture<happy_case_filtered.Fixture> {
		private readonly Fixture _fixture;

		public happy_case_filtered(Fixture fixture) {
			_fixture = fixture;
		}
		
		public static IEnumerable<object[]> FilterCases() => Filters.All.Select(filter => new object[] {filter});

		[Theory, MemberData(nameof(FilterCases))]
		public async Task reads_all_existing_filtered_events(string filterName) {
			var streamPrefix = $"{filterName}-{_fixture.GetStreamName()}";
			var (getFilter, prepareEvent) = Filters.GetFilter(filterName);
			var filter = getFilter(streamPrefix);
			
			var appeared = new TaskCompletionSource<bool>();
			var appearedEvents = new List<EventRecord>();
			var events = _fixture.CreateTestEvents(20).Select(e => prepareEvent(streamPrefix, e)).ToArray();

			foreach (var e in events) {
				await _fixture.StreamsClient.AppendToStreamAsync($"{streamPrefix}_{Guid.NewGuid():n}",
					StreamState.NoStream, new[] {e});
			}
			
			await _fixture.Client.CreateToAllAsync(filterName, filter,
				new PersistentSubscriptionSettings(startFrom: Position.Start), TestCredentials.Root);
			
			using var subscription = await _fixture.Client.SubscribeToAllAsync(filterName,
				eventAppeared: (s, e, r, ct) => {
					appearedEvents.Add(e.Event);
					if (appearedEvents.Count >=  events.Length) {
						appeared.TrySetResult(true);
					}
					return Task.CompletedTask;
				},
				userCredentials: TestCredentials.Root)
				.WithTimeout();

			await Task.WhenAll(appeared.Task).WithTimeout();
			
			Assert.Equal(events.Select(x => x.EventId), appearedEvents.Select(x => x.EventId));
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
