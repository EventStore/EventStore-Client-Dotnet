using System.Threading.Channels;
using EventStore.Client.Diagnostics;
using EventStore.Client.Streams;
using Grpc.Core;

using static EventStore.Client.Streams.ReadResp.ContentOneofCase;

namespace EventStore.Client {
	public partial class EventStoreClient {
		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="start">A <see cref="FromAll"/> (exclusive of) to start the subscription from.</param>
		/// <param name="eventAppeared">A Task invoked and awaited when a new event is received over the subscription.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
		/// <param name="filterOptions">The optional <see cref="SubscriptionFilterOptions"/> to apply.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamSubscription> SubscribeToAllAsync(
			FromAll start,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			SubscriptionFilterOptions? filterOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) => StreamSubscription.Confirm(
			SubscribeToAll(start, resolveLinkTos, filterOptions, userCredentials, cancellationToken),
			eventAppeared,
			subscriptionDropped,
			_log,
			filterOptions?.CheckpointReached,
			cancellationToken: cancellationToken
		);

		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="start">A <see cref="FromAll"/> (exclusive of) to start the subscription from.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="filterOptions">The optional <see cref="SubscriptionFilterOptions"/> to apply.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public StreamSubscriptionResult SubscribeToAll(
			FromAll start,
			bool resolveLinkTos = false,
			SubscriptionFilterOptions? filterOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) => new(
			async _ => await GetChannelInfo(cancellationToken).ConfigureAwait(false),
			new ReadReq {
				Options = new ReadReq.Types.Options {
					ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
					ResolveLinks  = resolveLinkTos,
					All           = ReadReq.Types.Options.Types.AllOptions.FromSubscriptionPosition(start),
					Subscription  = new ReadReq.Types.Options.Types.SubscriptionOptions(),
					Filter        = GetFilterOptions(filterOptions)!,
					UuidOption    = new() { Structured = new() }
				}
			},
			Settings,
			userCredentials,
			cancellationToken
		);

		/// <summary>
		/// Subscribes to a stream from a <see cref="StreamPosition">checkpoint</see>.
		/// </summary>
		/// <param name="start">A <see cref="FromStream"/> (exclusive of) to start the subscription from.</param>
		/// <param name="streamName">The name of the stream to read events from.</param>
		/// <param name="eventAppeared">A Task invoked and awaited when a new event is received over the subscription.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamSubscription> SubscribeToStreamAsync(
			string streamName,
			FromStream start,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) => StreamSubscription.Confirm(
			SubscribeToStream(streamName, start, resolveLinkTos, userCredentials, cancellationToken),
			eventAppeared,
			subscriptionDropped,
			_log,
			cancellationToken: cancellationToken
		);

		/// <summary>
		/// Subscribes to a stream from a <see cref="StreamPosition">checkpoint</see>.
		/// </summary>
		/// <param name="start">A <see cref="FromStream"/> (exclusive of) to start the subscription from.</param>
		/// <param name="streamName">The name of the stream to read events from.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public StreamSubscriptionResult SubscribeToStream(
			string streamName,
			FromStream start,
			bool resolveLinkTos = false,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) => new(
			async _ => await GetChannelInfo(cancellationToken).ConfigureAwait(false),
			new ReadReq {
				Options = new ReadReq.Types.Options {
					ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
					ResolveLinks = resolveLinkTos,
					Stream = ReadReq.Types.Options.Types.StreamOptions.FromSubscriptionPosition(streamName, start),
					Subscription = new ReadReq.Types.Options.Types.SubscriptionOptions(),
					UuidOption = new() { Structured = new() }
				}
			},
			Settings,
			userCredentials,
			cancellationToken
		);

		/// <summary>
		/// A class that represents the result of a subscription operation. You may either enumerate this instance directly or <see cref="Messages"/>. Do not enumerate more than once.
		/// </summary>
		public class StreamSubscriptionResult : IAsyncEnumerable<ResolvedEvent>, IAsyncDisposable, IDisposable {
			private readonly ReadReq                             _request;
			private readonly Channel<StreamMessage>              _channel;
			private readonly CancellationTokenSource             _cts;
			private readonly CallOptions                         _callOptions;
			private readonly EventStoreClientSettings            _settings;
			private          AsyncServerStreamingCall<ReadResp>? _call;

			private int _messagesEnumerated;

			/// <summary>
			/// The server-generated unique identifier for the subscription.
			/// </summary>
			public string? SubscriptionId { get; private set; }

			/// <summary>
			/// An <see cref="IAsyncEnumerable{StreamMessage}"/>. Do not enumerate more than once.
			/// </summary>
			public IAsyncEnumerable<StreamMessage> Messages {
				get {
					if (Interlocked.Exchange(ref _messagesEnumerated, 1) == 1)
                        throw new InvalidOperationException("Messages may only be enumerated once.");

					return GetMessages();

					async IAsyncEnumerable<StreamMessage> GetMessages() {
						try {
							await foreach (var message in _channel.Reader.ReadAllAsync(_cts.Token)) {
								if (message is StreamMessage.SubscriptionConfirmation(var subscriptionId)) 
                                    SubscriptionId = subscriptionId;

								yield return message;
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

			internal StreamSubscriptionResult(
				Func<CancellationToken, Task<ChannelInfo>> selectChannelInfo,
				ReadReq request, EventStoreClientSettings settings, UserCredentials? userCredentials,
				CancellationToken cancellationToken
			) {
				_request  = request;
				_settings = settings;
                
				_callOptions = EventStoreCallOptions.CreateStreaming(
					settings,
					userCredentials: userCredentials,
					cancellationToken: cancellationToken
				);

				_channel = Channel.CreateBounded<StreamMessage>(ReadBoundedChannelOptions);

				_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

				if (_request.Options.FilterOptionCase == ReadReq.Types.Options.FilterOptionOneofCase.None) {
					_request.Options.NoFilter = new();
				}

				_ = PumpMessages();

				return;

				async Task PumpMessages() {
					try {
						var channelInfo = await selectChannelInfo(_cts.Token).ConfigureAwait(false);
						var client      = new Streams.Streams.StreamsClient(channelInfo.CallInvoker);
						_call = client.Read(_request, _callOptions);
                        await foreach (var response in _call.ResponseStream.ReadAllAsync(_cts.Token).ConfigureAwait(false)) {
                            StreamMessage subscriptionMessage =
                                response.ContentCase switch {
                                    Confirmation          => new StreamMessage.SubscriptionConfirmation(response.Confirmation.SubscriptionId),
                                    Event                 => new StreamMessage.Event(ConvertToResolvedEvent(response.Event)),
                                    FirstStreamPosition   => new StreamMessage.FirstStreamPosition(new StreamPosition(response.FirstStreamPosition)),
                                    LastStreamPosition    => new StreamMessage.LastStreamPosition(new StreamPosition(response.LastStreamPosition)),
                                    LastAllStreamPosition => new StreamMessage.LastAllStreamPosition(
                                        new Position(
                                            response.LastAllStreamPosition.CommitPosition,
                                            response.LastAllStreamPosition.PreparePosition
                                        )
                                    ),
                                    Checkpoint => new StreamMessage.AllStreamCheckpointReached(
                                        new Position(
                                            response.Checkpoint.CommitPosition,
                                            response.Checkpoint.PreparePosition
                                        )
                                    ),
                                    CaughtUp   => StreamMessage.CaughtUp.Instance,
                                    FellBehind => StreamMessage.FellBehind.Instance,
                                    _          => StreamMessage.Unknown.Instance
                                };

                            if (subscriptionMessage is StreamMessage.Event evt)
                                EventStoreClientDiagnostics.ActivitySource.TraceSubscriptionEvent(
                                    SubscriptionId,
                                    evt.ResolvedEvent,
                                    channelInfo,
                                    _settings,
                                    userCredentials
                                );

                            await _channel.Writer
                                .WriteAsync(subscriptionMessage, _cts.Token)
                                .ConfigureAwait(false);
                        }

						_channel.Writer.Complete();
					} catch (Exception ex) {
						_channel.Writer.TryComplete(ex);
					}
				}
			}

			/// <inheritdoc />
			public async ValueTask DisposeAsync() {
                //TODO SS: Check if `CastAndDispose` is still relevant
				await CastAndDispose(_cts).ConfigureAwait(false);
				await CastAndDispose(_call).ConfigureAwait(false);

				return;

				static async ValueTask CastAndDispose(IDisposable? resource) {
					switch (resource) {
						case null:
							return;

						case IAsyncDisposable disposable:
							await disposable.DisposeAsync().ConfigureAwait(false);
							break;

						default:
							resource.Dispose();
							break;
					}
				}
			}

			/// <inheritdoc />
			public void Dispose() {
				_cts.Dispose();
				_call?.Dispose();
			}

			/// <inheritdoc />
			public async IAsyncEnumerator<ResolvedEvent> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
				try {
					await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
						if (message is not StreamMessage.Event e)
                            continue;

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
}
