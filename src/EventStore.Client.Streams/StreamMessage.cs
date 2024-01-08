namespace EventStore.Client {
	/// <summary>
	/// The base record of all stream messages.
	/// </summary>
	public abstract record StreamMessage {
		/// <summary>
		/// A <see cref="EventStore.Client.StreamMessage"/> that represents a <see cref="EventStore.Client.ResolvedEvent"/>.
		/// </summary>
		/// <param name="ResolvedEvent">The <see cref="EventStore.Client.ResolvedEvent"/>.</param>
		public record Event(ResolvedEvent ResolvedEvent) : StreamMessage;

		/// <summary>
		/// A <see cref="StreamMessage"/> representing a stream that was not found.
		/// </summary>
		public record NotFound : StreamMessage {
			internal static readonly NotFound Instance = new();
		}

		/// <summary>
		/// A <see cref="StreamMessage"/> representing a successful read operation.
		/// </summary>
		public record Ok : StreamMessage {
			internal static readonly Ok Instance = new();
		};

		/// <summary>
		/// A <see cref="EventStore.Client.StreamMessage"/> indicating the first position of a stream.
		/// </summary>
		/// <param name="StreamPosition">The <see cref="EventStore.Client.StreamPosition"/>.</param>
		public record FirstStreamPosition(StreamPosition StreamPosition) : StreamMessage;

		/// <summary>
		/// A <see cref="EventStore.Client.StreamMessage"/> indicating the last position of a stream.
		/// </summary>
		/// <param name="StreamPosition">The <see cref="EventStore.Client.StreamPosition"/>.</param>
		public record LastStreamPosition(StreamPosition StreamPosition) : StreamMessage;

		/// <summary>
		/// A <see cref="EventStore.Client.StreamMessage"/> indicating the last position of the $all stream.
		/// </summary>
		/// <param name="Position">The <see cref="EventStore.Client.Position"/>.</param>
		public record LastAllStreamPosition(Position Position) : StreamMessage;

		/// <summary>
		/// The base record of all subscription specific messages.
		/// </summary>
		public abstract record SubscriptionMessage : StreamMessage {
			
			/// <summary>
			/// A <see cref="EventStore.Client.StreamMessage.SubscriptionMessage"/> that represents a subscription confirmation.
			/// </summary>
			public record SubscriptionConfirmation(string SubscriptionId) : SubscriptionMessage;

			/// <summary>
			/// A <see cref="EventStore.Client.StreamMessage.SubscriptionMessage"/> representing position reached in subscribed stream. This message will only be received when subscribing to $all stream
			/// </summary>
			public record Checkpoint(Position Position) : SubscriptionMessage;

			/// <summary>
			/// A <see cref="EventStore.Client.StreamMessage.SubscriptionMessage"/> representing client has reached live stream position
			/// </summary>
			public record CaughtUp : SubscriptionMessage {
				internal static readonly CaughtUp Instance = new();
			}

			/// <summary>
			/// A <see cref="EventStore.Client.StreamMessage.SubscriptionMessage"/> representing client has fallen behind, meaning it's no longer keeping up with the stream's space
			/// </summary>
			public record FellBehind : SubscriptionMessage {
				internal static readonly FellBehind Instance = new();
			}
		}

		/// <summary>
		/// A <see cref="EventStore.Client.StreamMessage"/> that could not be identified, usually indicating a lower client compatibility level than the server supports.
		/// </summary>
		public record Unknown : StreamMessage {
			internal static readonly Unknown Instance = new();
		}

		
		/// <summary>
		/// A test method that returns true if this message can be expected to be received when reading from stream; otherwise, this method returns false
		/// </summary>
		/// <returns></returns>
		public bool IsStreamReadMessage() {
			return this is not SubscriptionMessage && this is not Ok && this is not Unknown;
		}
		
		/// <summary>
		/// A test method that returns true if this message can be expected to be received when subscribing to a stream; otherwise, this method returns false
		/// </summary>
		/// <returns></returns>
		public bool IsSubscriptionMessage() {
			return this is SubscriptionMessage || this is NotFound || this is Event;
		}
	}
}
