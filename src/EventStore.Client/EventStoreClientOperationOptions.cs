using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public class EventStoreClientOperationOptions {
		public TimeSpan? TimeoutAfter { get; set; }
		public bool ThrowOnAppendFailure { get; set; }

		public Func<UserCredentials, CancellationToken, ValueTask<string>> GetAuthenticationHeaderValue { get; set; } =
			null!;

		public static EventStoreClientOperationOptions Default => new EventStoreClientOperationOptions {
			TimeoutAfter = TimeSpan.FromSeconds(5),
			ThrowOnAppendFailure = true,
			GetAuthenticationHeaderValue = (userCredentials, ct) => new ValueTask<string>(userCredentials.ToString())
		};

		public EventStoreClientOperationOptions Clone() =>
			new EventStoreClientOperationOptions {
				TimeoutAfter = TimeoutAfter,
				ThrowOnAppendFailure = ThrowOnAppendFailure,
				GetAuthenticationHeaderValue = GetAuthenticationHeaderValue
			};
	}
}
