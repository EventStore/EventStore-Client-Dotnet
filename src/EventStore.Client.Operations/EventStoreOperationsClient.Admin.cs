using EventStore.Client.Operations;
using static EventStore.Client.Operations.Operations;

namespace EventStore.Client;

public partial class EventStoreOperationsClient {
	static readonly Empty EmptyResult = new();

	/// <summary>
	/// Shuts down the EventStoreDB node.
	/// </summary>
	/// <param name="deadline"></param>
	/// <param name="userCredentials"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task ShutdownAsync(
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) {
		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
		using var call = new OperationsClient(channelInfo.CallInvoker).ShutdownAsync(
			EmptyResult,
			EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken)
		);

		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Initiates an index merge operation.
	/// </summary>
	/// <param name="deadline"></param>
	/// <param name="userCredentials"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task MergeIndexesAsync(
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) {
		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
		using var call = new OperationsClient(channelInfo.CallInvoker).MergeIndexesAsync(
			EmptyResult,
			EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken)
		);

		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Resigns a node.
	/// </summary>
	/// <param name="deadline"></param>
	/// <param name="userCredentials"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task ResignNodeAsync(
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) {
		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
		using var call = new OperationsClient(channelInfo.CallInvoker).ResignNodeAsync(
			EmptyResult,
			EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken)
		);

		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Sets the node priority.
	/// </summary>
	/// <param name="nodePriority"></param>
	/// <param name="deadline"></param>
	/// <param name="userCredentials"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task SetNodePriorityAsync(
		int nodePriority,
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) {
		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
		using var call = new OperationsClient(channelInfo.CallInvoker).SetNodePriorityAsync(
			new SetNodePriorityReq { Priority = nodePriority },
			EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken)
		);

		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Restart persistent subscriptions
	/// </summary>
	/// <param name="deadline"></param>
	/// <param name="userCredentials"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task RestartPersistentSubscriptions(
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) {
		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
		using var call = new OperationsClient(channelInfo.CallInvoker).RestartPersistentSubscriptionsAsync(
			EmptyResult,
			EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken)
		);

		await call.ResponseAsync.ConfigureAwait(false);
	}
}