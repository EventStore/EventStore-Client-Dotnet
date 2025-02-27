using System;

namespace EventStore.Client {
	/// <summary>
	/// Exception thrown when an append exceeds the maximum size set by the server.
	/// </summary>
	public class MaximumAppendSizeExceededException : Exception {
		/// <summary>
		/// The configured maximum append size.
		/// </summary>
		public uint MaxAppendSize { get; }

		/// <summary>
		/// Constructs a new <see cref="MaximumAppendSizeExceededException"/>.
		/// </summary>
		/// <param name="maxAppendSize"></param>
		/// <param name="innerException"></param>
		public MaximumAppendSizeExceededException(uint maxAppendSize, Exception? innerException = null) :
			base($"Maximum Append Size of {maxAppendSize} Exceeded.", innerException) {
			MaxAppendSize = maxAppendSize;
		}

		/// <summary>
		/// Constructs a new <see cref="MaximumAppendSizeExceededException"/>.
		/// </summary>
		/// <param name="maxAppendSize"></param>
		/// <param name="innerException"></param>
		public MaximumAppendSizeExceededException(int maxAppendSize, Exception? innerException = null) : this(
			(uint)maxAppendSize, innerException) {

		}
	}
}
