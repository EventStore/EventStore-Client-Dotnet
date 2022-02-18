using System.Linq;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public static class EventStoreProjectionManagementClientExtensions {
		public static async Task WarmUpAsync(this EventStoreProjectionManagementClient self) {
			await self.WarmUpWith(async cancellationToken => {
				await self.ListAllAsync(userCredentials: TestCredentials.Root, cancellationToken: cancellationToken)
					.ToArrayAsync(cancellationToken);
			});
		}
	}
}
