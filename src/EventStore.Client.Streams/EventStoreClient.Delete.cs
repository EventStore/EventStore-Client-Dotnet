using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Streams;
using Microsoft.Extensions.Logging;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreClient {
		/// <summary>
		/// Soft Deletes a stream asynchronously.
		/// </summary>
		/// <param name="streamName">The name of the stream to delete.</param>
		/// <param name="expectedRevision">The expected <see cref="StreamRevision"/> of the stream being deleted.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<DeleteResult> SoftDeleteAsync(
			string streamName,
			StreamRevision expectedRevision,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {

			var operationOptions = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(operationOptions);

			return SoftDeleteAsync(streamName, expectedRevision, operationOptions, userCredentials, cancellationToken);
		}

		/// <summary>
		/// Soft Deletes a stream asynchronously.
		/// </summary>
		/// <param name="streamName">The name of the stream to delete.</param>
		/// <param name="expectedState">The expected <see cref="StreamState"/> of the stream being deleted.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<DeleteResult> SoftDeleteAsync(
			string streamName,
			StreamState expectedState,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			
			var options = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(options);

			return SoftDeleteAsync(streamName, expectedState, options, userCredentials, cancellationToken);
		}

		private Task<DeleteResult> SoftDeleteAsync(
			string streamName,
			StreamState expectedState,
			EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			DeleteInternal(new DeleteReq {
				Options = new DeleteReq.Types.Options {
					StreamIdentifier = streamName
				}
			}.WithAnyStreamRevision(expectedState), operationOptions, userCredentials, cancellationToken);

		private Task<DeleteResult> SoftDeleteAsync(
			string streamName,
			StreamRevision expectedRevision,
			EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) =>
			DeleteInternal(new DeleteReq {
				Options = new DeleteReq.Types.Options {
					StreamIdentifier = streamName,
					Revision = expectedRevision
				}
			}, operationOptions, userCredentials, cancellationToken);

		private async Task<DeleteResult> DeleteInternal(DeleteReq request,
			EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials,
			CancellationToken cancellationToken) {
			_log.LogDebug("Deleting stream {streamName}.", request.Options.StreamIdentifier);
			var result = await _client.DeleteAsync(request,
				EventStoreCallOptions.Create(Settings, operationOptions, userCredentials, cancellationToken));

			return new DeleteResult(new Position(result.Position.CommitPosition, result.Position.PreparePosition));
		}
	}
}
