using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Streams;
using Microsoft.Extensions.Logging;

namespace EventStore.Client {
	public partial class KurrentClient {
		/// <summary>
		/// Asynchronously reads the metadata for a stream
		/// </summary>
		/// <param name="streamName">The name of the stream to read the metadata for.</param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public async Task<StreamMetadataResult> GetStreamMetadataAsync(string streamName, TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			_log.LogDebug("Read stream metadata for {streamName}.", streamName);

			try {
				var result = ReadStreamAsync(Direction.Backwards, SystemStreams.MetastreamOf(streamName),
					StreamPosition.End, 1, false, deadline, userCredentials, cancellationToken);
				await foreach (var message in result.Messages.ConfigureAwait(false)) {
					if (message is not StreamMessage.Event(var resolvedEvent)) {
						continue;
					}

					return StreamMetadataResult.Create(streamName, resolvedEvent.OriginalEventNumber,
						JsonSerializer.Deserialize<StreamMetadata>(resolvedEvent.Event.Data.Span,
							StreamMetadataJsonSerializerOptions));
				}

			} catch (StreamNotFoundException) {
			}
			_log.LogWarning("Stream metadata for {streamName} not found.", streamName);
			return StreamMetadataResult.None(streamName);
		}

		/// <summary>
		/// Asynchronously sets the metadata for a stream.
		/// </summary>
		/// <param name="streamName">The name of the stream to set metadata for.</param>
		/// <param name="expectedState">The <see cref="StreamState"/> of the stream to append to.</param>
		/// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{KurrentClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<IWriteResult> SetStreamMetadataAsync(string streamName, StreamState expectedState,
			StreamMetadata metadata, Action<KurrentClientOperationOptions>? configureOperationOptions = null,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var options = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(options);

			return SetStreamMetadataInternal(metadata, new AppendReq {
				Options = new AppendReq.Types.Options {
					StreamIdentifier = SystemStreams.MetastreamOf(streamName)
				}
			}.WithAnyStreamRevision(expectedState), options, deadline, userCredentials, cancellationToken);
		}

		/// <summary>
		/// Asynchronously sets the metadata for a stream.
		/// </summary>
		/// <param name="streamName">The name of the stream to set metadata for.</param>
		/// <param name="expectedRevision">The <see cref="StreamRevision"/> of the stream to append to.</param>
		/// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{KurrentClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<IWriteResult> SetStreamMetadataAsync(string streamName, StreamRevision expectedRevision,
			StreamMetadata metadata, Action<KurrentClientOperationOptions>? configureOperationOptions = null,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var options = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(options);

			return SetStreamMetadataInternal(metadata, new AppendReq {
				Options = new AppendReq.Types.Options {
					StreamIdentifier = SystemStreams.MetastreamOf(streamName),
					Revision = expectedRevision
				}
			}, options, deadline, userCredentials, cancellationToken);
		}

		private async Task<IWriteResult> SetStreamMetadataInternal(StreamMetadata metadata,
			AppendReq appendReq,
			KurrentClientOperationOptions operationOptions,
			TimeSpan? deadline,
			UserCredentials? userCredentials,
			CancellationToken cancellationToken) {

			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			return await AppendToStreamInternal(channelInfo, appendReq, new[] {
				new EventData(Uuid.NewUuid(), SystemEventTypes.StreamMetadata,
					JsonSerializer.SerializeToUtf8Bytes(metadata, StreamMetadataJsonSerializerOptions)),
			}, operationOptions, deadline, userCredentials, cancellationToken).ConfigureAwait(false);
		}
	}
}
