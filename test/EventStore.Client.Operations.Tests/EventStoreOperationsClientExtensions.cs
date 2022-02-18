using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public static class EventStoreOperationsClientExtensions {
		public static async Task WarmUpAsync(this EventStoreOperationsClient self) {
			await self.WarmUpWith(async cancellationToken => {
				await self.RestartPersistentSubscriptions(userCredentials: TestCredentials.Root,
					cancellationToken: cancellationToken);
			});
		}
	}
}
