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
			var (channel, _) = await GetCurrentChannelInfo().ConfigureAwait(false);
			using var call = new Operations.Operations.OperationsClient(
				CreateCallInvoker(channel)).ShutdownAsync(EmptyResult,
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
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
			var (channel, _) = await GetCurrentChannelInfo().ConfigureAwait(false);
			using var call = new Operations.Operations.OperationsClient(
				CreateCallInvoker(channel)).MergeIndexesAsync(EmptyResult,
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
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
			var (channel, _) = await GetCurrentChannelInfo().ConfigureAwait(false);
			using var call = new Operations.Operations.OperationsClient(
				CreateCallInvoker(channel)).ResignNodeAsync(EmptyResult,
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
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
			var (channel, _) = await GetCurrentChannelInfo().ConfigureAwait(false);
			using var call = new Operations.Operations.OperationsClient(
				CreateCallInvoker(channel)).SetNodePriorityAsync(
				new SetNodePriorityReq {Priority = nodePriority},
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Restart persistent subscriptions
		/// </summary>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task RestartPersistentSubscriptions(UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var (channel, _) = await GetCurrentChannelInfo().ConfigureAwait(false);
			using var call = new Operations.Operations.OperationsClient(
				CreateCallInvoker(channel)).RestartPersistentSubscriptionsAsync(
				EmptyResult,
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}
	}
}
