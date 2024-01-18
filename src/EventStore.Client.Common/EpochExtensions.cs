namespace EventStore.Client;

static class EpochExtensions {
	private const long TicksPerMillisecond = 10000;
	private const long TicksPerSecond = TicksPerMillisecond * 1000;
	private const long TicksPerMinute = TicksPerSecond * 60;
	private const long TicksPerHour = TicksPerMinute * 60;
	private const long TicksPerDay = TicksPerHour * 24;
	private const int DaysPerYear = 365;
	private const int DaysPer4Years = DaysPerYear * 4 + 1;
	private const int DaysPer100Years = DaysPer4Years * 25 - 1;
	private const int DaysPer400Years = DaysPer100Years * 4 + 1;
	private const int DaysTo1970 = DaysPer400Years * 4 + DaysPer100Years * 3 + DaysPer4Years * 17 + DaysPerYear;
	private const long UnixEpochTicks = DaysTo1970 * TicksPerDay;

	private static readonly DateTime UnixEpoch = new(UnixEpochTicks, DateTimeKind.Utc);

	public static DateTime FromTicksSinceEpoch(this long value) => new(UnixEpoch.Ticks + value, DateTimeKind.Utc);

	public static long ToTicksSinceEpoch(this DateTime value) => (value - UnixEpoch).Ticks;
}
