using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Google.Protobuf;
using EventStore.Client.Streams;
using Grpc.Core;
using Microsoft.Extensions.Logging;
#nullable enable
namespace EventStore.Client {
	public partial class EventStoreClient {
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
		public async Task<IWriteResult> AppendToStreamAsync(
			string streamName,
			StreamRevision expectedRevision,
			IEnumerable<EventData> eventData,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var options = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(options);

			_log.LogDebug("Append to stream - {streamName}@{expectedRevision}.", streamName, expectedRevision);

			var task =
				userCredentials == null
					? _streamAppender.Append(streamName, expectedRevision, eventData, options.TimeoutAfter,
						cancellationToken)
					:
					AppendToStreamInternal(new AppendReq {
						Options = new AppendReq.Types.Options {
							StreamIdentifier = streamName,
							Revision = expectedRevision
						}
					}, eventData, options, userCredentials, cancellationToken);

			return (await task.ConfigureAwait(false)).OptionallyThrowWrongExpectedVersionException(options);
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
		public async Task<IWriteResult> AppendToStreamAsync(
			string streamName,
			StreamState expectedState,
			IEnumerable<EventData> eventData,
			Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var operationOptions = Settings.OperationOptions.Clone();
			configureOperationOptions?.Invoke(operationOptions);

			_log.LogDebug("Append to stream - {streamName}@{expectedRevision}.", streamName, expectedState);

			var task =
				userCredentials == null
					? _streamAppender.Append(streamName, expectedState, eventData, operationOptions.TimeoutAfter,
						cancellationToken)
					:
					AppendToStreamInternal(new AppendReq {
							Options = new AppendReq.Types.Options {
								StreamIdentifier = streamName
							}
						}.WithAnyStreamRevision(expectedState), eventData, operationOptions, userCredentials,
						cancellationToken);
			return (await task.ConfigureAwait(false)).OptionallyThrowWrongExpectedVersionException(operationOptions);
		}

		private async ValueTask<IWriteResult> AppendToStreamInternal(
			AppendReq header,
			IEnumerable<EventData> eventData,
			EventStoreClientOperationOptions operationOptions,
			UserCredentials? userCredentials,
			CancellationToken cancellationToken) {
			using var call = new Streams.Streams.StreamsClient(
				await SelectCallInvoker(cancellationToken).ConfigureAwait(false)).Append(EventStoreCallOptions.Create(
				Settings, operationOptions, userCredentials, cancellationToken));

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
			} finally {
				await call.RequestStream.CompleteAsync().ConfigureAwait(false);

				var response = await call.ResponseAsync.ConfigureAwait(false);

				if (response.Success != null) {
					writeResult = new SuccessResult(response.Success.CurrentRevisionOptionCase ==
					                                AppendResp.Types.Success.CurrentRevisionOptionOneofCase.NoStream
							? StreamRevision.None
							: new StreamRevision(response.Success.CurrentRevision),
						response.Success.PositionOptionCase == AppendResp.Types.Success.PositionOptionOneofCase.Position
							? new Position(response.Success.Position.CommitPosition,
								response.Success.Position.PreparePosition)
							: default);
					_log.LogDebug("Append to stream succeeded - {streamName}@{logPosition}/{nextExpectedVersion}.",
						header.Options.StreamIdentifier, writeResult.LogPosition, writeResult.NextExpectedStreamRevision);
				} else {
					if (response.WrongExpectedVersion != null) {
						var actualStreamRevision = response.WrongExpectedVersion.CurrentRevisionOptionCase switch {
								AppendResp.Types.WrongExpectedVersion.CurrentRevisionOptionOneofCase.CurrentNoStream =>
									StreamRevision.None,
								_ => new StreamRevision(response.WrongExpectedVersion.CurrentRevision)
							};

						_log.LogDebug(
							"Append to stream failed with Wrong Expected Version - {streamName}/{expectedRevision}/{currentRevision}",
							header.Options.StreamIdentifier, new StreamRevision(header.Options.Revision),
							actualStreamRevision);

						if (operationOptions.ThrowOnAppendFailure) {
							if (response.WrongExpectedVersion.ExpectedRevisionOptionCase == AppendResp.Types
								.WrongExpectedVersion.ExpectedRevisionOptionOneofCase.ExpectedRevision) {
								throw new WrongExpectedVersionException(header.Options.StreamIdentifier,
									new StreamRevision(response.WrongExpectedVersion.ExpectedRevision),
									actualStreamRevision);
							}

							var expectedStreamState = response.WrongExpectedVersion.ExpectedRevisionOptionCase switch {
								AppendResp.Types.WrongExpectedVersion.ExpectedRevisionOptionOneofCase.ExpectedAny =>
									StreamState.Any,
								AppendResp.Types.WrongExpectedVersion.ExpectedRevisionOptionOneofCase.ExpectedNoStream =>
									StreamState.NoStream,
								AppendResp.Types.WrongExpectedVersion.ExpectedRevisionOptionOneofCase.ExpectedStreamExists =>
									StreamState.StreamExists,
								_ => StreamState.Any
							};

							throw new WrongExpectedVersionException(header.Options.StreamIdentifier,
								expectedStreamState, actualStreamRevision);
						}

						if (response.WrongExpectedVersion.ExpectedRevisionOptionCase == AppendResp.Types
							.WrongExpectedVersion.ExpectedRevisionOptionOneofCase.ExpectedRevision) {
							writeResult = new WrongExpectedVersionResult(header.Options.StreamIdentifier,
								new StreamRevision(response.WrongExpectedVersion.ExpectedRevision),
								actualStreamRevision);
						} else {
							writeResult = new WrongExpectedVersionResult(header.Options.StreamIdentifier,
								StreamRevision.None,
								actualStreamRevision);
						}

					} else {
						throw new InvalidOperationException("The operation completed with an unexpected result.");
					}
				}
			}

			return writeResult;
		}


		private class StreamAppender : IDisposable {
			private readonly EventStoreClientSettings _settings;
			private readonly CancellationToken _cancellationToken;
			private readonly Action<Exception> _onException;
			private readonly Channel<BatchAppendReq> _channel;
			private readonly ConcurrentDictionary<Uuid, TaskCompletionSource<IWriteResult>> _pendingRequests;

			private readonly Task<AsyncDuplexStreamingCall<BatchAppendReq, BatchAppendResp>> _callTask;

			public StreamAppender(EventStoreClientSettings settings,
				Task<AsyncDuplexStreamingCall<BatchAppendReq, BatchAppendResp>> callTask, CancellationToken cancellationToken,
				Action<Exception> onException) {
				_settings = settings;
				_callTask = callTask;
				_cancellationToken = cancellationToken;
				_onException = onException;
				_channel = System.Threading.Channels.Channel.CreateBounded<BatchAppendReq>(10000);
				_pendingRequests = new ConcurrentDictionary<Uuid, TaskCompletionSource<IWriteResult>>();
				_ = Task.Factory.StartNew(Send, TaskCreationOptions.LongRunning);
				_ = Task.Factory.StartNew(Receive, TaskCreationOptions.LongRunning);
			}

			public ValueTask<IWriteResult> Append(string streamName, StreamRevision expectedStreamPosition,
				IEnumerable<EventData> events, TimeSpan? timeoutAfter, CancellationToken cancellationToken = default) =>
				AppendInternal(BatchAppendReq.Types.Options.Create(streamName, expectedStreamPosition, timeoutAfter),
					events, cancellationToken);

			public ValueTask<IWriteResult> Append(string streamName, StreamState expectedStreamState,
				IEnumerable<EventData> events, TimeSpan? timeoutAfter, CancellationToken cancellationToken = default) =>
				AppendInternal(BatchAppendReq.Types.Options.Create(streamName, expectedStreamState, timeoutAfter),
					events, cancellationToken);

			private async Task Receive() {
				var call = await _callTask.ConfigureAwait(false);
				try {
					await foreach (var response in call.ResponseStream.ReadAllAsync(_cancellationToken)
						.ConfigureAwait(false)) {
						if (!_pendingRequests.TryRemove(Uuid.FromDto(response.CorrelationId), out var writeResult)) {
							continue; // TODO: Log?
						}

						try {
							writeResult.TrySetResult(response.ToWriteResult());
						} catch (Exception ex) when (ex is not RpcException) {
							writeResult.TrySetException(ex);
						}
					}
				} catch (Exception ex) {
					_onException(ex);
					foreach (var (_, source) in _pendingRequests) {
						source.TrySetException(ex);
					}
				}
			}

			private async Task Send() {
				var call = await _callTask.ConfigureAwait(false);

				await foreach (var appendRequest in _channel.Reader.ReadAllAsync(_cancellationToken)
					.ConfigureAwait(false)) {
					await call.RequestStream.WriteAsync(appendRequest).ConfigureAwait(false);
				}
			}

			private async ValueTask<IWriteResult> AppendInternal(BatchAppendReq.Types.Options options,
				IEnumerable<EventData> events, CancellationToken cancellationToken) {
				var batchSize = 0;
				var correlationId = Uuid.NewUuid();
				var correlationIdDto = correlationId.ToDto();

				var complete = _pendingRequests.GetOrAdd(correlationId, new TaskCompletionSource<IWriteResult>());

				foreach (var appendRequest in GetRequests()) {
					await _channel.Writer.WriteAsync(appendRequest, cancellationToken).ConfigureAwait(false);
				}

				return await complete.Task.ConfigureAwait(false);

				IEnumerable<BatchAppendReq> GetRequests() {
					bool first = true;
					var proposedMessages = new List<BatchAppendReq.Types.ProposedMessage>();
					foreach (var @event in events) {
						var proposedMessage = new BatchAppendReq.Types.ProposedMessage {
							Data = ByteString.CopyFrom(@event.Data.Span),
							CustomMetadata = ByteString.CopyFrom(@event.Metadata.Span),
							Id = @event.EventId.ToDto(),
							Metadata = {
								{Constants.Metadata.Type, @event.Type},
								{Constants.Metadata.ContentType, @event.ContentType}
							}
						};

						proposedMessages.Add(proposedMessage);

						if ((batchSize += proposedMessage.CalculateSize()) <
						    _settings.OperationOptions.BatchAppendSize) {
							continue;
						}

						yield return new BatchAppendReq {
							ProposedMessages = {proposedMessages},
							CorrelationId = correlationIdDto,
							Options = first ? options : null
						};
						first = false;
						proposedMessages.Clear();
						batchSize = 0;
					}

					yield return new BatchAppendReq {
						ProposedMessages = {proposedMessages},
						IsFinal = true,
						CorrelationId = correlationIdDto,
						Options = first ? options : null
					};
				}
			}

			public void Dispose() {
				_channel.Writer.TryComplete();
			}
		}
	}
}
