using System;
using System.Threading.Tasks;

namespace EventStore.Client {
	public static class EventStorePersistentSubscriptionsClientExtensions {
		public static async Task WarmUpAsync(this EventStorePersistentSubscriptionsClient self) {
			await self. WarmUpWith(async cancellationToken => {
				var id = Guid.NewGuid();
				await self.CreateToStreamAsync(
						streamName: $"warmup-stream-{id}",
						groupName: $"warmup-group-{id}",
						settings: new(),
						userCredentials: TestCredentials.Root,
						cancellationToken: cancellationToken);
			});
		}
	}
}
