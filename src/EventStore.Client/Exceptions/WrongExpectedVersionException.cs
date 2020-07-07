using System;
using System.Runtime.Serialization;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// Exception thrown if the expected version specified on an operation
	/// does not match the version of the stream when the operation was attempted.
	/// </summary>
	public class WrongExpectedVersionException : Exception {
		/// <summary>
		/// The stream identifier.
		/// </summary>
		public string StreamName { get; }

		/// <summary>
		/// If available, the expected version specified for the operation that failed.
		/// </summary>
		public long? ExpectedVersion { get; }

		/// <summary>
		/// If available, the current version of the stream that the operation was attempted on.
		/// </summary>
		public long? ActualVersion { get; }


		/// <summary>
		/// Constructs a new instance of <see cref="WrongExpectedVersionException" /> with the expected and actual versions if available.
		/// </summary>
		public WrongExpectedVersionException(string streamName, long? expectedVersion, long? actualVersion,
			Exception? exception = null) :
			base(
				$"Append failed due to WrongExpectedVersion. Stream: {streamName}, Expected version: {expectedVersion}, Actual version: {actualVersion}",
				exception) {
			StreamName = streamName;
			ExpectedVersion = expectedVersion;
			ActualVersion = actualVersion;
		}
	}
}
