using EventStore.Client.PersistentSubscriptions;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace EventStore.Client {
	/// <summary>
	/// Represents a persistent subscription connection.
	/// </summary>
	public class PersistentSubscription : IDisposable {
		private readonly KurrentPersistentSubscriptionsClient.PersistentSubscriptionResult _persistentSubscriptionResult;
		private readonly IAsyncEnumerator<PersistentSubscriptionMessage> _enumerator;
		private readonly Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> _eventAppeared;
		private readonly Action<PersistentSubscription, SubscriptionDroppedReason, Exception?> _subscriptionDropped;
		private readonly ILogger _log;
		private readonly CancellationTokenSource _cts;

		private int _subscriptionDroppedInvoked;

		/// <summary>
		/// The Subscription Id.
		/// </summary>
		public string SubscriptionId { get; }

		internal static async Task<PersistentSubscription> Confirm(
			KurrentPersistentSubscriptionsClient.PersistentSubscriptionResult persistentSubscriptionResult,
			Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
			Action<PersistentSubscription, SubscriptionDroppedReason, Exception?> subscriptionDropped,
			ILogger log, UserCredentials? userCredentials, CancellationToken cancellationToken = default) {
			var enumerator = persistentSubscriptionResult
				.Messages
				.GetAsyncEnumerator(cancellationToken);

			var result = await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false);

			return (result, enumerator.Current) switch {
				(true, PersistentSubscriptionMessage.SubscriptionConfirmation (var subscriptionId)) =>
					new PersistentSubscription(persistentSubscriptionResult, enumerator, subscriptionId, eventAppeared,
						subscriptionDropped, log, cancellationToken),
				(true, PersistentSubscriptionMessage.NotFound) =>
					throw new PersistentSubscriptionNotFoundException(persistentSubscriptionResult.StreamName,
						persistentSubscriptionResult.GroupName),
				_ => throw new InvalidOperationException("Subscription could not be confirmed.")
			};
		}

		// PersistentSubscription takes responsibility for disposing the call and the disposable
		private PersistentSubscription(
			KurrentPersistentSubscriptionsClient.PersistentSubscriptionResult persistentSubscriptionResult,
			IAsyncEnumerator<PersistentSubscriptionMessage> enumerator, string subscriptionId,
			Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
			Action<PersistentSubscription, SubscriptionDroppedReason, Exception?> subscriptionDropped, ILogger log,
			CancellationToken cancellationToken) {
			_persistentSubscriptionResult = persistentSubscriptionResult;
			_enumerator = enumerator;
			SubscriptionId = subscriptionId;
			_eventAppeared = eventAppeared;
			_subscriptionDropped = subscriptionDropped;
			_log = log;
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			Task.Run(Subscribe, _cts.Token);
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
		public Task Nack(PersistentSubscriptionNakEventAction action, string reason, params Uuid[] eventIds) => NackInternal(eventIds, action, reason);

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
				while (await _enumerator.MoveNextAsync(_cts.Token).ConfigureAwait(false)) {
					if (_enumerator.Current is not PersistentSubscriptionMessage.Event(var resolvedEvent, var retryCount)) {
						continue;
					}

					if (_enumerator.Current is PersistentSubscriptionMessage.NotFound) {
						if (_subscriptionDroppedInvoked != 0) {
							return;
						}
						SubscriptionDropped(SubscriptionDroppedReason.ServerError,
							new PersistentSubscriptionNotFoundException(
								_persistentSubscriptionResult.StreamName, _persistentSubscriptionResult.GroupName));
						return;
					}
					
					_log.LogTrace(
						"Persistent Subscription {subscriptionId} received event {streamName}@{streamRevision} {position}",
						SubscriptionId, resolvedEvent.OriginalEvent.EventStreamId,
						resolvedEvent.OriginalEvent.EventNumber, resolvedEvent.OriginalEvent.Position);

					try {
						await _eventAppeared(
							this,
							resolvedEvent,
							retryCount,
							_cts.Token).ConfigureAwait(false);
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
		}

		private void SubscriptionDropped(SubscriptionDroppedReason reason, Exception? ex = null) {
			if (Interlocked.CompareExchange(ref _subscriptionDroppedInvoked, 1, 0) == 1) {
				return;
			}

			try {
				_subscriptionDropped.Invoke(this, reason, ex);
			} finally {
				_persistentSubscriptionResult.Dispose();
				_cts.Dispose();
			}
		}

		private Task AckInternal(params Uuid[] ids) => _persistentSubscriptionResult.Ack(ids);

		private Task NackInternal(Uuid[] ids, PersistentSubscriptionNakEventAction action, string reason) =>
			_persistentSubscriptionResult.Nack(action, reason, ids);
	}
}
