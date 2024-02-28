namespace EventStore.Client {
	/// <summary>
	/// A class representing the options to use when filtering read operations.
	/// </summary>
	public class SubscriptionFilterOptions {
		/// <summary>
		/// The <see cref="IEventFilter"/> to apply.
		/// </summary>
		public IEventFilter Filter { get; }

		/// <summary>
		/// Sets how often the checkpointReached callback is called.
		/// </summary>
		public uint CheckpointInterval { get; }

		/// <summary>
		/// A Task invoked and await when a checkpoint is reached.
		/// Set the checkpointInterval to define how often this method is called.
		/// </summary>
		[Obsolete]
		public Func<StreamSubscription, Position, CancellationToken, Task> CheckpointReached { get; } = null!;

		/// <summary>
		///
		/// </summary>
		/// <param name="filter">The <see cref="IEventFilter"/> to apply.</param>
		/// <param name="checkpointInterval">Sets how often the checkpointReached callback is called.</param>
		/// <param name="checkpointReached">
		/// A Task invoked and await when a checkpoint is reached.
		/// Set the checkpointInterval to define how often this method is called.
		/// </param>
		/// <exception cref="ArgumentNullException"></exception>
		[Obsolete]
		public SubscriptionFilterOptions(IEventFilter filter, uint checkpointInterval,
			Func<StreamSubscription, Position, CancellationToken, Task>? checkpointReached) 
			: this(filter, checkpointInterval) {
			CheckpointReached = checkpointReached ?? ((_, __, ct) => Task.CompletedTask);
		}
		
		/// <summary>
		///
		/// </summary>
		/// <param name="filter">The <see cref="IEventFilter"/> to apply.</param>
		/// <param name="checkpointInterval">Sets how often the checkpointReached callback is called.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public SubscriptionFilterOptions(IEventFilter filter, uint checkpointInterval = 1) {
			if (checkpointInterval == 0) {
				throw new ArgumentOutOfRangeException(nameof(checkpointInterval),
					checkpointInterval, $"{nameof(checkpointInterval)} must be greater than 0.");
			}

			Filter = filter;
			CheckpointInterval = checkpointInterval;
		}
	}
}
