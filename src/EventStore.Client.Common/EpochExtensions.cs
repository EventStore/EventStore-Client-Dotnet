namespace EventStore.Client;

static class EpochExtensions {
	static readonly DateTime UnixEpoch = DateTime.UnixEpoch;

	public static DateTime FromTicksSinceEpoch(this long value) => new(UnixEpoch.Ticks + value, DateTimeKind.Utc);

	public static long ToTicksSinceEpoch(this DateTime value) => (value - UnixEpoch).Ticks;
}