using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.PersistentSubscriptions;

#nullable enable
namespace EventStore.Client {
	partial class EventStorePersistentSubscriptionsClient {
		/// <summary>
		/// Subscribes to a persistent subscription.
		/// </summary>
		/// <param name="streamName"></param>
		/// <param name="groupName"></param>
		/// <param name="eventAppeared"></param>
		/// <param name="subscriptionDropped"></param>
		/// <param name="userCredentials"></param>
		/// <param name="bufferSize"></param>
		/// <param name="autoAck"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task<PersistentSubscription> SubscribeAsync(string streamName, string groupName,
			Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
			Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
			UserCredentials? userCredentials = null, int bufferSize = 10, bool autoAck = true,
			CancellationToken cancellationToken = default) {
			if (streamName == null) {
				throw new ArgumentNullException(nameof(streamName));
			}

			if (groupName == null) {
				throw new ArgumentNullException(nameof(groupName));
			}

			if (eventAppeared == null) {
				throw new ArgumentNullException(nameof(eventAppeared));
			}

			if (streamName == string.Empty) {
				throw new ArgumentException($"{nameof(streamName)} may not be empty.", nameof(streamName));
			}

			if (groupName == string.Empty) {
				throw new ArgumentException($"{nameof(groupName)} may not be empty.", nameof(groupName));
			}

			if (bufferSize <= 0) {
				throw new ArgumentOutOfRangeException(nameof(bufferSize));
			}

			var operationOptions = Settings.OperationOptions.Clone();
			operationOptions.TimeoutAfter = new TimeSpan?();

			var call = new PersistentSubscriptions.PersistentSubscriptions.PersistentSubscriptionsClient(
				await SelectCallInvoker(cancellationToken).ConfigureAwait(false)).Read(EventStoreCallOptions.Create(
				Settings, operationOptions, userCredentials, cancellationToken));

			return await PersistentSubscription.Confirm(call, new ReadReq.Types.Options {
				BufferSize = bufferSize,
				GroupName = groupName,
				StreamIdentifier = streamName,
				UuidOption = new ReadReq.Types.Options.Types.UUIDOption {Structured = new Empty()}
			}, autoAck, eventAppeared, subscriptionDropped ?? delegate { }, cancellationToken).ConfigureAwait(false);
		}
	}
}
