using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.PersistentSubscriptions;
using Grpc.Core;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// Represents a persistent subscription connection.
	/// </summary>
	public class PersistentSubscription : IDisposable {
		private readonly bool _autoAck;
		private readonly Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> _eventAppeared;
		private readonly Action<PersistentSubscription, SubscriptionDroppedReason, Exception?> _subscriptionDropped;
		private readonly CancellationTokenSource _disposed;
		private readonly AsyncDuplexStreamingCall<ReadReq, ReadResp> _call;
		private int _subscriptionDroppedInvoked;

		/// <summary>
		/// The Subscription Id.
		/// </summary>
		public string SubscriptionId { get; }

		internal static async Task<PersistentSubscription> Confirm(AsyncDuplexStreamingCall<ReadReq, ReadResp> call,
			ReadReq.Types.Options options, bool autoAck,
			Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
			Action<PersistentSubscription, SubscriptionDroppedReason, Exception?> subscriptionDropped,
			CancellationToken cancellationToken = default) {
			await call.RequestStream.WriteAsync(new ReadReq {
				Options = options
			}).ConfigureAwait(false);

			if (!await call.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false) ||
			    call.ResponseStream.Current.ContentCase != ReadResp.ContentOneofCase.SubscriptionConfirmation) {
				throw new InvalidOperationException();
			}

			return new PersistentSubscription(call, autoAck, eventAppeared, subscriptionDropped);
		}

		private PersistentSubscription(
			AsyncDuplexStreamingCall<ReadReq, ReadResp> call,
			bool autoAck,
			Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
			Action<PersistentSubscription, SubscriptionDroppedReason, Exception?> subscriptionDropped) {
			_call = call;
			_autoAck = autoAck;
			_eventAppeared = eventAppeared;
			_subscriptionDropped = subscriptionDropped;
			_disposed = new CancellationTokenSource();
			SubscriptionId = call.ResponseStream.Current.SubscriptionConfirmation.SubscriptionId;
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
		/// <returns></returns>
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
		/// <returns></returns>
		/// <exception cref="ArgumentException">The number of resolvedEvents exceeded the limit of 2000.</exception>
		public Task Nack(PersistentSubscriptionNakEventAction action, string reason,
			params ResolvedEvent[] resolvedEvents) =>
			Nack(action, reason,
				Array.ConvertAll(resolvedEvents, resolvedEvent => resolvedEvent.OriginalEvent.EventId));

		/// <inheritdoc />
		public void Dispose() {
			if (_disposed.IsCancellationRequested) {
				return;
			}

			SubscriptionDropped(SubscriptionDroppedReason.Disposed);

			_disposed.Dispose();
		}

		private async Task Subscribe() {
			try {
				while (await _call!.ResponseStream.MoveNext().ConfigureAwait(false) &&
				       !_disposed.IsCancellationRequested) {
					var current = _call!.ResponseStream.Current;
					switch (current.ContentCase) {
						case ReadResp.ContentOneofCase.Event:
							try {
								await _eventAppeared(this, ConvertToResolvedEvent(current),
									current.Event.CountCase switch {
										ReadResp.Types.ReadEvent.CountOneofCase.RetryCount => current.Event.RetryCount,
										_ => default
									}, _disposed.Token).ConfigureAwait(false);
								if (_autoAck) {
									await AckInternal(Uuid.FromDto(current.Event.Link?.Id ?? current.Event.Event.Id))
										.ConfigureAwait(false);
								}
							} catch (Exception ex) when (ex is ObjectDisposedException ||
							                             ex is OperationCanceledException) {
								SubscriptionDropped(SubscriptionDroppedReason.Disposed);
								return;
							} catch (Exception ex) {
								try {
									SubscriptionDropped(SubscriptionDroppedReason.SubscriberError, ex);
								} finally {
									_disposed.Cancel();
								}

								return;
							}

							break;
					}
				}
			} catch (Exception ex) {
				try {
					SubscriptionDropped(SubscriptionDroppedReason.ServerError, ex);
				} finally {
					_disposed.Cancel();
				}
			}

			ResolvedEvent ConvertToResolvedEvent(ReadResp response) => new ResolvedEvent(
				ConvertToEventRecord(response.Event.Event)!,
				ConvertToEventRecord(response.Event.Link),
				response.Event.PositionCase switch {
					ReadResp.Types.ReadEvent.PositionOneofCase.CommitPosition => response.Event.CommitPosition,
					_ => null
				});

			EventRecord? ConvertToEventRecord(ReadResp.Types.ReadEvent.Types.RecordedEvent e) =>
				e == null
					? null
					: new EventRecord(
						e.StreamIdentifier,
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

			_call?.Dispose();
			_subscriptionDropped?.Invoke(this, reason, ex);
			_disposed.Dispose();
		}

		private Task AckInternal(params Uuid[] ids) =>
			_call!.RequestStream.WriteAsync(new ReadReq {
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
						PersistentSubscriptionNakEventAction.Park => ReadReq.Types.Nack.Types.Action.Park,
						PersistentSubscriptionNakEventAction.Retry => ReadReq.Types.Nack.Types.Action.Retry,
						PersistentSubscriptionNakEventAction.Skip => ReadReq.Types.Nack.Types.Action.Skip,
						PersistentSubscriptionNakEventAction.Stop => ReadReq.Types.Nack.Types.Action.Stop,
						_ => ReadReq.Types.Nack.Types.Action.Unknown
					},
					Reason = reason
				}
			});
	}
}
