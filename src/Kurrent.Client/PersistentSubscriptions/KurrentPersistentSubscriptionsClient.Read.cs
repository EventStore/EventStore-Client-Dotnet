using System.Threading.Channels;
using EventStore.Client.PersistentSubscriptions;
using EventStore.Client.Diagnostics;
using Grpc.Core;
using static EventStore.Client.PersistentSubscriptions.PersistentSubscriptions;
using static EventStore.Client.PersistentSubscriptions.ReadResp.ContentOneofCase;
using DeserializationContext = Kurrent.Client.Core.Serialization.DeserializationContext;

namespace EventStore.Client {
	public class SubscribeToPersistentSubscriptionOptions {
		/// <summary>
		/// The size of the buffer.
		/// </summary>
		public int                                BufferSize            { get; set; } = 10;
		/// <summary>
		/// The optional settings to customize the default serialization for the specific persistent subscription
		/// </summary>
		public KurrentClientSerializationSettings SerializationSettings { get; set; } = KurrentClientSerializationSettings.Default();
		/// <summary>
		/// The optional user credentials to perform operation with.
		/// </summary>
		public UserCredentials?                   UserCredentials       { get; set; } = null;
	}

	partial class KurrentPersistentSubscriptionsClient {
		/// <summary>
		/// Subscribes to a persistent subscription.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		[Obsolete("SubscribeAsync is no longer supported. Use SubscribeToStream with manual acks instead.", false)]
		public async Task<PersistentSubscription> SubscribeAsync(
			string streamName, string groupName,
			Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
			Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
			UserCredentials? userCredentials = null, int bufferSize = 10, bool autoAck = true,
			CancellationToken cancellationToken = default
		) {
			if (autoAck) {
				throw new InvalidOperationException(
					$"AutoAck is no longer supported. Please use {nameof(SubscribeToStream)} with manual acks instead."
				);
			}

			return await PersistentSubscription
				.Confirm(
					SubscribeToStream(streamName, groupName, bufferSize, userCredentials, cancellationToken),
					eventAppeared,
					subscriptionDropped ?? delegate { },
					_log,
					userCredentials,
					cancellationToken
				)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Subscribes to a persistent subscription. Messages must be manually acknowledged
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public Task<PersistentSubscription> SubscribeToStreamAsync(
			string streamName, 
			string groupName,
			Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
			Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
			UserCredentials? userCredentials = null, 
			int bufferSize = 10,
			CancellationToken cancellationToken = default
		) {
			return SubscribeToStreamAsync(
				streamName,
				groupName,
				eventAppeared,
				new SubscribeToPersistentSubscriptionOptions {
					UserCredentials = userCredentials,
					BufferSize      = bufferSize
				},
				subscriptionDropped,
				cancellationToken
			);
		}

		/// <summary>
		/// Subscribes to a persistent subscription. Messages must be manually acknowledged
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task<PersistentSubscription> SubscribeToStreamAsync(
			string streamName, 
			string groupName,
			Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
			SubscribeToPersistentSubscriptionOptions options,
			Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
			CancellationToken cancellationToken = default
		) {
			return await PersistentSubscription
				.Confirm(
					SubscribeToStream(streamName, groupName, options, cancellationToken),
					eventAppeared,
					subscriptionDropped ?? delegate { },
					_log,
					options.UserCredentials,
					cancellationToken
				)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Subscribes to a persistent subscription. Messages must be manually acknowledged.
		/// </summary>
		/// <param name="streamName">The name of the stream to read events from.</param>
		/// <param name="groupName">The name of the persistent subscription group.</param>
		/// <param name="bufferSize">The size of the buffer.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public PersistentSubscriptionResult SubscribeToStream(
			string streamName, 
			string groupName, 
			int bufferSize = 10,
			UserCredentials? userCredentials = null, 
			CancellationToken cancellationToken = default
		) {
			return SubscribeToStream(
				streamName,
				groupName,
				new SubscribeToPersistentSubscriptionOptions {
					BufferSize      = bufferSize,
					UserCredentials = userCredentials
				},
				cancellationToken
			);
		}

		/// <summary>
		/// Subscribes to a persistent subscription. Messages must be manually acknowledged.
		/// </summary>
		/// <param name="streamName">The name of the stream to read events from.</param>
		/// <param name="groupName">The name of the persistent subscription group.</param>
		/// <param name="options">Optional settings to configure subscription</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public PersistentSubscriptionResult SubscribeToStream(
			string streamName, string groupName, 
			SubscribeToPersistentSubscriptionOptions options, 
			CancellationToken cancellationToken = default
		) {
			if (streamName == null) {
				throw new ArgumentNullException(nameof(streamName));
			}

			if (groupName == null) {
				throw new ArgumentNullException(nameof(groupName));
			}

			if (streamName == string.Empty) {
				throw new ArgumentException($"{nameof(streamName)} may not be empty.", nameof(streamName));
			}

			if (groupName == string.Empty) {
				throw new ArgumentException($"{nameof(groupName)} may not be empty.", nameof(groupName));
			}

			if (options.BufferSize <= 0) {
				throw new ArgumentOutOfRangeException(nameof(options.BufferSize));
			}

			var readOptions = new ReadReq.Types.Options {
				BufferSize = options.BufferSize,
				GroupName  = groupName,
				UuidOption = new ReadReq.Types.Options.Types.UUIDOption { Structured = new Empty() }
			};

			if (streamName == SystemStreams.AllStream) {
				readOptions.All = new Empty();
			} else {
				readOptions.StreamIdentifier = streamName;
			}

			return new PersistentSubscriptionResult(
				streamName,
				groupName,
				async ct => {
					var channelInfo = await GetChannelInfo(ct).ConfigureAwait(false);

					if (streamName == SystemStreams.AllStream &&
					    !channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsToAll) {
						throw new NotSupportedException(
							"The server does not support persistent subscriptions to $all."
						);
					}

					return channelInfo;
				},
				new() { Options = readOptions },
				Settings,
				options.UserCredentials,
				new DeserializationContext(
					_schemaRegistry,
					Settings.Serialization.AutomaticDeserialization
				),
				cancellationToken
			);
		}

		/// <summary>
		/// Subscribes to a persistent subscription to $all. Messages must be manually acknowledged
		/// </summary>
		public async Task<PersistentSubscription> SubscribeToAllAsync(
			string groupName,
			Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
			Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
			UserCredentials? userCredentials = null, int bufferSize = 10,
			CancellationToken cancellationToken = default
		) =>
			await SubscribeToStreamAsync(
					SystemStreams.AllStream,
					groupName,
					eventAppeared,
					subscriptionDropped,
					userCredentials,
					bufferSize,
					cancellationToken
				)
				.ConfigureAwait(false);

		/// <summary>
		/// Subscribes to a persistent subscription to $all. Messages must be manually acknowledged.
		/// </summary>
		/// <param name="groupName">The name of the persistent subscription group.</param>
		/// <param name="bufferSize">The size of the buffer.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public PersistentSubscriptionResult SubscribeToAll(
			string groupName, int bufferSize = 10,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default
		) =>
			SubscribeToStream(SystemStreams.AllStream, groupName, bufferSize, userCredentials, cancellationToken);

		/// <inheritdoc />
		public class PersistentSubscriptionResult : IAsyncEnumerable<ResolvedEvent>, IAsyncDisposable, IDisposable {
			const int MaxEventIdLength = 2000;

			readonly ReadReq                                _request;
			readonly Channel<PersistentSubscriptionMessage> _channel;
			readonly CancellationTokenSource                _cts;
			readonly CallOptions                            _callOptions;

			AsyncDuplexStreamingCall<ReadReq, ReadResp>? _call;
			int                                          _messagesEnumerated;

			/// <summary>
			/// The server-generated unique identifier for the subscription.
			/// </summary>
			public string? SubscriptionId { get; private set; }

			/// <summary>
			/// The name of the stream to read events from.
			/// </summary>
			public string StreamName { get; }

			/// <summary>
			/// The name of the persistent subscription group.
			/// </summary>
			public string GroupName { get; }

			/// <summary>
			/// An <see cref="IAsyncEnumerable{PersistentSubscriptionMessage}"/>. Do not enumerate more than once.
			/// </summary>
			public IAsyncEnumerable<PersistentSubscriptionMessage> Messages {
				get {
					if (Interlocked.Exchange(ref _messagesEnumerated, 1) == 1)
						throw new InvalidOperationException("Messages may only be enumerated once.");

					return GetMessages();

					async IAsyncEnumerable<PersistentSubscriptionMessage> GetMessages() {
						try {
							await foreach (var message in _channel.Reader.ReadAllAsync(_cts.Token)) {
								if (message is PersistentSubscriptionMessage.SubscriptionConfirmation(var subscriptionId
								    ))
									SubscriptionId = subscriptionId;

								yield return message;
							}
						} finally {
							_cts.Cancel();
						}
					}
				}
			}

			internal PersistentSubscriptionResult(
				string streamName,
				string groupName,
				Func<CancellationToken, Task<ChannelInfo>> selectChannelInfo,
				ReadReq request,
				KurrentClientSettings settings,
				UserCredentials? userCredentials,
				DeserializationContext deserializationContext,
				CancellationToken cancellationToken
			) {
				StreamName = streamName;
				GroupName  = groupName;

				_request = request;

				_callOptions = KurrentCallOptions.CreateStreaming(
					settings,
					userCredentials: userCredentials,
					cancellationToken: cancellationToken
				);

				_channel = Channel.CreateBounded<PersistentSubscriptionMessage>(ReadBoundedChannelOptions);

				_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

				_ = PumpMessages();

				return;

				async Task PumpMessages() {
					try {
						var channelInfo = await selectChannelInfo(_cts.Token).ConfigureAwait(false);
						var client      = new PersistentSubscriptionsClient(channelInfo.CallInvoker);

						_call = client.Read(_callOptions);

						await _call.RequestStream.WriteAsync(_request).ConfigureAwait(false);

						await foreach (var response in _call.ResponseStream.ReadAllAsync(_cts.Token)
							               .ConfigureAwait(false)) {
							PersistentSubscriptionMessage subscriptionMessage = response.ContentCase switch {
								SubscriptionConfirmation => new PersistentSubscriptionMessage.SubscriptionConfirmation(
									response.SubscriptionConfirmation.SubscriptionId
								),
								Event => new PersistentSubscriptionMessage.Event(
									ConvertToResolvedEvent(response, deserializationContext),
									response.Event.CountCase switch {
										ReadResp.Types.ReadEvent.CountOneofCase.RetryCount => response.Event.RetryCount,
										_                                                  => null
									}
								),
								_ => PersistentSubscriptionMessage.Unknown.Instance
							};

							if (subscriptionMessage is PersistentSubscriptionMessage.Event evnt)
								KurrentClientDiagnostics.ActivitySource.TraceSubscriptionEvent(
									SubscriptionId,
									evnt.ResolvedEvent,
									channelInfo,
									settings,
									userCredentials
								);

							await _channel.Writer.WriteAsync(subscriptionMessage, _cts.Token).ConfigureAwait(false);
						}

						_channel.Writer.TryComplete();
					} catch (Exception ex) {
#if NET48
						switch (ex) {
							// The gRPC client for .NET 48 uses WinHttpHandler under the hood for sending HTTP requests.
							// In certain scenarios, this can lead to exceptions of type WinHttpException being thrown.
							// One such scenario is when the server abruptly closes the connection, which results in a WinHttpException with the error code 12030.
							// Additionally, there are cases where the server response does not include the 'grpc-status' header.
							// The absence of this header leads to an RpcException with the status code 'Cancelled' and the message "No grpc-status found on response".
							// The switch statement below handles these specific exceptions and translates them into the appropriate
							// PersistentSubscriptionDroppedByServerException exception.
							case RpcException { StatusCode: StatusCode.Unavailable } rex1
								when rex1.Status.Detail.Contains("WinHttpException: Error 12030"):
							case RpcException { StatusCode: StatusCode.Cancelled } rex2
								when rex2.Status.Detail.Contains("No grpc-status found on response"):
								ex = new PersistentSubscriptionDroppedByServerException(StreamName, GroupName, ex);
								break;
						}
#endif
						if (ex is PersistentSubscriptionNotFoundException) {
							await _channel.Writer
								.WriteAsync(PersistentSubscriptionMessage.NotFound.Instance, cancellationToken)
								.ConfigureAwait(false);

							_channel.Writer.TryComplete();
							return;
						}

						_channel.Writer.TryComplete(ex);
					}
				}
			}

			/// <summary>
			/// Acknowledge that a message has completed processing (this will tell the server it has been processed).
			/// </summary>
			/// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
			/// <param name="eventIds">The <see cref="Uuid"/> of the <see cref="ResolvedEvent" />s to acknowledge. There should not be more than 2000 to ack at a time.</param>
			public Task Ack(params Uuid[] eventIds) => AckInternal(eventIds);

			/// <summary>
			/// Acknowledge that a message has completed processing (this will tell the server it has been processed).
			/// </summary>
			/// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
			/// <param name="eventIds">The <see cref="Uuid"/> of the <see cref="ResolvedEvent" />s to acknowledge. There should not be more than 2000 to ack at a time.</param>
			public Task Ack(IEnumerable<Uuid> eventIds) => Ack(eventIds.ToArray());

			/// <summary>
			/// Acknowledge that a message has completed processing (this will tell the server it has been processed).
			/// </summary>
			/// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
			/// <param name="resolvedEvents">The <see cref="ResolvedEvent"></see>s to acknowledge. There should not be more than 2000 to ack at a time.</param>
			public Task Ack(params ResolvedEvent[] resolvedEvents) =>
				Ack(Array.ConvertAll(resolvedEvents, resolvedEvent => resolvedEvent.OriginalEvent.EventId));

			/// <summary>
			/// Acknowledge that a message has completed processing (this will tell the server it has been processed).
			/// </summary>
			/// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
			/// <param name="resolvedEvents">The <see cref="ResolvedEvent"></see>s to acknowledge. There should not be more than 2000 to ack at a time.</param>
			public Task Ack(IEnumerable<ResolvedEvent> resolvedEvents) =>
				Ack(resolvedEvents.Select(resolvedEvent => resolvedEvent.OriginalEvent.EventId));

			/// <summary>
			/// Acknowledge that a message has failed processing (this will tell the server it has not been processed).
			/// </summary>
			/// <param name="action">The <see cref="PersistentSubscriptionNakEventAction"/> to take.</param>
			/// <param name="reason">A reason given.</param>
			/// <param name="eventIds">The <see cref="Uuid"/> of the <see cref="ResolvedEvent" />s to nak. There should not be more than 2000 to nak at a time.</param>
			/// <exception cref="ArgumentException">The number of eventIds exceeded the limit of 2000.</exception>
			public Task Nack(PersistentSubscriptionNakEventAction action, string reason, params Uuid[] eventIds) =>
				NackInternal(eventIds, action, reason);

			/// <summary>
			/// Acknowledge that a message has failed processing (this will tell the server it has not been processed).
			/// </summary>
			/// <param name="action">The <see cref="PersistentSubscriptionNakEventAction"/> to take.</param>
			/// <param name="reason">A reason given.</param>
			/// <param name="resolvedEvents">The <see cref="ResolvedEvent" />s to nak. There should not be more than 2000 to nak at a time.</param>
			/// <exception cref="ArgumentException">The number of resolvedEvents exceeded the limit of 2000.</exception>
			public Task Nack(
				PersistentSubscriptionNakEventAction action, string reason, params ResolvedEvent[] resolvedEvents
			) =>
				Nack(action, reason, Array.ConvertAll(resolvedEvents, re => re.OriginalEvent.EventId));

			static ResolvedEvent ConvertToResolvedEvent(
				ReadResp response,
				DeserializationContext deserializationContext
			) =>
				ResolvedEvent.From(
					ConvertToEventRecord(response.Event.Event)!,
					ConvertToEventRecord(response.Event.Link),
					response.Event.PositionCase switch {
						ReadResp.Types.ReadEvent.PositionOneofCase.CommitPosition => response.Event.CommitPosition,
						_                                                         => null
					},
					deserializationContext
				);

			Task AckInternal(params Uuid[] eventIds) {
				if (eventIds.Length > MaxEventIdLength) {
					throw new ArgumentException(
						$"The number of eventIds exceeds the maximum length of {MaxEventIdLength}.",
						nameof(eventIds)
					);
				}

				return _call is null
					? throw new InvalidOperationException()
					: _call.RequestStream.WriteAsync(
						new ReadReq {
							Ack = new ReadReq.Types.Ack {
								Ids = {
									Array.ConvertAll(eventIds, id => id.ToDto())
								}
							}
						}
					);
			}

			Task NackInternal(Uuid[] eventIds, PersistentSubscriptionNakEventAction action, string reason) {
				if (eventIds.Length > MaxEventIdLength) {
					throw new ArgumentException(
						$"The number of eventIds exceeds the maximum length of {MaxEventIdLength}.",
						nameof(eventIds)
					);
				}

				return _call is null
					? throw new InvalidOperationException()
					: _call.RequestStream.WriteAsync(
						new ReadReq {
							Nack = new ReadReq.Types.Nack {
								Ids = {
									Array.ConvertAll(eventIds, id => id.ToDto())
								},
								Action = action switch {
									PersistentSubscriptionNakEventAction.Park => ReadReq.Types.Nack.Types.Action.Park,
									PersistentSubscriptionNakEventAction.Retry => ReadReq.Types.Nack.Types.Action.Retry,
									PersistentSubscriptionNakEventAction.Skip => ReadReq.Types.Nack.Types.Action.Skip,
									PersistentSubscriptionNakEventAction.Stop => ReadReq.Types.Nack.Types.Action.Stop,
									_ => ReadReq.Types.Nack.Types.Action.Unknown
								},
								Reason = reason
							}
						}
					);
			}

			static EventRecord? ConvertToEventRecord(ReadResp.Types.ReadEvent.Types.RecordedEvent? e) =>
				e is null
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

			/// <inheritdoc />
			public async ValueTask DisposeAsync() {
				await CastAndDispose(_cts).ConfigureAwait(false);
				await CastAndDispose(_call).ConfigureAwait(false);

				return;

				static async Task CastAndDispose(IDisposable? resource) {
					switch (resource) {
						case null:
							return;

						case IAsyncDisposable resourceAsyncDisposable:
							await resourceAsyncDisposable.DisposeAsync().ConfigureAwait(false);
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
				await foreach (var message in Messages.WithCancellation(cancellationToken)) {
					if (message is not PersistentSubscriptionMessage.Event(var resolvedEvent, _))
						continue;

					yield return resolvedEvent;
				}
			}
		}
	}
}
