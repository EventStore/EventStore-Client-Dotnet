using System.IO;

#nullable enable
namespace EventStore.Client {
	public readonly struct WrongExpectedVersionResult : IWriteResult {
		public string StreamName { get; }
		public long NextExpectedVersion { get; }
		public long ActualVersion { get; }
		public Position LogPosition { get; }

		public WrongExpectedVersionResult(string streamName, long nextExpectedVersion, long actualVersion) {
			StreamName = streamName;
			ActualVersion = actualVersion;
			NextExpectedVersion = nextExpectedVersion;
			LogPosition = default;
		}
	}
}
