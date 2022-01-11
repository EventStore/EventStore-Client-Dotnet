using System;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public static class EventStorePersistentSubscriptionsClientExtensions {
		public static async Task WarmUpAsync(this EventStorePersistentSubscriptionsClient self) {
			await self.WarmUpWith(async cancellationToken => {
				await self.CreateToAllAsync(
						groupName: $"warmup-group-{Guid.NewGuid()}",
						settings: new(),
						userCredentials: TestCredentials.Root,
						cancellationToken: cancellationToken);
			});
		}
	}
}
