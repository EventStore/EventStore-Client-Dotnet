namespace EventStore.Client {
	public interface IWriteResult {
		long NextExpectedVersion { get; }
		Position LogPosition { get; }
	}
}
