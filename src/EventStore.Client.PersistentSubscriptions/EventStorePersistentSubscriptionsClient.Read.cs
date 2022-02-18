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
		[Obsolete("SubscribeAsync is no longer supported. Use SubscribeToStreamAsync with manual acks instead.", true)]
		public async Task<PersistentSubscription> SubscribeAsync(string streamName, string groupName,
			Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
			Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
			UserCredentials? userCredentials = null, int bufferSize = 10, bool autoAck = true,
			CancellationToken cancellationToken = default) {
			if (autoAck) {
				throw new InvalidOperationException(
					$"AutoAck is no longer supported. Please use {nameof(SubscribeToStreamAsync)} with manual acks instead.");
			}

			return await SubscribeToStreamAsync(streamName, groupName, eventAppeared, subscriptionDropped,
				userCredentials, bufferSize, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Subscribes to a persistent subscription. Messages must be manually acknowledged
		/// </summary>
		/// <param name="streamName"></param>
		/// <param name="groupName"></param>
		/// <param name="eventAppeared"></param>
		/// <param name="subscriptionDropped"></param>
		/// <param name="userCredentials"></param>
		/// <param name="bufferSize"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task<PersistentSubscription> SubscribeToStreamAsync(string streamName, string groupName,
			Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
			Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
			UserCredentials? userCredentials = null, int bufferSize = 10,
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

			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);

			if (streamName == SystemStreams.AllStream &&
			    !channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsToAll) {
				throw new NotSupportedException("The server does not support persistent subscriptions to $all.");
			}

			var readOptions = new ReadReq.Types.Options {
				BufferSize = bufferSize,
				GroupName = groupName,
				UuidOption = new ReadReq.Types.Options.Types.UUIDOption {Structured = new Empty()}
			};

			if (streamName == SystemStreams.AllStream) {
				readOptions.All = new Empty();
			} else {
				readOptions.StreamIdentifier = streamName;
			}

			return await PersistentSubscription.Confirm(channelInfo.Channel, channelInfo.CallInvoker, Settings, userCredentials, readOptions, _log, eventAppeared,
				subscriptionDropped ?? delegate { }, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Subscribes to a persistent subscription to $all. Messages must be manually acknowledged
		/// </summary>
		/// <param name="groupName"></param>
		/// <param name="eventAppeared"></param>
		/// <param name="subscriptionDropped"></param>
		/// <param name="userCredentials"></param>
		/// <param name="bufferSize"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<PersistentSubscription> SubscribeToAllAsync(string groupName,
			Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
			Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
			UserCredentials? userCredentials = null, int bufferSize = 10,
			CancellationToken cancellationToken = default) =>
			await SubscribeToStreamAsync(SystemStreams.AllStream, groupName, eventAppeared, subscriptionDropped,
					userCredentials, bufferSize, cancellationToken)
				.ConfigureAwait(false);
	}
}
