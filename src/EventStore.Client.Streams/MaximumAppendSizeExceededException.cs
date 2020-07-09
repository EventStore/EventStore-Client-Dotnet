using System;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// Exception thrown when an append exceeds the maximum size set by the server.
	/// </summary>
	public class MaximumAppendSizeExceededException : Exception {
		/// <summary>
		/// Constructs a new <see cref="MaximumAppendSizeExceededException"/>.
		/// </summary>
		/// <param name="maxAppendSize"></param>
		/// <param name="innerException"></param>
		public MaximumAppendSizeExceededException(int maxAppendSize, Exception? innerException) :
			base($"Maximum Append Size of {maxAppendSize} Exceeded.", innerException) {
		}
	}
}
