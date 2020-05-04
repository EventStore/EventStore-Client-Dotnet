using System;

#nullable enable
namespace EventStore.Client {
	public class EventStoreClientOperationOptions {
		public TimeSpan? TimeoutAfter { get; set; }
		public bool ThrowOnAppendFailure { get; set; }

		public static EventStoreClientOperationOptions Default => new EventStoreClientOperationOptions {
			TimeoutAfter = TimeSpan.FromSeconds(5),
			ThrowOnAppendFailure = true
		};

		public EventStoreClientOperationOptions Clone() =>
			new EventStoreClientOperationOptions {
				TimeoutAfter = TimeoutAfter,
				ThrowOnAppendFailure = true
			};
	}
}
