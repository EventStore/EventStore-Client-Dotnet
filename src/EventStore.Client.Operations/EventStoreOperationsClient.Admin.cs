using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Operations;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreOperationsClient {
		private static readonly Empty EmptyResult = new Empty();

		/// <summary>
		/// Shuts down the EventStoreDB node.
		/// </summary>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task ShutdownAsync(
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			await _client.ShutdownAsync(EmptyResult,
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
		}

		/// <summary>
		/// Initiates an index merge operation.
		/// </summary>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task MergeIndexesAsync(
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			await _client.MergeIndexesAsync(EmptyResult,
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
		}

		/// <summary>
		/// Resigns a node.
		/// </summary>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task ResignNodeAsync(
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			await _client.ResignNodeAsync(EmptyResult,
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
		}

		/// <summary>
		/// Sets the node priority.
		/// </summary>
		/// <param name="nodePriority"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task SetNodePriorityAsync(int nodePriority,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			await _client.SetNodePriorityAsync(new SetNodePriorityReq {Priority = nodePriority},
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
		}
	}
}
