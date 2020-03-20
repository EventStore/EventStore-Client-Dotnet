using System;

#nullable enable
namespace EventStore.Client {
	public class StreamNotFoundException : Exception {
		/// <summary>
		/// The name of the deleted stream.
		/// </summary>
		public readonly string Stream;

		/// <summary>
		/// Constructs a new instance of <see cref="StreamNotFoundException"/>.
		/// </summary>
		/// <param name="stream">The name of the deleted stream.</param>
		/// <param name="exception"></param>
		public StreamNotFoundException(string stream, Exception? exception = null)
			: base($"Event stream '{stream}' was not found.", exception) {
			Stream = stream;
		}
	}
}
