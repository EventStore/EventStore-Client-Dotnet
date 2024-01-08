using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EventStore.Client.Streams;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using static EventStore.Client.SubscriptionState;

namespace EventStore.Client {
	public partial class EventStoreClient {
		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="start">A <see cref="FromAll"/> (exclusive of) to start the subscription from.</param>
		/// <param name="eventAppeared">A Task invoked and awaited when a new event is received over the subscription.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
		/// <param name="caughtUp">An action invoked if the subscription reached the head of the stream.</param>
		/// <param name="fellBehind">An action invoked if the subscription has fallen behind</param>
		/// <param name="filterOptions">The optional <see cref="SubscriptionFilterOptions"/> to apply.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamSubscription> SubscribeToAllAsync(
			FromAll start,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			Func<StreamSubscription, CancellationToken, Task>? caughtUp = default,
			Func<StreamSubscription, CancellationToken, Task>? fellBehind = default,
			SubscriptionFilterOptions? filterOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) => StreamSubscription.Confirm(ReadInternal(new ReadReq {
				Options = new ReadReq.Types.Options {
					ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
					ResolveLinks = resolveLinkTos,
					All = ReadReq.Types.Options.Types.AllOptions.FromSubscriptionPosition(start),
					Subscription = new ReadReq.Types.Options.Types.SubscriptionOptions(),
					Filter = GetFilterOptions(filterOptions)!
				}
			}, userCredentials, cancellationToken), eventAppeared, subscriptionDropped, caughtUp, fellBehind, _log,
			filterOptions?.CheckpointReached, cancellationToken);

		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="start">A <see cref="FromAll"/> (exclusive of) to start the subscription from.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="filterOptions">The optional <see cref="SubscriptionFilterOptions"/> to apply.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns>An instance of SubscriptionResult which contains current state of the subscription and an enumerator to consume messages</returns>
		public SubscriptionResult SubscribeToAll(
			FromAll start, bool resolveLinkTos = false, SubscriptionFilterOptions? filterOptions = null, UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			return new SubscriptionResult(async _ => {
				var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
				return channelInfo.CallInvoker;
			}, new ReadReq {
				Options = new ReadReq.Types.Options {
					ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
					ResolveLinks = resolveLinkTos,
					All = ReadReq.Types.Options.Types.AllOptions.FromSubscriptionPosition(start),
					Subscription = new ReadReq.Types.Options.Types.SubscriptionOptions(),
					Filter = GetFilterOptions(filterOptions)!
				}
			}, Settings, userCredentials, cancellationToken, _log);
		}

		/// <summary>
		/// Subscribes to a stream from a <see cref="StreamPosition">checkpoint</see>.
		/// </summary>
		/// <param name="start">A <see cref="FromStream"/> (exclusive of) to start the subscription from.</param>
		/// <param name="streamName">The name of the stream to read events from.</param>
		/// <param name="eventAppeared">A Task invoked and awaited when a new event is received over the subscription.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
		/// <param name="caughtUp">An action invoked if the subscription reached the head of the stream.</param>
		/// <param name="fellBehind">An action is invoked if the subscription has fallen behind</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamSubscription> SubscribeToStreamAsync(string streamName,
			FromStream start,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			Func<StreamSubscription, CancellationToken, Task>? caughtUp = default,
			Func<StreamSubscription, CancellationToken, Task>? fellBehind = default,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) => StreamSubscription.Confirm(ReadInternal(new ReadReq {
				Options = new ReadReq.Types.Options {
					ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
					ResolveLinks = resolveLinkTos,
					Stream = ReadReq.Types.Options.Types.StreamOptions.FromSubscriptionPosition(streamName, start),
					Subscription = new ReadReq.Types.Options.Types.SubscriptionOptions()
				}
			}, userCredentials, cancellationToken), eventAppeared, subscriptionDropped, caughtUp, fellBehind, _log,
			cancellationToken: cancellationToken);

		/// <summary>
		/// Subscribes to a stream from a <see cref="StreamPosition">checkpoint</see>.
		/// </summary>
		/// <param name="start">A <see cref="FromStream"/> (exclusive of) to start the subscription from.</param>
		/// <param name="streamName">The name of the stream to read events from.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns>An instance of SubscriptionResult which contains current state of the subscription and an enumerator to consume messages</returns>
		public SubscriptionResult SubscribeToStream(string streamName,
			FromStream start, bool resolveLinkTos = false,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			return new SubscriptionResult(async _ => {
				var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
				return channelInfo.CallInvoker;
			}, new ReadReq {
				Options = new ReadReq.Types.Options {
					ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
					ResolveLinks = resolveLinkTos,
					Stream = ReadReq.Types.Options.Types.StreamOptions.FromSubscriptionPosition(streamName, start),
					Subscription = new ReadReq.Types.Options.Types.SubscriptionOptions(),
				}
			}, Settings, userCredentials, cancellationToken, _log);
		}

		/// <summary>
		/// A class which represents current subscription state and an enumerator to consume messages
		/// </summary>
		public class SubscriptionResult {
			private readonly Channel<StreamMessage> _internalChannel;
			private readonly CancellationTokenSource _cts;
			private int _messagesEnumerated;
			private ILogger _log;
			/// <summary>
			/// The name of the stream.
			/// </summary>
			public string StreamName { get; }

			/// <summary>
			///
			/// </summary>
			public Position StreamPosition { get; private set; }

			/// <summary>
			/// Represents subscription ID for the current subscription
			/// </summary>
			public string? SubscriptionId { get; private set; }

			/// <summary>
			/// Current subscription state
			/// </summary>

			public SubscriptionState SubscriptionState {
				get {
					if (_exceptionInternal is not null) {
						throw _exceptionInternal;
					}

					return _subscriptionStateInternal;
				}
			}

			private volatile SubscriptionState _subscriptionStateInternal;

			private volatile Exception? _exceptionInternal;

			/// <summary>
			/// An <see cref="IAsyncEnumerable{StreamMessage}"/>. Do not enumerate more than once.
			/// </summary>
			public IAsyncEnumerable<StreamMessage> Messages {
				get {
					return GetMessages();

					async IAsyncEnumerable<StreamMessage> GetMessages() {
						if (Interlocked.Exchange(ref _messagesEnumerated, 1) == 1) {
							throw new InvalidOperationException("Messages may only be enumerated once.");
						}

						try {
							await foreach (var message in _internalChannel.Reader.ReadAllAsync()
								               .ConfigureAwait(false)) {
								if (!message.IsSubscriptionMessage()) {
									continue;
								}

								switch (message) {
									case StreamMessage.SubscriptionMessage.SubscriptionConfirmation(var
										subscriptionId):
										SubscriptionId = subscriptionId;
										continue;
									case StreamMessage.SubscriptionMessage.Checkpoint(var position):
										StreamPosition = position;
										break;
								}

								yield return message;
							}
						} finally {
							Dispose();
						}
					}
				}
			}

			/// <summary>
			/// Terminates subscription
			/// </summary>
			public void Dispose() {
				if (_subscriptionStateInternal == Disposed) {
					return;
				}
				_subscriptionStateInternal = Disposed;
				_cts.Cancel();
			}

			internal SubscriptionResult(Func<CancellationToken, Task<CallInvoker>> selectCallInvoker, ReadReq request,
				EventStoreClientSettings settings, UserCredentials? userCredentials,
				CancellationToken cancellationToken, ILogger log) {
				Sanitize(request);
				Validate(request);

				_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

				var callOptions = EventStoreCallOptions.CreateStreaming(settings, userCredentials: userCredentials,
					cancellationToken: _cts.Token);

				_internalChannel = Channel.CreateBounded<StreamMessage>(new BoundedChannelOptions(1) {
					SingleReader = true,
					SingleWriter = true,
					AllowSynchronousContinuations = true
				});

				_log = log;

				StreamName = request.Options.All != null
					? SystemStreams.AllStream
					: request.Options.Stream.StreamIdentifier!;

				_subscriptionStateInternal = Initializing;

				_ = PumpMessages(selectCallInvoker, request, callOptions);
			}

			async Task PumpMessages(Func<CancellationToken, Task<CallInvoker>> selectCallInvoker, ReadReq request, CallOptions callOptions) {
				var firstMessageRead = false;
				var callInvoker = await selectCallInvoker(_cts.Token).ConfigureAwait(false);
				var streamsClient = new Streams.Streams.StreamsClient(callInvoker);
				try {
					using var call = streamsClient.Read(request, callOptions);

					await foreach (var response in call.ResponseStream
						               .ReadAllAsync(_cts.Token)
						               .WithCancellation(_cts.Token)
						               .ConfigureAwait(false)) {
						if (response is null) {
							continue;
						}

						var message = ConvertResponseToMessage(response);
						if (!firstMessageRead) {
							firstMessageRead = true;

							if (message is not StreamMessage.SubscriptionMessage.SubscriptionConfirmation) {
								throw new InvalidOperationException(
									$"Subscription to {StreamName} could not be confirmed.");
							}

							_subscriptionStateInternal = Ok;
						}

						var messageToWrite = message.IsSubscriptionMessage()
							? message
							: StreamMessage.Unknown.Instance;
						await _internalChannel.Writer.WriteAsync(messageToWrite, _cts.Token).ConfigureAwait(false);

						if (messageToWrite is StreamMessage.NotFound) {
							_exceptionInternal = new StreamNotFoundException(StreamName);
							break;
						}
					}
				} catch (RpcException ex) when (ex.Status.StatusCode == StatusCode.Cancelled &&
				                                ex.Status.Detail.Contains("Call canceled by the client.")) {
					_log.LogInformation(
						"Subscription {subscriptionId} was dropped because cancellation was requested by the client.",
						SubscriptionId);
				} catch (Exception ex) {
					if (ex is ObjectDisposedException or OperationCanceledException) {
						_log.LogWarning(
							ex,
							"Subscription {subscriptionId} was dropped because cancellation was requested by another caller.",
							SubscriptionId
						);
					} else {
						_exceptionInternal = ex;
					}
				} finally {
					_internalChannel.Writer.Complete();
				}
			}

			private static void Sanitize(ReadReq request) {
				if (request.Options.Filter == null) {
					request.Options.NoFilter = new Empty();
				}

				request.Options.UuidOption = new ReadReq.Types.Options.Types.UUIDOption {Structured = new Empty()};
			}

			private static void Validate(ReadReq request) {
				if (request.Options.CountOptionCase == ReadReq.Types.Options.CountOptionOneofCase.Count &&
				    request.Options.Count <= 0) {
					throw new ArgumentOutOfRangeException("count");
				}

				var streamOptions = request.Options.Stream;
				var allOptions = request.Options.All;

				if (allOptions == null && streamOptions == null) {
					throw new ArgumentException("No stream provided to subscribe");
				}

				if (allOptions != null && streamOptions != null) {
					throw new ArgumentException($"Cannot subscribe both ${SystemStreams.AllStream}, and ${streamOptions.StreamIdentifier}");
				}
			}
		}
	}
}
