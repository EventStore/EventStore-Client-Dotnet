using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Streams;
using Microsoft.Extensions.Logging;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreClient {
		private Task<DeleteResult> TombstoneAsync(
			string streamName,
			StreamRevision expectedRevision,
			EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			TombstoneInternal(new TombstoneReq {
				Options = new TombstoneReq.Types.Options {
					StreamIdentifier = streamName,
					Revision = expectedRevision
				}
			}, operationOptions, userCredentials, cancellationToken);

		/// <summary>
		/// Tombstones a stream asynchronously. Note: Tombstoned streams can never be recreated.
		/// </summary>
		/// <param name="streamName">The name of the stream to tombstone.</param>
		/// <param name="expectedRevision">The expected <see cref="StreamRevision"/> of the stream being deleted.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<DeleteResult> TombstoneAsync(
			string streamName,
			StreamRevision expectedRevision,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {

			var operationOptions = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(operationOptions);
			
			return TombstoneAsync(streamName, expectedRevision, operationOptions, userCredentials, cancellationToken);
		}

		private Task<DeleteResult> TombstoneAsync(
			string streamName,
			StreamState expectedState,
			EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			TombstoneInternal(new TombstoneReq {
				Options = new TombstoneReq.Types.Options {
					StreamIdentifier = streamName
				}
			}.WithAnyStreamRevision(expectedState), operationOptions, userCredentials, cancellationToken);

		/// <summary>
		/// Tombstones a stream asynchronously. Note: Tombstoned streams can never be recreated.
		/// </summary>
		/// <param name="streamName">The name of the stream to tombstone.</param>
		/// <param name="expectedState">The expected <see cref="StreamState"/> of the stream being deleted.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<DeleteResult> TombstoneAsync(
			string streamName,
			StreamState expectedState,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {

			var operationOptions = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(operationOptions);
			
			return TombstoneAsync(streamName, expectedState, operationOptions, userCredentials, cancellationToken);
		}

		private async Task<DeleteResult> TombstoneInternal(TombstoneReq request,
			EventStoreClientOperationOptions operationOptions, UserCredentials? userCredentials,
			CancellationToken cancellationToken) {
			_log.LogDebug("Tombstoning stream {streamName}.", request.Options.StreamIdentifier);

			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Streams.Streams.StreamsClient(
				channelInfo.CallInvoker).TombstoneAsync(request,
				EventStoreCallOptions.Create(Settings, operationOptions, userCredentials, cancellationToken));
			var result = await call.ResponseAsync.ConfigureAwait(false);

			return new DeleteResult(new Position(result.Position.CommitPosition, result.Position.PreparePosition));
		}
	}
}
