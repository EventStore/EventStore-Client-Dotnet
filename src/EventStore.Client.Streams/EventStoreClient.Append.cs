using System.Collections.Concurrent;
using System.Threading.Channels;
using EventStore.Client.Streams;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using static EventStore.Client.Streams.AppendResp.Types.WrongExpectedVersion;

namespace EventStore.Client;

public partial class EventStoreClient {
	/// <summary>
	/// Appends events asynchronously to a stream.
	/// </summary>
	/// <param name="streamName">The name of the stream to append events to.</param>
	/// <param name="expectedRevision">The expected <see cref="StreamRevision"/> of the stream to append to.</param>
	/// <param name="eventData">An <see cref="IEnumerable{EventData}"/> to append to the stream.</param>
	/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
	/// <param name="deadline"></param>
	/// <param name="userCredentials">The <see cref="UserCredentials"/> for the operation.</param>
	/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
	/// <returns></returns>
	public async Task<IWriteResult> AppendToStreamAsync(
		string streamName,
		StreamRevision expectedRevision,
		IEnumerable<EventData> eventData,
		Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) {
		var options = Settings.OperationOptions.Clone();
		configureOperationOptions?.Invoke(options);

		_log.LogDebug("Append to stream - {streamName}@{expectedRevision}.", streamName, expectedRevision);

		var batchAppender = _streamAppender;
		var task = userCredentials == null && await batchAppender.IsUsable().ConfigureAwait(false)
			? batchAppender.Append(streamName, expectedRevision, eventData, deadline, cancellationToken)
			: AppendToStreamInternal(
				(await GetChannelInfo(cancellationToken).ConfigureAwait(false)).CallInvoker,
				new AppendReq {
					Options = new AppendReq.Types.Options {
						StreamIdentifier = streamName,
						Revision         = expectedRevision
					}
				}, eventData, options, deadline, userCredentials, cancellationToken
			);

		return (await task.ConfigureAwait(false)).OptionallyThrowWrongExpectedVersionException(options);
	}

	/// <summary>
	/// Appends events asynchronously to a stream.
	/// </summary>
	/// <param name="streamName">The name of the stream to append events to.</param>
	/// <param name="expectedState">The expected <see cref="StreamState"/> of the stream to append to.</param>
	/// <param name="eventData">An <see cref="IEnumerable{EventData}"/> to append to the stream.</param>
	/// <param name="configureOperationOptions">An <see cref="Action{EventStoreClientOperationOptions}"/> to configure the operation's options.</param>
	/// <param name="deadline"></param>
	/// <param name="userCredentials">The <see cref="UserCredentials"/> for the operation.</param>
	/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
	/// <returns></returns>
	public async Task<IWriteResult> AppendToStreamAsync(
		string streamName,
		StreamState expectedState,
		IEnumerable<EventData> eventData,
		Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) {
		var operationOptions = Settings.OperationOptions.Clone();
		configureOperationOptions?.Invoke(operationOptions);

		_log.LogDebug("Append to stream - {streamName}@{expectedRevision}.", streamName, expectedState);

		var batchAppender = _streamAppender;
		var task =
			userCredentials == null && await batchAppender.IsUsable().ConfigureAwait(false)
				? batchAppender.Append(streamName, expectedState, eventData, deadline, cancellationToken)
				: AppendToStreamInternal(
					(await GetChannelInfo(cancellationToken).ConfigureAwait(false)).CallInvoker,
					new AppendReq {
						Options = new AppendReq.Types.Options {
							StreamIdentifier = streamName
						}
					}.WithAnyStreamRevision(expectedState), eventData, operationOptions, deadline, userCredentials,
					cancellationToken
				);

		return (await task.ConfigureAwait(false)).OptionallyThrowWrongExpectedVersionException(operationOptions);
	}

	async ValueTask<IWriteResult> AppendToStreamInternal(
		CallInvoker callInvoker,
		AppendReq header,
		IEnumerable<EventData> eventData,
		EventStoreClientOperationOptions operationOptions,
		TimeSpan? deadline,
		UserCredentials? userCredentials,
		CancellationToken cancellationToken
	) {
		using var call = new Streams.Streams.StreamsClient(callInvoker).Append(
			EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken)
		);

		await call.RequestStream.WriteAsync(header).ConfigureAwait(false);

		foreach (var e in eventData)
			await call.RequestStream.WriteAsync(
				new AppendReq {
					ProposedMessage = new AppendReq.Types.ProposedMessage {
						Id             = e.EventId.ToDto(),
						Data           = ByteString.CopyFrom(e.Data.Span),
						CustomMetadata = ByteString.CopyFrom(e.Metadata.Span),
						Metadata = {
							{ Constants.Metadata.Type, e.Type },
							{ Constants.Metadata.ContentType, e.ContentType }
						}
					}
				}
			).ConfigureAwait(false);

		await call.RequestStream.CompleteAsync().ConfigureAwait(false);

		var response = await call.ResponseAsync.ConfigureAwait(false);

		if (response.Success != null)
			return HandleSuccessAppend(response, header);

		if (response.WrongExpectedVersion == null)
			throw new InvalidOperationException("The operation completed with an unexpected result.");

		return HandleWrongExpectedRevision(response, header, operationOptions);
	}

	IWriteResult HandleSuccessAppend(AppendResp response, AppendReq header) {
		var currentRevision = response.Success.CurrentRevisionOptionCase == AppendResp.Types.Success.CurrentRevisionOptionOneofCase.NoStream
			? StreamRevision.None
			: new StreamRevision(response.Success.CurrentRevision);

		var position = response.Success.PositionOptionCase == AppendResp.Types.Success.PositionOptionOneofCase.Position
			? new Position(response.Success.Position.CommitPosition, response.Success.Position.PreparePosition)
			: default;

		_log.LogDebug(
			"Append to stream succeeded - {streamName}@{logPosition}/{nextExpectedVersion}.",
			header.Options.StreamIdentifier,
			position,
			currentRevision
		);

		return new SuccessResult(currentRevision, position);
	}

	IWriteResult HandleWrongExpectedRevision(AppendResp response, AppendReq header, EventStoreClientOperationOptions operationOptions) {
		var actualStreamRevision = response.WrongExpectedVersion.CurrentRevisionOptionCase switch {
			CurrentRevisionOptionOneofCase.CurrentNoStream => StreamRevision.None,
			_ => new StreamRevision(response.WrongExpectedVersion.CurrentRevision)
		};

		_log.LogDebug(
			"Append to stream failed with Wrong Expected Version - {streamName}/{expectedRevision}/{currentRevision}",
			header.Options.StreamIdentifier,
			new StreamRevision(header.Options.Revision),
			actualStreamRevision
		);

		if (operationOptions.ThrowOnAppendFailure) {
			if (response.WrongExpectedVersion.ExpectedRevisionOptionCase == ExpectedRevisionOptionOneofCase.ExpectedRevision)
				throw new WrongExpectedVersionException(
					header.Options.StreamIdentifier!,
					new StreamRevision(response.WrongExpectedVersion.ExpectedRevision),
					actualStreamRevision
				);

			var expectedStreamState =
				response.WrongExpectedVersion.ExpectedRevisionOptionCase switch {
					ExpectedRevisionOptionOneofCase.ExpectedAny          => StreamState.Any,
					ExpectedRevisionOptionOneofCase.ExpectedNoStream     => StreamState.NoStream,
					ExpectedRevisionOptionOneofCase.ExpectedStreamExists => StreamState.StreamExists,
					_                                                                                          => StreamState.Any
				};

			throw new WrongExpectedVersionException(
				header.Options.StreamIdentifier!,
				expectedStreamState,
				actualStreamRevision
			);
		}

		var expectedRevision = response.WrongExpectedVersion.ExpectedRevisionOptionCase
		                    == ExpectedRevisionOptionOneofCase
			                       .ExpectedRevision
			? new StreamRevision(response.WrongExpectedVersion.ExpectedRevision)
			: StreamRevision.None;

		return new WrongExpectedVersionResult(
			header.Options.StreamIdentifier!,
			expectedRevision,
			actualStreamRevision
		);
	}

	class StreamAppender : IDisposable {
		readonly Task<AsyncDuplexStreamingCall<BatchAppendReq, BatchAppendResp>?> _callTask;
		readonly CancellationToken                                                _cancellationToken;
		readonly Channel<BatchAppendReq>                                          _channel;
		readonly Action<Exception>                                                _onException;
		readonly ConcurrentDictionary<Uuid, TaskCompletionSource<IWriteResult>>   _pendingRequests;
		readonly EventStoreClientSettings                                         _settings;

		public StreamAppender(
			EventStoreClientSettings settings,
			Task<AsyncDuplexStreamingCall<BatchAppendReq, BatchAppendResp>?> callTask, CancellationToken cancellationToken,
			Action<Exception> onException
		) {
			_settings          = settings;
			_callTask          = callTask;
			_cancellationToken = cancellationToken;
			_onException       = onException;
			_channel           = Channel.CreateBounded<BatchAppendReq>(10000);
			_pendingRequests   = new ConcurrentDictionary<Uuid, TaskCompletionSource<IWriteResult>>();
			_                  = Task.Factory.StartNew(Send);
			_                  = Task.Factory.StartNew(Receive);
		}

		public void Dispose() {
			_channel.Writer.TryComplete();
		}

		public ValueTask<IWriteResult> Append(
			string streamName, StreamRevision expectedStreamPosition,
			IEnumerable<EventData> events, TimeSpan? timeoutAfter, CancellationToken cancellationToken = default
		) =>
			AppendInternal(
				BatchAppendReq.Types.Options.Create(streamName, expectedStreamPosition, timeoutAfter),
				events, cancellationToken
			);

		public ValueTask<IWriteResult> Append(
			string streamName, StreamState expectedStreamState,
			IEnumerable<EventData> events, TimeSpan? timeoutAfter, CancellationToken cancellationToken = default
		) =>
			AppendInternal(
				BatchAppendReq.Types.Options.Create(streamName, expectedStreamState, timeoutAfter),
				events, cancellationToken
			);

		public async ValueTask<bool> IsUsable() {
			var call = await _callTask.ConfigureAwait(false);
			return call != null;
		}

		async Task Receive() {
			try {
				var call = await _callTask.ConfigureAwait(false);
				if (call is null) {
					_channel.Writer.TryComplete(new NotSupportedException("Server does not support batch append"));
					return;
				}

				await foreach (var response in call.ResponseStream.ReadAllAsync(_cancellationToken)
					               .ConfigureAwait(false)) {
					if (!_pendingRequests.TryRemove(Uuid.FromDto(response.CorrelationId), out var writeResult)) continue; // TODO: Log?

					try {
						writeResult.TrySetResult(response.ToWriteResult());
					}
					catch (Exception ex) {
						writeResult.TrySetException(ex);
					}
				}
			}
			catch (Exception ex) {
				// signal that no tcs added to _pendingRequests after this point will necessarily complete
				_channel.Writer.TryComplete(ex);

				// complete whatever tcs's we have
				_onException(ex);
				foreach (var request in _pendingRequests) request.Value.TrySetException(ex);
			}
		}

		async Task Send() {
			var call = await _callTask.ConfigureAwait(false);
			if (call is null)
				throw new NotSupportedException("Server does not support batch append");

			await foreach (var appendRequest in _channel.Reader.ReadAllAsync(_cancellationToken)
				               .ConfigureAwait(false))
				await call.RequestStream.WriteAsync(appendRequest).ConfigureAwait(false);

			await call.RequestStream.CompleteAsync().ConfigureAwait(false);
		}

		async ValueTask<IWriteResult> AppendInternal(
			BatchAppendReq.Types.Options options,
			IEnumerable<EventData> events, CancellationToken cancellationToken
		) {
			var batchSize        = 0;
			var correlationId    = Uuid.NewUuid();
			var correlationIdDto = correlationId.ToDto();

			var complete = _pendingRequests.GetOrAdd(correlationId, new TaskCompletionSource<IWriteResult>());

			try {
				foreach (var appendRequest in GetRequests()) await _channel.Writer.WriteAsync(appendRequest, cancellationToken).ConfigureAwait(false);
			}
			catch (ChannelClosedException ex) {
				// channel is closed, our tcs won't necessarily get completed, don't wait for it.
				throw ex.InnerException ?? ex;
			}

			return await complete.Task.ConfigureAwait(false);

			IEnumerable<BatchAppendReq> GetRequests() {
				var first            = true;
				var proposedMessages = new List<BatchAppendReq.Types.ProposedMessage>();
				foreach (var @event in events) {
					var proposedMessage = new BatchAppendReq.Types.ProposedMessage {
						Data           = ByteString.CopyFrom(@event.Data.Span),
						CustomMetadata = ByteString.CopyFrom(@event.Metadata.Span),
						Id             = @event.EventId.ToDto(),
						Metadata = {
							{ Constants.Metadata.Type, @event.Type },
							{ Constants.Metadata.ContentType, @event.ContentType }
						}
					};

					proposedMessages.Add(proposedMessage);

					if ((batchSize += proposedMessage.CalculateSize()) <
					    _settings.OperationOptions.BatchAppendSize)
						continue;

					yield return new BatchAppendReq {
						ProposedMessages = { proposedMessages },
						CorrelationId    = correlationIdDto,
						Options          = first ? options : null
					};

					first = false;
					proposedMessages.Clear();
					batchSize = 0;
				}

				yield return new BatchAppendReq {
					ProposedMessages = { proposedMessages },
					IsFinal          = true,
					CorrelationId    = correlationIdDto,
					Options          = first ? options : null
				};
			}
		}
	}
}