using System;

namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when the maximum number of subscribers on a persistent subscription is exceeded.
	/// </summary>
	public class MaximumSubscribersReachedException : Exception {
		/// <summary>
		/// The stream name.
		/// </summary>
		public readonly string StreamName;
		/// <summary>
		/// The group name.
		/// </summary>
		public readonly string GroupName;

		/// <summary>
		/// Constructs a new <see cref="MaximumSubscribersReachedException"/>.
		/// </summary>
		/// <param name="streamName"></param>
		/// <param name="groupName"></param>
		/// <param name="exception"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public MaximumSubscribersReachedException(string streamName, string groupName, Exception? exception = null)
			: base($"Maximum subscriptions reached for subscription group '{groupName}' on stream '{streamName}.'",
				exception) {
			if (streamName == null) throw new ArgumentNullException(nameof(streamName));
			if (groupName == null) throw new ArgumentNullException(nameof(groupName));
			StreamName = streamName;
			GroupName = groupName;
		}
	}
}
