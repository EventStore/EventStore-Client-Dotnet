using EventStore.Client.Streams;
using Microsoft.Extensions.Logging;

namespace EventStore.Client;

public partial class EventStoreClient {
	/// <summary>
	/// Tombstones a stream asynchronously. Note: Tombstoned streams can never be recreated.
	/// </summary>
	/// <param name="streamName">The name of the stream to tombstone.</param>
	/// <param name="expectedRevision">The expected <see cref="StreamRevision"/> of the stream being deleted.</param>
	/// <param name="deadline"></param>
	/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
	/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
	/// <returns></returns>
	public Task<DeleteResult> TombstoneAsync(
		string streamName,
		StreamRevision expectedRevision,
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) => TombstoneInternal(
		new TombstoneReq {
			Options = new TombstoneReq.Types.Options {
				StreamIdentifier = streamName,
				Revision         = expectedRevision
			}
		}, deadline, userCredentials, cancellationToken
	);

	/// <summary>
	/// Tombstones a stream asynchronously. Note: Tombstoned streams can never be recreated.
	/// </summary>
	/// <param name="streamName">The name of the stream to tombstone.</param>
	/// <param name="expectedState">The expected <see cref="StreamState"/> of the stream being deleted.</param>
	/// <param name="deadline"></param>
	/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
	/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
	/// <returns></returns>
	public Task<DeleteResult> TombstoneAsync(
		string streamName,
		StreamState expectedState,
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) => TombstoneInternal(
		new TombstoneReq {
			Options = new TombstoneReq.Types.Options {
				StreamIdentifier = streamName
			}
		}.WithAnyStreamRevision(expectedState), deadline, userCredentials, cancellationToken
	);

	async Task<DeleteResult> TombstoneInternal(
		TombstoneReq request, TimeSpan? deadline,
		UserCredentials? userCredentials, CancellationToken cancellationToken
	) {
		_log.LogDebug("Tombstoning stream {streamName}.", request.Options.StreamIdentifier);

		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
		using var call = new Streams.Streams.StreamsClient(channelInfo.CallInvoker).TombstoneAsync(
			request,
			EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken)
		);

		var result = await call.ResponseAsync.ConfigureAwait(false);

		return new DeleteResult(new Position(result.Position.CommitPosition, result.Position.PreparePosition));
	}
}