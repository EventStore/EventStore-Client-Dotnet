using System;

namespace EventStore.Client {
	/// <summary>
	/// A class representing the settings of a persistent subscription.
	/// </summary>
	public sealed class PersistentSubscriptionSettings {
		/// <summary>
		/// Whether the <see cref="PersistentSubscription"></see> should resolve linkTo events to their linked events.
		/// </summary>
		public readonly bool ResolveLinkTos;

		/// <summary>
		/// Which event position in the stream or transaction file the subscription should start from.
		/// </summary>
		public readonly IPosition? StartFrom;

		/// <summary>
		/// Whether to track latency statistics on this subscription.
		/// </summary>
		public readonly bool ExtraStatistics;

		/// <summary>
		/// The amount of time after which to consider a message as timed out and retried.
		/// </summary>
		public readonly TimeSpan MessageTimeout;

		/// <summary>
		/// The maximum number of retries (due to timeout) before a message is considered to be parked.
		/// </summary>
		public readonly int MaxRetryCount;

		/// <summary>
		/// The size of the buffer (in-memory) listening to live messages as they happen before paging occurs.
		/// </summary>
		public readonly int LiveBufferSize;

		/// <summary>
		/// The number of events read at a time when paging through history.
		/// </summary>
		public readonly int ReadBatchSize;

		/// <summary>
		/// The number of events to cache when paging through history.
		/// </summary>
		public readonly int HistoryBufferSize;

		/// <summary>
		/// The amount of time to try to checkpoint after.
		/// </summary>
		public readonly TimeSpan CheckPointAfter;

		/// <summary>
		/// The minimum number of messages to process before a checkpoint may be written.
		/// </summary>
		public readonly int CheckPointLowerBound;

		/// <summary>
		/// The maximum number of messages not checkpointed before forcing a checkpoint.
		/// </summary>
		public readonly int CheckPointUpperBound;

		/// <summary>
		/// The maximum number of subscribers allowed.
		/// </summary>
		public readonly int MaxSubscriberCount;

		/// <summary>
		/// The strategy to use for distributing events to client consumers. See <see cref="SystemConsumerStrategies"/> for system supported strategies.
		/// </summary>
		public readonly string ConsumerStrategyName;

		/// <summary>
		/// Constructs a new <see cref="PersistentSubscriptionSettings"/>.
		/// </summary>
		/// <param name="resolveLinkTos"></param>
		/// <param name="startFrom"></param>
		/// <param name="extraStatistics"></param>
		/// <param name="messageTimeout"></param>
		/// <param name="maxRetryCount"></param>
		/// <param name="liveBufferSize"></param>
		/// <param name="readBatchSize"></param>
		/// <param name="historyBufferSize"></param>
		/// <param name="checkPointAfter"></param>
		/// <param name="checkPointLowerBound"></param>
		/// <param name="checkPointUpperBound"></param>
		/// <param name="maxSubscriberCount"></param>
		/// <param name="consumerStrategyName"></param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public PersistentSubscriptionSettings(bool resolveLinkTos = false, IPosition? startFrom = null,
			bool extraStatistics = false, TimeSpan? messageTimeout = null, int maxRetryCount = 10,
			int liveBufferSize = 500, int readBatchSize = 20, int historyBufferSize = 500,
			TimeSpan? checkPointAfter = null, int checkPointLowerBound = 10, int checkPointUpperBound = 1000,
			int maxSubscriberCount = 0, string consumerStrategyName = SystemConsumerStrategies.RoundRobin) {
			messageTimeout ??= TimeSpan.FromSeconds(30);
			checkPointAfter ??= TimeSpan.FromSeconds(2);

			if (messageTimeout.Value < TimeSpan.Zero || messageTimeout.Value.TotalMilliseconds > int.MaxValue) {
				throw new ArgumentOutOfRangeException(
					nameof(messageTimeout),
					$"{nameof(messageTimeout)} must be greater than {TimeSpan.Zero} and less than or equal to {TimeSpan.FromMilliseconds(int.MaxValue)}");
			}

			if (checkPointAfter.Value < TimeSpan.Zero || checkPointAfter.Value.TotalMilliseconds > int.MaxValue) {
				throw new ArgumentOutOfRangeException(
					nameof(checkPointAfter),
					$"{nameof(checkPointAfter)} must be greater than {TimeSpan.Zero} and less than or equal to {TimeSpan.FromMilliseconds(int.MaxValue)}");
			}

			ResolveLinkTos = resolveLinkTos;
			StartFrom = startFrom;
			ExtraStatistics = extraStatistics;
			MessageTimeout = messageTimeout.Value;
			MaxRetryCount = maxRetryCount;
			LiveBufferSize = liveBufferSize;
			ReadBatchSize = readBatchSize;
			HistoryBufferSize = historyBufferSize;
			CheckPointAfter = checkPointAfter.Value;
			CheckPointLowerBound = checkPointLowerBound;
			CheckPointUpperBound = checkPointUpperBound;
			MaxSubscriberCount = maxSubscriberCount;
			ConsumerStrategyName = consumerStrategyName;
		}
	}
}
