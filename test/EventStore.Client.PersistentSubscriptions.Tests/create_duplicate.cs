using System;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class create_duplicate
		: IClassFixture<create_duplicate.Fixture> {
		public create_duplicate(Fixture fixture) {
			_fixture = fixture;
		}

		private const string Stream = nameof(create_duplicate);
		private readonly Fixture _fixture;

		public class Fixture : EventStoreClientFixture {
			protected override Task Given() => Task.CompletedTask;

			protected override Task When() =>
				Client.CreateAsync(Stream, "group32",
					new PersistentSubscriptionSettings(), TestCredentials.Root);
		}

		[Fact]
		public Task the_completion_fails_with_invalid_operation_exception() =>
			Assert.ThrowsAsync<InvalidOperationException>(
				() => _fixture.Client.CreateAsync(Stream, "group32",
					new PersistentSubscriptionSettings(),
					TestCredentials.Root));
	}
}
