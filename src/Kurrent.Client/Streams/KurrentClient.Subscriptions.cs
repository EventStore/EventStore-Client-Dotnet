using System.Threading.Channels;
using EventStore.Client.Diagnostics;
using EventStore.Client.Serialization;
using EventStore.Client.Streams;
using Grpc.Core;
using static EventStore.Client.Streams.ReadResp.ContentOneofCase;

namespace EventStore.Client {
	/// <summary>
	/// Subscribes to all events options.
	/// </summary>
	public class SubscribeToAllOptions {
		/// <summary>
		/// A <see cref="FromAll"/> (exclusive of) to start the subscription from.
		/// </summary>
		public FromAll Start { get; set; } = FromAll.Start;

		/// <summary>
		/// Whether to resolve LinkTo events automatically.
		/// </summary>
		public bool ResolveLinkTos { get; set; }

		/// <summary>
		/// The optional <see cref="SubscriptionFilterOptions"/> to apply.
		/// </summary>
		public SubscriptionFilterOptions? FilterOptions { get; set; }

		/// <summary>
		/// The optional user credentials to perform operation with.
		/// </summary>
		public UserCredentials? UserCredentials { get; set; }

		/// <summary>
		/// Allows to customize or disable the automatic deserialization
		/// </summary>
		public OperationSerializationSettings? SerializationSettings { get; set; }
	}

	/// <summary>
	/// Subscribes to all events options.
	/// </summary>
	public class SubscribeToStreamOptions {
		/// <summary>
		/// A <see cref="FromAll"/> (exclusive of) to start the subscription from.
		/// </summary>
		public FromStream Start { get; set; } = FromStream.Start;

		/// <summary>
		/// Whether to resolve LinkTo events automatically.
		/// </summary>
		public bool ResolveLinkTos { get; set; }

		/// <summary>
		/// The optional user credentials to perform operation with.
		/// </summary>
		public UserCredentials? UserCredentials { get; set; }

		/// <summary>
		/// Allows to customize or disable the automatic deserialization
		/// </summary>
		public OperationSerializationSettings? SerializationSettings { get; set; }
	}

	public class SubscriptionListener {
#if NET48
		/// <summary>
		/// A handler called when a new event is received over the subscription.
		/// </summary>
		public Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> EventAppeared { get; set; } = null!;
#else
		public required Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> EventAppeared { get; set; }
#endif
		/// <summary>
		/// A handler called if the subscription is dropped.
		/// </summary>
		public Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? SubscriptionDropped { get; set; }

		/// <summary>
		/// A handler called when a checkpoint is reached.
		/// Set the checkpointInterval in subscription filter options to define how often this method is called.
		/// </summary>
		public Func<StreamSubscription, Position, CancellationToken, Task>? CheckpointReached { get; set; }

		/// <summary>
		/// Returns the subscription listener with configured handlers
		/// </summary>
		/// <param name="eventAppeared">Handler invoked when a new event is received over the subscription.</param>
		/// <param name="subscriptionDropped">A handler invoked if the subscription is dropped.</param>
		/// <param name="checkpointReached">A handler called when a checkpoint is reached.
		/// Set the checkpointInterval in subscription filter options to define how often this method is called.
		/// </param>
		/// <returns></returns>
		public static SubscriptionListener Handle(
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
			Func<StreamSubscription, Position, CancellationToken, Task>? checkpointReached = null
		) =>
			new SubscriptionListener {
				EventAppeared       = eventAppeared,
				SubscriptionDropped = subscriptionDropped,
				CheckpointReached   = checkpointReached
			};
	}

	public partial class KurrentClient {
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
		) {
			var listener = SubscriptionListener.Handle(
				eventAppeared,
				subscriptionDropped,
				filterOptions?.CheckpointReached
			);

			var options = new SubscribeToAllOptions {
				Start                 = start,
				FilterOptions         = filterOptions,
				ResolveLinkTos        = resolveLinkTos,
				UserCredentials       = userCredentials,
				SerializationSettings = OperationSerializationSettings.Disabled,
			};

			return SubscribeToAllAsync(listener, options, cancellationToken);
		}

		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="listener">Listener configured to receive notifications about new events and subscription state change.</param>
		/// <param name="options">Optional settings like: Position <see cref="FromAll"/> from which to read, <see cref="SubscriptionFilterOptions"/> to apply, etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamSubscription> SubscribeToAllAsync(
			SubscriptionListener listener,
			SubscribeToAllOptions options,
			CancellationToken cancellationToken = default
		) {
			listener.CheckpointReached ??= options.FilterOptions?.CheckpointReached;

			return StreamSubscription.Confirm(
				SubscribeToAll(options, cancellationToken),
				listener,
				_log,
				cancellationToken
			);
		}

		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="options">Optional settings like: Position <see cref="FromAll"/> from which to read, <see cref="SubscriptionFilterOptions"/> to apply, etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public StreamSubscriptionResult SubscribeToAll(
			SubscribeToAllOptions options,
			CancellationToken cancellationToken = default
		) => new(
			async _ => await GetChannelInfo(cancellationToken).ConfigureAwait(false),
			new ReadReq {
				Options = new ReadReq.Types.Options {
					ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
					ResolveLinks  = options.ResolveLinkTos,
					All           = ReadReq.Types.Options.Types.AllOptions.FromSubscriptionPosition(options.Start),
					Subscription  = new ReadReq.Types.Options.Types.SubscriptionOptions(),
					Filter        = GetFilterOptions(options.FilterOptions)!,
					UuidOption    = new() { Structured = new() }
				}
			},
			Settings,
			options.UserCredentials,
			_messageSerializer.With(Settings.Serialization, options.SerializationSettings),
			cancellationToken
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
		) =>
			SubscribeToAll(
				new SubscribeToAllOptions {
					Start                 = start,
					ResolveLinkTos        = resolveLinkTos,
					FilterOptions         = filterOptions,
					UserCredentials       = userCredentials,
					SerializationSettings = OperationSerializationSettings.Disabled
				},
				cancellationToken
			);

		/// <summary>
		/// Subscribes to a stream from a <see cref="StreamPosition">checkpoint</see>.
		/// </summary>
		/// <param name="start">A <see cref="FromStream"/> (exclusive of) to start the subscription from.</param>
		/// <param name="streamName">The name of the stream to subscribe for notifications about new events.</param>
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
		) =>
			SubscribeToStreamAsync(
				streamName,
				SubscriptionListener.Handle(eventAppeared, subscriptionDropped),
				new SubscribeToStreamOptions {
					Start                 = start,
					ResolveLinkTos        = resolveLinkTos,
					UserCredentials       = userCredentials,
					SerializationSettings = OperationSerializationSettings.Disabled
				},
				cancellationToken
			);

		/// <summary>
		/// Subscribes to a stream from a <see cref="StreamPosition">checkpoint</see>.
		/// </summary>
		/// <param name="streamName">The name of the stream to subscribe for notifications about new events.</param>
		/// <param name="listener">Listener configured to receive notifications about new events and subscription state change.</param>
		/// <param name="options">Optional settings like: Position <see cref="FromStream"/> from which to read, etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamSubscription> SubscribeToStreamAsync(
			string streamName,
			SubscriptionListener listener,
			SubscribeToStreamOptions options,
			CancellationToken cancellationToken = default
		) {
			return StreamSubscription.Confirm(
				SubscribeToStream(streamName, options, cancellationToken),
				listener,
				_log,
				cancellationToken
			);
		}

		/// <summary>
		/// Subscribes to a stream from a <see cref="StreamPosition">checkpoint</see>.
		/// </summary>
		/// <param name="start">A <see cref="FromStream"/> (exclusive of) to start the subscription from.</param>
		/// <param name="streamName">The name of the stream to subscribe for notifications about new events.</param>
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
		) =>
			SubscribeToStream(
				streamName,
				new SubscribeToStreamOptions {
					Start                 = start,
					ResolveLinkTos        = resolveLinkTos,
					UserCredentials       = userCredentials,
					SerializationSettings = OperationSerializationSettings.Disabled
				},
				cancellationToken
			);

		/// <summary>
		/// Subscribes to a stream from a <see cref="StreamPosition">checkpoint</see>.
		/// </summary>
		/// <param name="streamName">The name of the stream to subscribe for notifications about new events.</param>
		/// <param name="options">Optional settings like: Position <see cref="FromStream"/> from which to read, etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public StreamSubscriptionResult SubscribeToStream(
			string streamName,
			SubscribeToStreamOptions options,
			CancellationToken cancellationToken = default
		) => new(
			async _ => await GetChannelInfo(cancellationToken).ConfigureAwait(false),
			new ReadReq {
				Options = new ReadReq.Types.Options {
					ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
					ResolveLinks  = options.ResolveLinkTos,
					Stream = ReadReq.Types.Options.Types.StreamOptions.FromSubscriptionPosition(
						streamName,
						options.Start
					),
					Subscription = new ReadReq.Types.Options.Types.SubscriptionOptions(),
					UuidOption   = new() { Structured = new() }
				}
			},
			Settings,
			options.UserCredentials,
			_messageSerializer.With(Settings.Serialization, options.SerializationSettings),
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
			private readonly KurrentClientSettings               _settings;
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
						} finally {
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
				ReadReq request,
				KurrentClientSettings settings,
				UserCredentials? userCredentials,
				IMessageSerializer messageSerializer,
				CancellationToken cancellationToken
			) {
				_request  = request;
				_settings = settings;

				_callOptions = KurrentCallOptions.CreateStreaming(
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
						await foreach (var response in _call.ResponseStream.ReadAllAsync(_cts.Token)
							               .ConfigureAwait(false)) {
							StreamMessage subscriptionMessage =
								response.ContentCase switch {
									Confirmation => new StreamMessage.SubscriptionConfirmation(
										response.Confirmation.SubscriptionId
									),
									Event => new StreamMessage.Event(
										ConvertToResolvedEvent(response.Event, messageSerializer)
									),
									FirstStreamPosition => new StreamMessage.FirstStreamPosition(
										new StreamPosition(response.FirstStreamPosition)
									),
									LastStreamPosition => new StreamMessage.LastStreamPosition(
										new StreamPosition(response.LastStreamPosition)
									),
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
								KurrentClientDiagnostics.ActivitySource.TraceSubscriptionEvent(
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
			public async IAsyncEnumerator<ResolvedEvent> GetAsyncEnumerator(
				CancellationToken cancellationToken = default
			) {
				try {
					await foreach (var message in
					               _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
						if (message is not StreamMessage.Event e)
							continue;

						yield return e.ResolvedEvent;
					}
				} finally {
#if NET8_0_OR_GREATER
					await _cts.CancelAsync().ConfigureAwait(false);
#else
					_cts.Cancel();
#endif
				}
			}
		}
	}

	public static class KurrentClientSubscribeToAllExtensions {
		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="kurrentClient"></param>
		/// <param name="listener">Listener configured to receive notifications about new events and subscription state change.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public static Task<StreamSubscription> SubscribeToAllAsync(
			this KurrentClient kurrentClient,
			SubscriptionListener listener,
			CancellationToken cancellationToken = default
		) =>
			kurrentClient.SubscribeToAllAsync(listener, new SubscribeToAllOptions(), cancellationToken);

		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="kurrentClient"></param>
		/// <param name="eventAppeared"></param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public static Task<StreamSubscription> SubscribeToAllAsync(
			this KurrentClient kurrentClient,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			CancellationToken cancellationToken = default
		) =>
			kurrentClient.SubscribeToAllAsync(
				eventAppeared,
				new SubscribeToAllOptions(),
				cancellationToken
			);

		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="kurrentClient"></param>
		/// <param name="eventAppeared">Handler invoked when a new event is received over the subscription.</param>
		/// <param name="options">Optional settings like: Position <see cref="FromAll"/> from which to read, <see cref="SubscriptionFilterOptions"/> to apply, etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public static Task<StreamSubscription> SubscribeToAllAsync(
			this KurrentClient kurrentClient,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			SubscribeToAllOptions options,
			CancellationToken cancellationToken = default
		) =>
			kurrentClient.SubscribeToAllAsync(
				SubscriptionListener.Handle(eventAppeared),
				options,
				cancellationToken
			);

		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="kurrentClient"></param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public static KurrentClient.StreamSubscriptionResult SubscribeToAll(
			this KurrentClient kurrentClient,
			CancellationToken cancellationToken = default
		) =>
			kurrentClient.SubscribeToAll(new SubscribeToAllOptions(), cancellationToken);
	}

	public static class KurrentClientSubscribeToStreamExtensions {
		/// <summary>
		/// Subscribes to messages from a specific stream
		/// </summary>
		/// <param name="kurrentClient"></param>
		/// <param name="streamName">The name of the stream to subscribe for notifications about new events.</param>
		/// <param name="listener">Listener configured to receive notifications about new events and subscription state change.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public static Task<StreamSubscription> SubscribeToStreamAsync(
			this KurrentClient kurrentClient,
			string streamName,
			SubscriptionListener listener,
			CancellationToken cancellationToken = default
		) =>
			kurrentClient.SubscribeToStreamAsync(
				streamName,
				listener,
				new SubscribeToStreamOptions(),
				cancellationToken
			);

		/// <summary>
		/// Subscribes to messages from a specific stream
		/// </summary>
		/// <param name="kurrentClient"></param>
		/// <param name="streamName">The name of the stream to subscribe for notifications about new events.</param>
		/// <param name="eventAppeared"></param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public static Task<StreamSubscription> SubscribeToStreamAsync(
			this KurrentClient kurrentClient,
			string streamName,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			CancellationToken cancellationToken = default
		) =>
			kurrentClient.SubscribeToStreamAsync(
				streamName,
				eventAppeared,
				new SubscribeToStreamOptions(),
				cancellationToken
			);

		/// <summary>
		/// Subscribes to messages from a specific stream
		/// </summary>
		/// <param name="kurrentClient"></param>
		/// <param name="streamName">The name of the stream to subscribe for notifications about new events.</param>
		/// <param name="eventAppeared">Handler invoked when a new event is received over the subscription.</param>
		/// <param name="options">Optional settings like: Position <see cref="FromAll"/> from which to read, <see cref="SubscriptionFilterOptions"/> to apply, etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public static Task<StreamSubscription> SubscribeToStreamAsync(
			this KurrentClient kurrentClient,
			string streamName,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			SubscribeToStreamOptions options,
			CancellationToken cancellationToken = default
		) =>
			kurrentClient.SubscribeToStreamAsync(
				streamName,
				SubscriptionListener.Handle(eventAppeared),
				options,
				cancellationToken
			);

		/// <summary>
		/// Subscribes to messages from a specific stream
		/// </summary>
		/// <param name="kurrentClient"></param>
		/// <param name="streamName">The name of the stream to subscribe for notifications about new events.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public static KurrentClient.StreamSubscriptionResult SubscribeToStream(
			this KurrentClient kurrentClient,
			string streamName,
			CancellationToken cancellationToken = default
		) =>
			kurrentClient.SubscribeToStream(streamName, new SubscribeToStreamOptions(), cancellationToken);
	}
}
