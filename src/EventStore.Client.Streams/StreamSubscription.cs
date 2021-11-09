using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// A class representing a <see cref="StreamSubscription"/>.
	/// </summary>
	public class StreamSubscription : IDisposable {
		private readonly IAsyncEnumerable<ResolvedEvent> _events;
		private readonly Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> _eventAppeared;
		private readonly Func<StreamSubscription, Position, CancellationToken, Task> _checkpointReached;
		private readonly Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? _subscriptionDropped;
		private readonly ILogger _log;
		private readonly CancellationTokenSource _disposed;
		private int _subscriptionDroppedInvoked;

		/// <summary>
		/// The id of the <see cref="StreamSubscription"/> set by the server.
		/// </summary>
		public string SubscriptionId { get; }

		internal static async Task<StreamSubscription> Confirm(
			IAsyncEnumerable<(SubscriptionConfirmation confirmation, Position?, ResolvedEvent)> read,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped,
			ILogger log,
			Func<StreamSubscription, Position, CancellationToken, Task>? checkpointReached = null,
			CancellationToken cancellationToken = default) {

			var enumerator = read.GetAsyncEnumerator(cancellationToken);
			if (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false) &&
			    enumerator.Current.confirmation != SubscriptionConfirmation.None)
				return new StreamSubscription(enumerator, eventAppeared, subscriptionDropped, log,
					checkpointReached, cancellationToken);
			throw new InvalidOperationException($"Subscription to {enumerator} could not be confirmed.");
		}

		private StreamSubscription(
			IAsyncEnumerator<(SubscriptionConfirmation confirmation, Position?, ResolvedEvent)> events,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped,
			ILogger log,
			Func<StreamSubscription, Position, CancellationToken, Task>? checkpointReached,
			CancellationToken cancellationToken = default) {
			_disposed = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_events = new Enumerable(events, CheckpointReached, _disposed.Token);
			_eventAppeared = eventAppeared;
			_checkpointReached = checkpointReached ?? ((_, __, ct) => Task.CompletedTask);
			_subscriptionDropped = subscriptionDropped;
			_log = log;
			_subscriptionDroppedInvoked = 0;
			SubscriptionId = events.Current.confirmation.SubscriptionId;

			Task.Run(Subscribe);
		}

		private Task CheckpointReached(Position position) => _checkpointReached(this, position, _disposed.Token);

		private async Task Subscribe() {
			_log.LogDebug("Subscription {subscriptionId} confirmed.", SubscriptionId);
			using var _ = _disposed;
			try {
				await foreach (var resolvedEvent in _events.ConfigureAwait(false)) {
					try {
						_log.LogTrace("Subscription {subscriptionId} received event {streamName}@{streamRevision} {position}",
							SubscriptionId, resolvedEvent.OriginalEvent.EventStreamId,
							resolvedEvent.OriginalEvent.EventNumber, resolvedEvent.OriginalEvent.Position);
						await _eventAppeared(this, resolvedEvent, _disposed.Token).ConfigureAwait(false);
					} catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException) {
						if (_subscriptionDroppedInvoked != 0) {
							return;
						}
						_log.LogWarning(ex,
							"Subscription {subscriptionId} was dropped because cancellation was requested by another caller.",
							SubscriptionId);

						SubscriptionDropped(SubscriptionDroppedReason.Disposed);

						return;
					} catch (Exception ex) {
						_log.LogError(ex,
							"Subscription {subscriptionId} was dropped because the subscriber made an error.",
							SubscriptionId);
						SubscriptionDropped(SubscriptionDroppedReason.SubscriberError, ex);

						return;
					}
				}
			} catch (Exception ex) {
				if (_subscriptionDroppedInvoked == 0) {
					_log.LogError(ex,
						"Subscription {subscriptionId} was dropped because an error occurred on the server.",
						SubscriptionId);
					SubscriptionDropped(SubscriptionDroppedReason.ServerError, ex);
				}
			}
		}

		/// <inheritdoc />
		public void Dispose() => SubscriptionDropped(SubscriptionDroppedReason.Disposed);

		private void SubscriptionDropped(SubscriptionDroppedReason reason, Exception? ex = null) {
			if (Interlocked.CompareExchange(ref _subscriptionDroppedInvoked, 1, 0) == 1) {
				return;
			}

			try {
				_subscriptionDropped?.Invoke(this, reason, ex);
			} finally {
				_disposed.Dispose();
			}
		}

		private class Enumerable : IAsyncEnumerable<ResolvedEvent> {
			private readonly IAsyncEnumerator<(SubscriptionConfirmation, Position?, ResolvedEvent)> _inner;
			private readonly Func<Position, Task> _checkpointReached;
			private readonly CancellationToken _cancellationToken;

			public Enumerable(IAsyncEnumerator<(SubscriptionConfirmation, Position?, ResolvedEvent)> inner,
				Func<Position, Task> checkpointReached, CancellationToken cancellationToken) {
				if (inner == null) {
					throw new ArgumentNullException(nameof(inner));
				}

				if (checkpointReached == null) {
					throw new ArgumentNullException(nameof(checkpointReached));
				}

				_inner = inner;
				_checkpointReached = checkpointReached;
				_cancellationToken = cancellationToken;
			}

			public IAsyncEnumerator<ResolvedEvent> GetAsyncEnumerator(CancellationToken cancellationToken = default)
				=> new Enumerator(_inner, _checkpointReached, _cancellationToken);

			private class Enumerator : IAsyncEnumerator<ResolvedEvent> {
				private readonly IAsyncEnumerator<(SubscriptionConfirmation, Position? position, ResolvedEvent
					resolvedEvent)> _inner;

				private readonly Func<Position, Task> _checkpointReached;
				private readonly CancellationToken _cancellationToken;

				public Enumerator(IAsyncEnumerator<(SubscriptionConfirmation, Position?, ResolvedEvent)> inner,
					Func<Position, Task> checkpointReached, CancellationToken cancellationToken) {
					if (inner == null) {
						throw new ArgumentNullException(nameof(inner));
					}

					if (checkpointReached == null) {
						throw new ArgumentNullException(nameof(checkpointReached));
					}

					_inner = inner;
					_checkpointReached = checkpointReached;
					_cancellationToken = cancellationToken;
				}

				public ValueTask DisposeAsync() => _inner.DisposeAsync();

				public async ValueTask<bool> MoveNextAsync() {
					ReadLoop:
					if (_cancellationToken.IsCancellationRequested) {
						return false;
					}

					if (!await _inner.MoveNextAsync().ConfigureAwait(false)) {
						return false;
					}

					if (_cancellationToken.IsCancellationRequested) {
						return false;
					}

					if (!_inner.Current.position.HasValue) {
						return true;
					}

					await _checkpointReached(_inner.Current.position.Value).ConfigureAwait(false);
					goto ReadLoop;
				}

				public ResolvedEvent Current => _inner.Current.resolvedEvent;
			}
		}
	}
}
