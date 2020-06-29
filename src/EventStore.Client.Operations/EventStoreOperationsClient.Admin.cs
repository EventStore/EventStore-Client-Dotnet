using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Operations;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreOperationsClient {
		private static readonly Empty EmptyResult = new Empty();

		public async Task ShutdownAsync(
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			await _client.ShutdownAsync(EmptyResult,
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
		}

		public async Task MergeIndexesAsync(
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			await _client.MergeIndexesAsync(EmptyResult,
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
		}

		public async Task ResignNodeAsync(
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			await _client.ResignNodeAsync(EmptyResult,
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
		}

		public async Task SetNodePriorityAsync(int nodePriority,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			await _client.SetNodePriorityAsync(new SetNodePriorityReq {Priority = nodePriority},
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
		}
	}
}
