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
			if (!await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false) ||
			    enumerator.Current.confirmation == SubscriptionConfirmation.None) {
				throw new InvalidOperationException();
			}

			return new StreamSubscription(enumerator, eventAppeared, subscriptionDropped, log,
				checkpointReached, cancellationToken);
		}

		private StreamSubscription(IAsyncEnumerator<(SubscriptionConfirmation confirmation, Position?, ResolvedEvent)> events,
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
			try {
				await foreach (var resolvedEvent in _events.ConfigureAwait(false)) {
					try {
						if (_disposed.IsCancellationRequested) {
							_log.LogDebug("Subscription was dropped because cancellation was requested.");
							SubscriptionDropped(SubscriptionDroppedReason.Disposed);
							return;
						}

						_log.LogTrace("Subscription received event {streamName}@{streamRevision} {position}",
							resolvedEvent.OriginalEvent.EventStreamId, resolvedEvent.OriginalEvent.EventNumber,
							resolvedEvent.OriginalEvent.Position);
						await _eventAppeared(this, resolvedEvent, _disposed.Token).ConfigureAwait(false);
					} catch (Exception ex) when (ex is ObjectDisposedException || ex is OperationCanceledException) {
						_log.LogWarning(ex,
							"Subscription was dropped because cancellation was requested by another caller.");
						SubscriptionDropped(SubscriptionDroppedReason.Disposed);
						return;
					} catch (Exception ex) {
						try {
							_log.LogError(ex, "Subscription was dropped because the subscriber made an error.");
							SubscriptionDropped(SubscriptionDroppedReason.SubscriberError, ex);
						} finally {
							_disposed.Cancel();
						}

						return;
					}
				}
			} catch (Exception ex) {
				try {
					_log.LogError(ex, "Subscription was dropped because an error occurred on the server.");
					SubscriptionDropped(SubscriptionDroppedReason.ServerError, ex);
				} finally {
					_disposed.Cancel();
				}
			}
		}

		/// <inheritdoc />
		public void Dispose() {
			if (_disposed.IsCancellationRequested) {
				return;
			}

			SubscriptionDropped(SubscriptionDroppedReason.Disposed);

			_disposed.Cancel();
			_disposed.Dispose();
		}

		private void SubscriptionDropped(SubscriptionDroppedReason reason, Exception? ex = null) {
			if (Interlocked.CompareExchange(ref _subscriptionDroppedInvoked, 1, 0) == 1) {
				return;
			}

			_subscriptionDropped?.Invoke(this, reason, ex);
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
