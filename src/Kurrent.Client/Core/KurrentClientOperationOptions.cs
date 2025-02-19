using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	/// <summary>
	/// A class representing the options to apply to an individual operation.
	/// </summary>
	public class KurrentClientOperationOptions {
		/// <summary>
		/// Whether or not to immediately throw a <see cref="WrongExpectedVersionException"/> when an append fails.
		/// </summary>
		public bool ThrowOnAppendFailure { get; set; }

		/// <summary>
		/// The batch size, in bytes.
		/// </summary>
		public int BatchAppendSize { get; set; }

		/// <summary>
		/// A callback function to extract the authorize header value from the <see cref="UserCredentials"/> used in the operation.
		/// </summary>
		public Func<UserCredentials, CancellationToken, ValueTask<string>> GetAuthenticationHeaderValue { get; set; } =
			null!;

		/// <summary>
		/// The default <see cref="KurrentClientOperationOptions"/>.
		/// </summary>
		public static KurrentClientOperationOptions Default => new() {
			ThrowOnAppendFailure = true,
			GetAuthenticationHeaderValue = (userCredentials, _) => new ValueTask<string>(userCredentials.ToString()),
			BatchAppendSize = 3 * 1024 * 1024
		};


		/// <summary>
		/// Clones a copy of the current <see cref="KurrentClientOperationOptions"/>.
		/// </summary>
		/// <returns></returns>
		public KurrentClientOperationOptions Clone() => new() {
			ThrowOnAppendFailure = ThrowOnAppendFailure,
			GetAuthenticationHeaderValue = GetAuthenticationHeaderValue,
			BatchAppendSize = BatchAppendSize
		};
	}
}
