using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Streams;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreClient {
		private Task<WriteResult> AppendToStreamAsync(
			string streamName,
			StreamRevision expectedRevision,
			IEnumerable<EventData> eventData,
			EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			_log.LogDebug("Append to stream - {streamName}@{expectedRevision}.", streamName, expectedRevision);

			return AppendToStreamInternal(new AppendReq {
				Options = new AppendReq.Types.Options {
					StreamName = streamName,
					Revision = expectedRevision
				}
			}, eventData, operationOptions, userCredentials, cancellationToken);
		}

		/// <summary>
		/// Appends events asynchronously to a stream.
		/// </summary>
		/// <param name="streamName">The name of the stream to append events to.</param>
		/// <param name="expectedRevision">The expected <see cref="StreamRevision"/> of the stream to append to.</param>
		/// <param name="eventData">An <see cref="IEnumerable{EventData}"/> to append to the stream.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="userCredentials">The <see cref="UserCredentials"/> for the operation.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<WriteResult> AppendToStreamAsync(
			string streamName,
			StreamRevision expectedRevision,
			IEnumerable<EventData> eventData,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var options = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(options);

			return AppendToStreamAsync(streamName, expectedRevision, eventData, options, userCredentials,
				cancellationToken);
		}

		private Task<WriteResult> AppendToStreamAsync(
			string streamName,
			StreamState expectedState,
			IEnumerable<EventData> eventData,
			EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			_log.LogDebug("Append to stream - {streamName}@{expectedRevision}.", streamName, expectedState);

			return AppendToStreamInternal(new AppendReq {
				Options = new AppendReq.Types.Options {
					StreamName = streamName
				}
			}.WithAnyStreamRevision(expectedState), eventData, operationOptions, userCredentials, cancellationToken);
		}

		/// <summary>
		/// Appends events asynchronously to a stream.
		/// </summary>
		/// <param name="streamName">The name of the stream to append events to.</param>
		/// <param name="expectedState">The expected <see cref="StreamState"/> of the stream to append to.</param>
		/// <param name="eventData">An <see cref="IEnumerable{EventData}"/> to append to the stream.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="userCredentials">The <see cref="UserCredentials"/> for the operation.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<WriteResult> AppendToStreamAsync(
			string streamName,
			StreamState expectedState,
			IEnumerable<EventData> eventData,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var operationOptions = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(operationOptions);

			return AppendToStreamAsync(streamName, expectedState, eventData, operationOptions, userCredentials,
				cancellationToken);
		}

		private async Task<WriteResult> AppendToStreamInternal(
			AppendReq header,
			IEnumerable<EventData> eventData,
			EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials,
			CancellationToken cancellationToken) {
			using var call = _client.Append(RequestMetadata.Create(userCredentials),
				DeadLine.After(operationOptions.TimeoutAfter), cancellationToken);

			WriteResult writeResult;
			try {
				await call.RequestStream.WriteAsync(header).ConfigureAwait(false);

				foreach (var e in eventData) {
					_log.LogTrace("Appending event to stream - {streamName}@{eventId} {eventType}.",
						header.Options.StreamName, e.EventId, e.Type);
					await call.RequestStream.WriteAsync(new AppendReq {
						ProposedMessage = new AppendReq.Types.ProposedMessage {
							Id = e.EventId.ToDto(),
							Data = ByteString.CopyFrom(e.Data.Span),
							CustomMetadata = ByteString.CopyFrom(e.Metadata.Span),
							Metadata = {
								{Constants.Metadata.Type, e.Type},
								{Constants.Metadata.ContentType, e.ContentType}
							}
						}
					}).ConfigureAwait(false);
				}

				await call.RequestStream.CompleteAsync().ConfigureAwait(false);
			} finally {
				var response = await call.ResponseAsync.ConfigureAwait(false);

				writeResult = new WriteResult(
					response.CurrentRevisionOptionCase == AppendResp.CurrentRevisionOptionOneofCase.NoStream
						? StreamState.NoStream.ToInt64()
						: new StreamRevision(response.CurrentRevision).ToInt64(),
					response.PositionOptionCase == AppendResp.PositionOptionOneofCase.Position
						? new Position(response.Position.CommitPosition, response.Position.PreparePosition)
						: default);

				_log.LogDebug("Append to stream succeeded - {streamName}@{logPosition}/{nextExpectedVersion}.",
					header.Options.StreamName, writeResult.LogPosition, writeResult.NextExpectedVersion);
			}
			return writeResult;
		}
	}
}
