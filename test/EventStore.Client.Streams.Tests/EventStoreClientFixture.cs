using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Polly;

#nullable enable
namespace EventStore.Client {
	public abstract class EventStoreClientFixture : EventStoreClientFixtureBase {
		public EventStoreClient Client { get; }
		protected EventStoreClientFixture(EventStoreClientSettings? settings = null,
			IDictionary<string, string>? env = null) : base(settings, env) {
			Client = new EventStoreClient(Settings);
		}

		//qq awkward callback perhaps there is a better way
		protected override async Task ConnectAsync() {
			await Policy.Handle<Exception>()
				.WaitAndRetryAsync(5, retryCount => TimeSpan.FromSeconds(retryCount * retryCount))
				.ExecuteAsync(async () => {
					var userCount = await Client.ReadStreamAsync(Direction.Forwards, "$users", StreamPosition.Start, userCredentials: TestCredentials.Root).CountAsync();
					if (userCount == 0) {
						throw new Exception();
					}
				});
		}

		public override async Task DisposeAsync() {
			await Client.DisposeAsync();
			await base.DisposeAsync();
		}
	}
}
