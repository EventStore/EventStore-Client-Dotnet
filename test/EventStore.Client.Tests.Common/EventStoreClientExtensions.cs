using System;
using System.Linq;
using System.Threading.Tasks;
using Polly;

#nullable enable
namespace EventStore.Client {
	public static class EventStoreClientExtensions {
		public static async Task WarmUpAsync(this EventStoreClient self) {
			// if we can read from $users then we know that 1. the users exist
			// and 2. we are connected to leader if we require it
			await Policy.Handle<Exception>()
				.WaitAndRetryAsync(10, retryCount => TimeSpan.FromSeconds(2))
				.ExecuteAsync(async () => {
					var users = await self
						.ReadStreamAsync(
							Direction.Forwards,
							"$users",
							StreamPosition.Start,
							userCredentials: TestCredentials.Root)
						.ToArrayAsync();

					if (users.Length == 0)
						throw new Exception("no users yet");

					// the read from leader above is not enough to guarantee the next write goes to leader
					await self.AppendToStreamAsync($"warmup", StreamState.Any, Enumerable.Empty<EventData>());
				});
		}
	}
}
