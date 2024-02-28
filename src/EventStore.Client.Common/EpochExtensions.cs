namespace EventStore.Client;

static class EpochExtensions {
#if NET
	static readonly DateTime UnixEpoch = DateTime.UnixEpoch;
#else
	const long TicksPerMillisecond = 10000;
	const long TicksPerSecond      = TicksPerMillisecond * 1000;
	const long TicksPerMinute      = TicksPerSecond * 60;
	const long TicksPerHour        = TicksPerMinute * 60;
	const long TicksPerDay         = TicksPerHour * 24;
	const int  DaysPerYear         = 365;
	const int  DaysPer4Years       = DaysPerYear * 4 + 1;
	const int  DaysPer100Years     = DaysPer4Years * 25 - 1;
	const int  DaysPer400Years     = DaysPer100Years * 4 + 1;
	const int  DaysTo1970          = DaysPer400Years * 4 + DaysPer100Years * 3 + DaysPer4Years * 17 + DaysPerYear;
	const long UnixEpochTicks      = DaysTo1970 * TicksPerDay;

	static readonly DateTime UnixEpoch = new(UnixEpochTicks, DateTimeKind.Utc);
#endif

	public static DateTime FromTicksSinceEpoch(this long value) => new(UnixEpoch.Ticks + value, DateTimeKind.Utc);

	public static long ToTicksSinceEpoch(this DateTime value) => (value - UnixEpoch).Ticks;
}