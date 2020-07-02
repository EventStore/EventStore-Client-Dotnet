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
		/// The current <see cref="StreamRevision" /> of the stream that the operation was attempted on.
		/// </summary>
		public StreamRevision ActualStreamRevision { get; }

		/// <summary>
		/// Constructs a new instance of <see cref="WrongExpectedVersionException" /> with the expected and actual versions if available.
		/// </summary>
		public WrongExpectedVersionException(string streamName, StreamRevision expectedStreamRevision,
			StreamRevision actualStreamRevision, Exception? exception = null) :
			base(
				$"Append failed due to WrongExpectedVersion. Stream: {streamName}, Expected version: {expectedStreamRevision}, Actual version: {actualStreamRevision}",
				exception) {
			StreamName = streamName;
			ActualStreamRevision = actualStreamRevision;
			ExpectedVersion = expectedStreamRevision == StreamRevision.None ? new long?() : expectedStreamRevision.ToInt64();
			ActualVersion = actualStreamRevision == StreamRevision.None ? new long?() : actualStreamRevision.ToInt64();
		}

		/// <summary>
		/// Constructs a new instance of <see cref="WrongExpectedVersionException" /> with the expected and actual versions if available.
		/// </summary>
		/// <param name="streamName"></param>
		/// <param name="expectedStreamState"></param>
		/// <param name="actualStreamRevision"></param>
		/// <param name="exception"></param>
		public WrongExpectedVersionException(string streamName, StreamState expectedStreamState,
			StreamRevision actualStreamRevision, Exception? exception = null) : base(
			$"Append failed due to WrongExpectedVersion. Stream: {streamName}, Expected state: {expectedStreamState}, Actual version: {actualStreamRevision}",
			exception) {
			StreamName = streamName;
			ActualStreamRevision = actualStreamRevision;
			ActualVersion = actualStreamRevision == StreamRevision.None ? new long?() : actualStreamRevision.ToInt64();
		}
	}
}
