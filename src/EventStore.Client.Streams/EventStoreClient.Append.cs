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
		private Task<IWriteResult> AppendToStreamAsync(
			string streamName,
			StreamRevision expectedRevision,
			IEnumerable<EventData> eventData,
			EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			_log.LogDebug("Append to stream - {streamName}@{expectedRevision}.", streamName, expectedRevision);

			return AppendToStreamInternal(new AppendReq {
				Options = new AppendReq.Types.Options {
					StreamIdentifier = streamName,
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
		public Task<IWriteResult> AppendToStreamAsync(
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

		private Task<IWriteResult> AppendToStreamAsync(
			string streamName,
			StreamState expectedState,
			IEnumerable<EventData> eventData,
			EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			_log.LogDebug("Append to stream - {streamName}@{expectedRevision}.", streamName, expectedState);

			return AppendToStreamInternal(new AppendReq {
				Options = new AppendReq.Types.Options {
					StreamIdentifier = streamName
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
		public Task<IWriteResult> AppendToStreamAsync(
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

		private async Task<IWriteResult> AppendToStreamInternal(
			AppendReq header,
			IEnumerable<EventData> eventData,
			EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials,
			CancellationToken cancellationToken) {
			using var call = _client.Append(RequestMetadata.Create(userCredentials ?? Settings.DefaultCredentials),
				DeadLine.After(operationOptions.TimeoutAfter), cancellationToken);

			IWriteResult writeResult;
			try {
				await call.RequestStream.WriteAsync(header).ConfigureAwait(false);

				foreach (var e in eventData) {
					_log.LogTrace("Appending event to stream - {streamName}@{eventId} {eventType}.",
						header.Options.StreamIdentifier, e.EventId, e.Type);
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

				if (response.Success != null) {
					writeResult = new SuccessResult(
						response.Success.CurrentRevisionOptionCase ==
						AppendResp.Types.Success.CurrentRevisionOptionOneofCase.NoStream
							? StreamState.NoStream.ToInt64()
							: new StreamRevision(response.Success.CurrentRevision).ToInt64(),
						response.Success.PositionOptionCase == AppendResp.Types.Success.PositionOptionOneofCase.Position
							? new Position(response.Success.Position.CommitPosition,
								response.Success.Position.PreparePosition)
							: default);
					_log.LogDebug("Append to stream succeeded - {streamName}@{logPosition}/{nextExpectedVersion}.",
						header.Options.StreamIdentifier, writeResult.LogPosition, writeResult.NextExpectedVersion);
				} else {
					if (response.WrongExpectedVersion != null) {
						var expectedRevision = response.WrongExpectedVersion.ExpectedRevisionOptionCase switch {
							AppendResp.Types.WrongExpectedVersion.ExpectedRevisionOptionOneofCase.Any =>
							StreamState.Any.ToInt64(),
							AppendResp.Types.WrongExpectedVersion.ExpectedRevisionOptionOneofCase.StreamExists =>
							StreamState.StreamExists.ToInt64(),
							_ => new StreamRevision(response.WrongExpectedVersion.ExpectedRevision).ToInt64()
						};

						var currentRevision = response.WrongExpectedVersion.CurrentRevisionOptionCase switch {
							AppendResp.Types.WrongExpectedVersion.CurrentRevisionOptionOneofCase.NoStream =>
							StreamState.NoStream.ToInt64(),
							_ => new StreamRevision(response.WrongExpectedVersion.CurrentRevision).ToInt64()
						};

						_log.LogDebug(
							"Append to stream failed with Wrong Expected Version - {streamName}/{expectedRevision}/{currentRevision}",
							header.Options.StreamIdentifier, expectedRevision, currentRevision);
						
						if (operationOptions.ThrowOnAppendFailure) {
							throw new WrongExpectedVersionException(header.Options.StreamIdentifier, expectedRevision,
								currentRevision);
						}

						writeResult = new WrongExpectedVersionResult(
							header.Options.StreamIdentifier, expectedRevision, currentRevision);
					} else {
						throw new InvalidOperationException("The operation completed with an unexpected result.");
					}
				}
			}

			return writeResult;
		}
	}
}
