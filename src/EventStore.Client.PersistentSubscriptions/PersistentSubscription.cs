using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.PersistentSubscriptions;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace EventStore.Client {
	/// <summary>
	/// Represents a persistent subscription connection.
	/// </summary>
	public class PersistentSubscription : IDisposable {
		private readonly Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> _eventAppeared;
		private readonly Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>      _subscriptionDropped;
		private readonly IDisposable                                                                _disposable;
		private readonly CancellationToken                                                          _cancellationToken;
		private readonly AsyncDuplexStreamingCall<ReadReq, ReadResp>                                _call;
		private readonly ILogger                                                                    _log;

		private int _subscriptionDroppedInvoked;

		/// <summary>
		/// The Subscription Id.
		/// </summary>
		public string SubscriptionId { get; }

		internal static async Task<PersistentSubscription> Confirm(
			ChannelBase channel, CallInvoker callInvoker, EventStoreClientSettings settings, 
			UserCredentials? userCredentials, ReadReq.Types.Options options, ILogger log,
			Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
			Action<PersistentSubscription, SubscriptionDroppedReason, Exception?> subscriptionDropped,
			CancellationToken cancellationToken = default) {

			var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			var call = new PersistentSubscriptions.PersistentSubscriptions.PersistentSubscriptionsClient(callInvoker)
				.Read(EventStoreCallOptions.CreateStreaming(settings, userCredentials: userCredentials,
					cancellationToken: cts.Token));

			await call.RequestStream.WriteAsync(new ReadReq {
				Options = options
			}).ConfigureAwait(false);

			if (await call.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false) &&
			    call.ResponseStream.Current.ContentCase == ReadResp.ContentOneofCase.SubscriptionConfirmation)
				return new PersistentSubscription(call, log, eventAppeared, subscriptionDropped, cts.Token, cts);

			call.Dispose();
			cts.Dispose();
			throw new InvalidOperationException("Subscription could not be confirmed.");
		}

		// PersistentSubscription takes responsibility for disposing the call and the disposable
		private PersistentSubscription(
			AsyncDuplexStreamingCall<ReadReq, ReadResp> call,
			ILogger log,
			Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
			Action<PersistentSubscription, SubscriptionDroppedReason, Exception?> subscriptionDropped,
			CancellationToken cancellationToken,
			IDisposable disposable) {
			_call                = call;
			_eventAppeared       = eventAppeared;
			_subscriptionDropped = subscriptionDropped;
			_cancellationToken   = cancellationToken;
			_disposable          = disposable;
			_log                 = log;
			SubscriptionId       = call.ResponseStream.Current.SubscriptionConfirmation.SubscriptionId;
			Task.Run(Subscribe);
		}

		/// <summary>
		/// Acknowledge that a message has completed processing (this will tell the server it has been processed).
		/// </summary>
		/// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
		/// <param name="eventIds">The <see cref="Uuid"/> of the <see cref="ResolvedEvent" />s to acknowledge. There should not be more than 2000 to ack at a time.</param>
		public Task Ack(params Uuid[] eventIds) {
			if (eventIds.Length > 2000) {
				throw new ArgumentException();
			}

			return AckInternal(eventIds);
		}

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
		public Task Nack(PersistentSubscriptionNakEventAction action, string reason, params Uuid[] eventIds) {
			if (eventIds.Length > 2000) {
				throw new ArgumentException();
			}

			return NackInternal(eventIds, action, reason);
		}

		/// <summary>
		/// Acknowledge that a message has failed processing (this will tell the server it has not been processed).
		/// </summary>
		/// <param name="action">The <see cref="PersistentSubscriptionNakEventAction"/> to take.</param>
		/// <param name="reason">A reason given.</param>
		/// <param name="resolvedEvents">The <see cref="ResolvedEvent" />s to nak. There should not be more than 2000 to nak at a time.</param>
		/// <exception cref="ArgumentException">The number of resolvedEvents exceeded the limit of 2000.</exception>
		public Task Nack(PersistentSubscriptionNakEventAction action, string reason,
		                 params ResolvedEvent[] resolvedEvents) =>
			Nack(action, reason,
				Array.ConvertAll(resolvedEvents, resolvedEvent => resolvedEvent.OriginalEvent.EventId));

		/// <inheritdoc />
		public void Dispose() => SubscriptionDropped(SubscriptionDroppedReason.Disposed);

		private async Task Subscribe() {
			_log.LogDebug("Persistent Subscription {subscriptionId} confirmed.", SubscriptionId);

			try {
				await foreach (var response in _call.ResponseStream.ReadAllAsync(_cancellationToken).ConfigureAwait(false)) {
					if (response.ContentCase != ReadResp.ContentOneofCase.Event) {
						continue;
					}

					var resolvedEvent = ConvertToResolvedEvent(response);
					_log.LogTrace(
						"Persistent Subscription {subscriptionId} received event {streamName}@{streamRevision} {position}",
						SubscriptionId, resolvedEvent.OriginalEvent.EventStreamId,
						resolvedEvent.OriginalEvent.EventNumber, resolvedEvent.OriginalEvent.Position);

					try {
						await _eventAppeared(
							this,
							resolvedEvent,
							response.Event.CountCase switch {
								ReadResp.Types.ReadEvent.CountOneofCase.RetryCount => response.Event
									.RetryCount,
								_ => default
							},
							_cancellationToken).ConfigureAwait(false);

					} catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException) {
						if (_subscriptionDroppedInvoked != 0) {
							return;
						}

						_log.LogWarning(ex,
							"Persistent Subscription {subscriptionId} was dropped because cancellation was requested by another caller.",
							SubscriptionId);

						SubscriptionDropped(SubscriptionDroppedReason.Disposed);

						return;
					} catch (Exception ex) {
						_log.LogError(ex,
							"Persistent Subscription {subscriptionId} was dropped because the subscriber made an error.",
							SubscriptionId);
						SubscriptionDropped(SubscriptionDroppedReason.SubscriberError, ex);

						return;
					}
				}
			} catch (Exception ex) {
				if (_subscriptionDroppedInvoked == 0) {
#if NET48
					// The gRPC client for .NET 48 uses WinHttpHandler under the hood for sending HTTP requests.
					// In certain scenarios, this can lead to exceptions of type WinHttpException being thrown.
					// One such scenario is when the server abruptly closes the connection, which results in a WinHttpException with the error code 12030.
					// Additionally, there are cases where the server response does not include the 'grpc-status' header.
					// The absence of this header leads to an RpcException with the status code 'Cancelled' and the message "No grpc-status found on response".
					// The switch statement below handles these specific exceptions and translates them into a PersistentSubscriptionDroppedByServerException
					// which is what our tests expect. The downside of this approach is that it does not return the stream name and group name.
					if (ex is RpcException { StatusCode: StatusCode.Unavailable } rex1 && rex1.Status.Detail.Contains("WinHttpException: Error 12030"))  {
						ex = new PersistentSubscriptionDroppedByServerException("", "", ex);
					} else if (ex is RpcException { StatusCode: StatusCode.Cancelled } rex2
					        && rex2.Status.Detail.Contains("No grpc-status found on response")) {
						ex = new PersistentSubscriptionDroppedByServerException("", "", ex);
					}
#endif
					_log.LogError(ex,
						"Persistent Subscription {subscriptionId} was dropped because an error occurred on the server.",
						SubscriptionId);
					SubscriptionDropped(SubscriptionDroppedReason.ServerError, ex);
				}
			} finally {
				if (_subscriptionDroppedInvoked == 0) {
					_log.LogError(
						"Persistent Subscription {subscriptionId} was unexpectedly terminated.",
						SubscriptionId);
					SubscriptionDropped(SubscriptionDroppedReason.ServerError);
				}
			}

			ResolvedEvent ConvertToResolvedEvent(ReadResp response) => new(
				ConvertToEventRecord(response.Event.Event)!,
				ConvertToEventRecord(response.Event.Link),
				response.Event.PositionCase switch {
					ReadResp.Types.ReadEvent.PositionOneofCase.CommitPosition => response.Event.CommitPosition,
					_                                                         => null
				});

			EventRecord? ConvertToEventRecord(ReadResp.Types.ReadEvent.Types.RecordedEvent? e) =>
				e == null
					? null
					: new EventRecord(
						e.StreamIdentifier!,
						Uuid.FromDto(e.Id),
						new StreamPosition(e.StreamRevision),
						new Position(e.CommitPosition, e.PreparePosition),
						e.Metadata,
						e.Data.ToByteArray(),
						e.CustomMetadata.ToByteArray());
		}

		private void SubscriptionDropped(SubscriptionDroppedReason reason, Exception? ex = null) {
			if (Interlocked.CompareExchange(ref _subscriptionDroppedInvoked, 1, 0) == 1) {
				return;
			}

			try {
				_subscriptionDropped.Invoke(this, reason, ex);
			} finally {
				_call.Dispose();
				_disposable.Dispose();
			}
		}

		private Task AckInternal(params Uuid[] ids) =>
			_call.RequestStream.WriteAsync(new ReadReq {
				Ack = new ReadReq.Types.Ack {
					Ids = {
						Array.ConvertAll(ids, id => id.ToDto())
					}
				}
			});

		private Task NackInternal(Uuid[] ids, PersistentSubscriptionNakEventAction action, string reason) =>
			_call!.RequestStream.WriteAsync(new ReadReq {
				Nack = new ReadReq.Types.Nack {
					Ids = {
						Array.ConvertAll(ids, id => id.ToDto())
					},
					Action = action switch {
						PersistentSubscriptionNakEventAction.Park  => ReadReq.Types.Nack.Types.Action.Park,
						PersistentSubscriptionNakEventAction.Retry => ReadReq.Types.Nack.Types.Action.Retry,
						PersistentSubscriptionNakEventAction.Skip  => ReadReq.Types.Nack.Types.Action.Skip,
						PersistentSubscriptionNakEventAction.Stop  => ReadReq.Types.Nack.Types.Action.Stop,
						_                                          => ReadReq.Types.Nack.Types.Action.Unknown
					},
					Reason = reason
				}
			});
	}
}