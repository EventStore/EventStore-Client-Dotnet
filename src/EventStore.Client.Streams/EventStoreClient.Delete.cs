using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Streams;
using Microsoft.Extensions.Logging;

namespace EventStore.Client {
	public partial class EventStoreClient {
		/// <summary>
		/// Deletes a stream asynchronously.
		/// </summary>
		/// <param name="streamName">The name of the stream to delete.</param>
		/// <param name="expectedRevision">The expected <see cref="StreamRevision"/> of the stream being deleted.</param>
		/// <param name="deadline">The maximum time to wait before terminating the call.</param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="userCertificate">The optional <see cref="UserCertificate"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<DeleteResult> DeleteAsync(
			string streamName,
			StreamRevision expectedRevision,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			UserCertificate? userCertificate = null,
			CancellationToken cancellationToken = default) =>
			DeleteInternal(new DeleteReq {
				Options = new DeleteReq.Types.Options {
					StreamIdentifier = streamName,
					Revision = expectedRevision
				}
				},
				deadline,
				userCredentials,
				userCertificate,
				cancellationToken
			);

		/// <summary>
		/// Deletes a stream asynchronously.
		/// </summary>
		/// <param name="streamName">The name of the stream to delete.</param>
		/// <param name="expectedState">The expected <see cref="StreamState"/> of the stream being deleted.</param>
		/// <param name="deadline">The maximum time to wait before terminating the call.</param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="userCertificate">The optional <see cref="UserCertificate"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<DeleteResult> DeleteAsync(
			string streamName,
			StreamState expectedState,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			UserCertificate? userCertificate = null,
			CancellationToken cancellationToken = default) => DeleteInternal(new DeleteReq {
				Options = new DeleteReq.Types.Options {
					StreamIdentifier = streamName
				}
			}.WithAnyStreamRevision(expectedState),
			deadline,
			userCredentials,
			userCertificate,
			cancellationToken
		);

		private async Task<DeleteResult> DeleteInternal(DeleteReq request,
			TimeSpan? deadline,
			UserCredentials? userCredentials,
			UserCertificate? userCertificate,
			CancellationToken cancellationToken) {
			_log.LogDebug("Deleting stream {streamName}.", request.Options.StreamIdentifier);
			var channelInfo = await GetChannelInfo(userCertificate?.Certificate, cancellationToken).ConfigureAwait(false);
			using var call = new Streams.Streams.StreamsClient(
				channelInfo.CallInvoker).DeleteAsync(request,
				EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			var result = await call.ResponseAsync.ConfigureAwait(false);

			return new DeleteResult(new Position(result.Position.CommitPosition, result.Position.PreparePosition));
		}
	}
}
