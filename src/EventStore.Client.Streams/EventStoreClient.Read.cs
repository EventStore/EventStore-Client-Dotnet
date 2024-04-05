using System.Threading.Channels;
using EventStore.Client.Streams;
using Grpc.Core;
using static EventStore.Client.Streams.ReadResp;
using static EventStore.Client.Streams.ReadResp.ContentOneofCase;

namespace EventStore.Client;

public partial class EventStoreClient {
	/// <summary>
	/// Asynchronously reads all events.
	/// </summary>
	/// <param name="direction">The <see cref="Direction"/> in which to read.</param>
	/// <param name="position">The <see cref="Position"/> to start reading from.</param>
	/// <param name="maxCount">The maximum count to read.</param>
	/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
	/// <param name="deadline"></param>
	/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
	/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
	/// <returns></returns>
	public ReadAllStreamResult ReadAllAsync(
		Direction direction,
		Position position,
		long maxCount = long.MaxValue,
		bool resolveLinkTos = false,
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) {
		if (maxCount <= 0) throw new ArgumentOutOfRangeException(nameof(maxCount));

		return new ReadAllStreamResult(
			async _ => {
				var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
				return channelInfo.CallInvoker;
			}, new ReadReq {
				Options = new ReadReq.Types.Options {
					ReadDirection = direction switch {
						Direction.Backwards => ReadReq.Types.Options.Types.ReadDirection.Backwards,
						Direction.Forwards  => ReadReq.Types.Options.Types.ReadDirection.Forwards,
						_                   => throw InvalidOption(direction)
					},
					ResolveLinks = resolveLinkTos,
					All = new ReadReq.Types.Options.Types.AllOptions {
						Position = new ReadReq.Types.Options.Types.Position {
							CommitPosition  = position.CommitPosition,
							PreparePosition = position.PreparePosition
						}
					},
					Count         = (ulong)maxCount,
					UuidOption    = new ReadReq.Types.Options.Types.UUIDOption { Structured = new Empty() },
					NoFilter      = new Empty(),
					ControlOption = new ReadReq.Types.Options.Types.ControlOption { Compatibility = 1 }
				}
			}, Settings, deadline, userCredentials, cancellationToken
		);
	}

	/// <summary>
	/// Asynchronously reads all events with filtering.
	/// </summary>
	/// <param name="direction">The <see cref="Direction"/> in which to read.</param>
	/// <param name="position">The <see cref="Position"/> to start reading from.</param>
	/// <param name="eventFilter">The <see cref="IEventFilter"/> to apply.</param>
	/// <param name="maxCount">The maximum count to read.</param>
	/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
	/// <param name="deadline"></param>
	/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
	/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
	/// <returns></returns>
	public ReadAllStreamResult ReadAllAsync(
		Direction direction,
		Position position,
		IEventFilter eventFilter,
		long maxCount = long.MaxValue,
		bool resolveLinkTos = false,
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) {
		if (maxCount <= 0) throw new ArgumentOutOfRangeException(nameof(maxCount));

		var readReq = new ReadReq {
			Options = new ReadReq.Types.Options {
				ReadDirection = direction switch {
					Direction.Backwards => ReadReq.Types.Options.Types.ReadDirection.Backwards,
					Direction.Forwards  => ReadReq.Types.Options.Types.ReadDirection.Forwards,
					_                   => throw InvalidOption(direction)
				},
				ResolveLinks = resolveLinkTos,
				All = new ReadReq.Types.Options.Types.AllOptions {
					Position = new ReadReq.Types.Options.Types.Position {
						CommitPosition  = position.CommitPosition,
						PreparePosition = position.PreparePosition
					}
				},
				Count         = (ulong)maxCount,
				UuidOption    = new ReadReq.Types.Options.Types.UUIDOption { Structured       = new Empty() },
				ControlOption = new ReadReq.Types.Options.Types.ControlOption { Compatibility = 1 },
				Filter        = GetFilterOptions(eventFilter)
			}
		};

		return new ReadAllStreamResult(
			async _ => {
				var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
				return channelInfo.CallInvoker;
			}, readReq, Settings, deadline, userCredentials, cancellationToken
		);
	}

	/// <summary>
	/// Asynchronously reads all the events from a stream.
	/// 
	/// The result could also be inspected as a means to avoid handling exceptions as the <see cref="ReadState"/> would indicate whether or not the stream is readable./>
	/// </summary>
	/// <param name="direction">The <see cref="Direction"/> in which to read.</param>
	/// <param name="streamName">The name of the stream to read.</param>
	/// <param name="revision">The <see cref="StreamRevision"/> to start reading from.</param>
	/// <param name="maxCount">The number of events to read from the stream.</param>
	/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
	/// <param name="deadline"></param>
	/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
	/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
	/// <returns></returns>
	public ReadStreamResult ReadStreamAsync(
		Direction direction,
		string streamName,
		StreamPosition revision,
		long maxCount = long.MaxValue,
		bool resolveLinkTos = false,
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) {
		if (maxCount <= 0) throw new ArgumentOutOfRangeException(nameof(maxCount));

		return new ReadStreamResult(
			async _ => {
				var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
				return channelInfo.CallInvoker;
			}, new ReadReq {
				Options = new ReadReq.Types.Options {
					ReadDirection = direction switch {
						Direction.Backwards => ReadReq.Types.Options.Types.ReadDirection.Backwards,
						Direction.Forwards  => ReadReq.Types.Options.Types.ReadDirection.Forwards,
						_                   => throw InvalidOption(direction)
					},
					ResolveLinks = resolveLinkTos,
					Stream = ReadReq.Types.Options.Types.StreamOptions.FromStreamNameAndRevision(
						streamName,
						revision
					),
					Count         = (ulong)maxCount,
					UuidOption    = new ReadReq.Types.Options.Types.UUIDOption { Structured = new Empty() },
					NoFilter      = new Empty(),
					ControlOption = new ReadReq.Types.Options.Types.ControlOption { Compatibility = 1 }
				}
			}, Settings, deadline, userCredentials, cancellationToken
		);
	}

	static ResolvedEvent ConvertToResolvedEvent(Types.ReadEvent readEvent) =>
		new(
			ConvertToEventRecord(readEvent.Event)!,
			ConvertToEventRecord(readEvent.Link),
			readEvent.PositionCase switch {
				Types.ReadEvent.PositionOneofCase.CommitPosition => readEvent.CommitPosition,
				_                                                => null
			}
		);

	static EventRecord? ConvertToEventRecord(ReadResp.Types.ReadEvent.Types.RecordedEvent? e) =>
		e == null
			? null
			: new EventRecord(
				e.StreamIdentifier!,
				Uuid.FromDto(e.Id),
				new StreamPosition(e.StreamRevision),
				new Position(e.CommitPosition, e.PreparePosition),
				e.Metadata,
				e.Data.ToByteArray(),
				e.CustomMetadata.ToByteArray()
			);

	/// <summary>
	/// A class that represents the result of a read operation on the $all stream. You may either enumerate this instance directly or <see cref="Messages"/>. Do not enumerate more than once.
	/// </summary>
	public class ReadAllStreamResult : IAsyncEnumerable<ResolvedEvent> {
		readonly Channel<StreamMessage>  _channel;
		readonly CancellationTokenSource _cts;

		int _messagesEnumerated;

		internal ReadAllStreamResult(
			Func<CancellationToken, Task<CallInvoker>> selectCallInvoker, ReadReq request,
			EventStoreClientSettings settings, TimeSpan? deadline, UserCredentials? userCredentials,
			CancellationToken cancellationToken
		) {
			var callOptions = EventStoreCallOptions.CreateStreaming(
				settings, deadline, userCredentials,
				cancellationToken
			);

			_channel = Channel.CreateBounded<StreamMessage>(ReadBoundedChannelOptions);

			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			var linkedCancellationToken = _cts.Token;

			_ = PumpMessages();

			async Task PumpMessages() {
				try {
					var       callInvoker = await selectCallInvoker(linkedCancellationToken).ConfigureAwait(false);
					var       client      = new Streams.Streams.StreamsClient(callInvoker);
					using var call        = client.Read(request, callOptions);
					await foreach (var response in call.ResponseStream.ReadAllAsync(linkedCancellationToken)
						               .ConfigureAwait(false))
						await _channel.Writer.WriteAsync(
							response.ContentCase switch {
								StreamNotFound      => StreamMessage.NotFound.Instance,
								Event               => new StreamMessage.Event(ConvertToResolvedEvent(response.Event)),
								FirstStreamPosition => new StreamMessage.FirstStreamPosition(new StreamPosition(response.FirstStreamPosition)),
								LastStreamPosition  => new StreamMessage.LastStreamPosition(new StreamPosition(response.LastStreamPosition)),
								LastAllStreamPosition => new StreamMessage.LastAllStreamPosition(
									new Position(
										response.LastAllStreamPosition.CommitPosition,
										response.LastAllStreamPosition.PreparePosition
									)
								),
								_ => StreamMessage.Unknown.Instance
							}, linkedCancellationToken
						).ConfigureAwait(false);

					_channel.Writer.Complete();
				}
				catch (Exception ex) {
					_channel.Writer.TryComplete(ex);
				}
			}
		}

		/// <summary>
		/// The last <see cref="Position"/> of the $all stream, if available.
		/// </summary>
		public Position? LastPosition { get; private set; }

		/// <summary>
		/// An <see cref="IAsyncEnumerable{StreamMessage}"/>. Do not enumerate more than once.
		/// </summary>
		public IAsyncEnumerable<StreamMessage> Messages {
			get {
				return GetMessages();

				async IAsyncEnumerable<StreamMessage> GetMessages() {
					if (Interlocked.Exchange(ref _messagesEnumerated, 1) == 1) throw new InvalidOperationException("Messages may only be enumerated once.");

					try {
						await foreach (var message in _channel.Reader.ReadAllAsync(_cts.Token)
							               .ConfigureAwait(false)) {
							if (message is StreamMessage.LastAllStreamPosition(var position)) LastPosition = position;

							yield return message;
						}
					}
					finally {
						_cts.Cancel();
					}
				}
			}
		}

		/// <inheritdoc />
		public async IAsyncEnumerator<ResolvedEvent> GetAsyncEnumerator(
			CancellationToken cancellationToken = default
		) {
			try {
				await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
					if (message is not StreamMessage.Event e) continue;

					yield return e.ResolvedEvent;
				}
			}
			finally {
#if NET8_0_OR_GREATER
				await _cts.CancelAsync().ConfigureAwait(false);
#else
				_cts.Cancel();
#endif
			}
		}
	}

	/// <summary>
	/// A class that represents the result of a read operation on a stream. You may either enumerate this instance directly or <see cref="Messages"/>. Do not enumerate more than once.
	/// </summary>
	public class ReadStreamResult : IAsyncEnumerable<ResolvedEvent> {
		readonly Channel<StreamMessage>  _channel;
		readonly CancellationTokenSource _cts;

		int _messagesEnumerated;

		internal ReadStreamResult(
			Func<CancellationToken, Task<CallInvoker>> selectCallInvoker, ReadReq request,
			EventStoreClientSettings settings, TimeSpan? deadline, UserCredentials? userCredentials,
			CancellationToken cancellationToken
		) {
			var callOptions = EventStoreCallOptions.CreateStreaming(
				settings, deadline, userCredentials,
				cancellationToken
			);

			_channel = Channel.CreateBounded<StreamMessage>(ReadBoundedChannelOptions);

			StreamName = request.Options.Stream.StreamIdentifier!;

			var tcs = new TaskCompletionSource<ReadState>();
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			var linkedCancellationToken = _cts.Token;
#pragma warning disable CS0612
			ReadState = tcs.Task;
#pragma warning restore CS0612

			_ = PumpMessages();

			async Task PumpMessages() {
				var firstMessageRead = false;

				try {
					var       callInvoker = await selectCallInvoker(linkedCancellationToken).ConfigureAwait(false);
					var       client      = new Streams.Streams.StreamsClient(callInvoker);
					using var call        = client.Read(request, callOptions);

					await foreach (var response in call.ResponseStream.ReadAllAsync(linkedCancellationToken)
						               .ConfigureAwait(false)) {
						if (!firstMessageRead) {
							firstMessageRead = true;

							if (response.ContentCase != StreamNotFound || request.Options.Stream == null) {
								await _channel.Writer.WriteAsync(StreamMessage.Ok.Instance, linkedCancellationToken)
									.ConfigureAwait(false);

								tcs.SetResult(Client.ReadState.Ok);
							}
							else {
								tcs.SetResult(Client.ReadState.StreamNotFound);
							}
						}

						await _channel.Writer.WriteAsync(
							response.ContentCase switch {
								StreamNotFound                       => StreamMessage.NotFound.Instance,
								Event                                => new StreamMessage.Event(ConvertToResolvedEvent(response.Event)),
								ContentOneofCase.FirstStreamPosition => new StreamMessage.FirstStreamPosition(new StreamPosition(response.FirstStreamPosition)),
								ContentOneofCase.LastStreamPosition  => new StreamMessage.LastStreamPosition(new StreamPosition(response.LastStreamPosition)),
								LastAllStreamPosition => new StreamMessage.LastAllStreamPosition(
									new Position(
										response.LastAllStreamPosition.CommitPosition,
										response.LastAllStreamPosition.PreparePosition
									)
								),
								_ => StreamMessage.Unknown.Instance
							}, linkedCancellationToken
						).ConfigureAwait(false);
					}

					_channel.Writer.Complete();
				}
				catch (Exception ex) {
					tcs.TrySetException(ex);
					_channel.Writer.TryComplete(ex);
				}
			}
		}

		/// <summary>
		/// The name of the stream.
		/// </summary>
		public string StreamName { get; }

		/// <summary>
		/// The <see cref="StreamPosition"/> of the first message in this stream. Will only be filled once <see cref="Messages"/> has been enumerated. 
		/// </summary>
		public StreamPosition? FirstStreamPosition { get; private set; }

		/// <summary>
		/// The <see cref="StreamPosition"/> of the last message in this stream. Will only be filled once <see cref="Messages"/> has been enumerated. 
		/// </summary>
		public StreamPosition? LastStreamPosition { get; private set; }

		/// <summary>
		/// An <see cref="IAsyncEnumerable{StreamMessage}"/>. Do not enumerate more than once.
		/// </summary>
		public IAsyncEnumerable<StreamMessage> Messages {
			get {
				return GetMessages();

				async IAsyncEnumerable<StreamMessage> GetMessages() {
					if (Interlocked.Exchange(ref _messagesEnumerated, 1) == 1) throw new InvalidOperationException("Messages may only be enumerated once.");

					try {
						await foreach (var message in _channel.Reader.ReadAllAsync(_cts.Token).ConfigureAwait(false)) {
							switch (message) {
								case StreamMessage.FirstStreamPosition(var streamPosition):
									FirstStreamPosition = streamPosition;
									break;

								case StreamMessage.LastStreamPosition(var lastStreamPosition):
									LastStreamPosition = lastStreamPosition;
									break;

								default:
									break;
							}

							yield return message;
						}
					}
					finally {
						_cts.Cancel();
					}
				}
			}
		}

		/// <summary>
		/// The <see cref="ReadState"/>.
		/// </summary>
		public Task<ReadState> ReadState { get; }

		/// <inheritdoc />
		public async IAsyncEnumerator<ResolvedEvent> GetAsyncEnumerator(
			CancellationToken cancellationToken = default
		) {
			try {
				await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
					if (message is StreamMessage.NotFound) throw new StreamNotFoundException(StreamName);

					if (message is not StreamMessage.Event e) continue;

					yield return e.ResolvedEvent;
				}
			}
			finally {
#if NET8_0_OR_GREATER
				await _cts.CancelAsync().ConfigureAwait(false);
#else
				_cts.Cancel();
#endif
			}
		}
	}
}