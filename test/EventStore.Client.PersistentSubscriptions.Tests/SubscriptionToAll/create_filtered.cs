using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.SubscriptionToAll {
	public class create_filtered : IClassFixture<create_filtered.Fixture> {
		public create_filtered(Fixture fixture) {
			_fixture = fixture;
		}

		private readonly Fixture _fixture;
		public static IEnumerable<object[]> FilterCases() => Filters.All.Select(filter => new object[] {filter});

		[SupportsPSToAll.Theory, MemberData(nameof(FilterCases))]
		public async Task the_completion_succeeds(string filterName) {
			var streamPrefix = _fixture.GetStreamName();
			var (getFilter, _) = Filters.GetFilter(filterName);
			var filter = getFilter(streamPrefix);
			
			await _fixture.Client.CreateToAllAsync(
				filterName,
				filter,
				new PersistentSubscriptionSettings(),
				userCredentials: TestCredentials.Root);
		}
		
		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;
			protected override Task When() => Task.CompletedTask;
		}
	}
}
