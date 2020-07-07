using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// A class representing the options to apply to an individual operation.
	/// </summary>
	public class EventStoreClientOperationOptions {
		/// <summary>
		/// An optional <see cref="TimeSpan"/> to use for gRPC deadlines.
		/// </summary>
		public TimeSpan? TimeoutAfter { get; set; }

		/// <summary>
		/// Whether or not to immediately throw a <see cref="WrongExpectedVersionException"/> when an append fails.
		/// </summary>
		public bool ThrowOnAppendFailure { get; set; }

		/// <summary>
		/// A callback function to extract the authorize header value from the <see cref="UserCredentials"/> used in the operation.
		/// </summary>
		public Func<UserCredentials, CancellationToken, ValueTask<string>> GetAuthenticationHeaderValue { get; set; } =
			null!;

		/// <summary>
		/// The default <see cref="EventStoreClientOperationOptions"/>.
		/// </summary>
		public static EventStoreClientOperationOptions Default => new EventStoreClientOperationOptions {
			TimeoutAfter = TimeSpan.FromSeconds(5),
			ThrowOnAppendFailure = true,
			GetAuthenticationHeaderValue = (userCredentials, ct) => new ValueTask<string>(userCredentials.ToString())
		};


		/// <summary>
		/// Clones a copy of the current <see cref="EventStoreClientOperationOptions"/>.
		/// </summary>
		/// <returns></returns>
		public EventStoreClientOperationOptions Clone() =>
			new EventStoreClientOperationOptions {
				TimeoutAfter = TimeoutAfter,
				ThrowOnAppendFailure = ThrowOnAppendFailure,
				GetAuthenticationHeaderValue = GetAuthenticationHeaderValue
			};
	}
}
