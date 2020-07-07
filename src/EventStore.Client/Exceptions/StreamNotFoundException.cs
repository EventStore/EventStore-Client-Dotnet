using System;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when an attempt is made to read or write to a stream that does not exist.
	/// </summary>
	public class StreamNotFoundException : Exception {
		/// <summary>
		/// The name of the stream.
		/// </summary>
		public readonly string Stream;

		/// <summary>
		/// Constructs a new instance of <see cref="StreamNotFoundException"/>.
		/// </summary>
		/// <param name="stream">The name of the stream.</param>
		/// <param name="exception"></param>
		public StreamNotFoundException(string stream, Exception? exception = null)
			: base($"Event stream '{stream}' was not found.", exception) {
			Stream = stream;
		}
	}
}
