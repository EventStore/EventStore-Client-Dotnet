namespace EventStore.Client {
	/// <summary>
	/// A collection of constants and methods to identify streams.
	/// </summary>
	public static class SystemStreams {
		/// <summary>
		/// A stream containing all events in the KurrentDB transaction file.
		/// </summary>
		public const string AllStream = "$all";

		/// <summary>
		/// A stream containing links pointing to each stream in the KurrentDB.
		/// </summary>
		public const string StreamsStream = "$streams";

		/// <summary>
		/// A stream containing system settings.
		/// </summary>
		public const string SettingsStream = "$settings";

		/// <summary>
		/// A stream containing statistics.
		/// </summary>
		public const string StatsStreamPrefix = "$stats";

		/// <summary>
		/// Returns True if the stream is a system stream.
		/// </summary>
		/// <param name="streamId"></param>
		/// <returns></returns>
		public static bool IsSystemStream(string streamId) => streamId.Length != 0 && streamId[0] == '$';

		/// <summary>
		/// Returns the metadata stream of the stream.
		/// </summary>
		/// <param name="streamId"></param>
		/// <returns></returns>
		public static string MetastreamOf(string streamId) => "$$" + streamId;

		/// <summary>
		/// Returns true if the stream is a metadata stream.
		/// </summary>
		/// <param name="streamId"></param>
		/// <returns></returns>
		public static bool IsMetastream(string streamId) => streamId[..2] == "$$";

		/// <summary>
		/// Returns the original stream of the metadata stream.
		/// </summary>
		/// <param name="metastreamId"></param>
		/// <returns></returns>
		public static string OriginalStreamOf(string metastreamId) => metastreamId[2..];
	}
}
