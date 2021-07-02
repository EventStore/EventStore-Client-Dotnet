using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Streams;
using Microsoft.Extensions.Logging;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreClient {
		private async Task<StreamMetadataResult> GetStreamMetadataAsync(string streamName,
			EventStoreClientOperationOptions operationOptions, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			ResolvedEvent metadata;

			_log.LogDebug("Read stream metadata for {streamName}.");

			try {
				metadata = await ReadStreamAsync(Direction.Backwards, SystemStreams.MetastreamOf(streamName), StreamPosition.End, 1, operationOptions, false, userCredentials, cancellationToken)
					.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			} catch (StreamNotFoundException) {
				_log.LogWarning("Stream metadata for {streamName} not found.");
				return StreamMetadataResult.None(streamName);
			}

			return metadata.Event == null
				? StreamMetadataResult.None(streamName)
				: StreamMetadataResult.Create(streamName, metadata.OriginalEventNumber,
					JsonSerializer.Deserialize<StreamMetadata>(metadata.Event.Data.Span,
						StreamMetadataJsonSerializerOptions));
		}

		/// <summary>
		/// Asynchronously reads the metadata for a stream
		/// </summary>
		/// <param name="streamName">The name of the stream to read the metadata for.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamMetadataResult> GetStreamMetadataAsync(string streamName,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			var options = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(options);
			return GetStreamMetadataAsync(streamName, options, userCredentials, cancellationToken);
		}

		private ValueTask<IWriteResult> SetStreamMetadataAsync(string streamName, StreamState expectedState,
			StreamMetadata metadata, EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default)
			=> SetStreamMetadataInternal(metadata, new AppendReq {
				Options = new AppendReq.Types.Options {
					StreamIdentifier = SystemStreams.MetastreamOf(streamName)
				}
			}.WithAnyStreamRevision(expectedState), operationOptions, userCredentials, cancellationToken);

		/// <summary>
		/// Asynchronously sets the metadata for a stream.
		/// </summary>
		/// <param name="streamName">The name of the stream to set metadata for.</param>
		/// <param name="expectedState">The <see cref="StreamState"/> of the stream to append to.</param>
		/// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<IWriteResult> SetStreamMetadataAsync(string streamName, StreamState expectedState,
			StreamMetadata metadata, Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var options = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(options);
			return SetStreamMetadataAsync(streamName, expectedState, metadata, options,
				userCredentials, cancellationToken).AsTask();
		}

		private ValueTask<IWriteResult> SetStreamMetadataAsync(string streamName, StreamRevision expectedRevision,
			StreamMetadata metadata, EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default)
			=> SetStreamMetadataInternal(metadata, new AppendReq {
				Options = new AppendReq.Types.Options {
					StreamIdentifier = SystemStreams.MetastreamOf(streamName),
					Revision = expectedRevision
				}
			}, operationOptions, userCredentials, cancellationToken);

		/// <summary>
		/// Asynchronously sets the metadata for a stream.
		/// </summary>
		/// <param name="streamName">The name of the stream to set metadata for.</param>
		/// <param name="expectedRevision">The <see cref="StreamRevision"/> of the stream to append to.</param>
		/// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<IWriteResult> SetStreamMetadataAsync(string streamName, StreamRevision expectedRevision,
			StreamMetadata metadata, Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var options = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(options);
			return SetStreamMetadataAsync(streamName, expectedRevision, metadata, options,
				userCredentials, cancellationToken).AsTask();
		}

		private ValueTask<IWriteResult> SetStreamMetadataInternal(StreamMetadata metadata,
			AppendReq appendReq,
			EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials,
			CancellationToken cancellationToken) =>
			AppendToStreamInternal(appendReq, new[] {
				new EventData(Uuid.NewUuid(), SystemEventTypes.StreamMetadata,
					JsonSerializer.SerializeToUtf8Bytes(metadata, StreamMetadataJsonSerializerOptions)),
			}, operationOptions, userCredentials, cancellationToken);
	}
}
