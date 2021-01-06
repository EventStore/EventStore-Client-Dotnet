using System;

#nullable enable
namespace EventStore.Client {
	internal static class EpochExtensions {
		private static readonly DateTime UnixEpoch =
#if NETFRAMEWORK
				new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
#else
				DateTime.UnixEpoch
#endif
			;

		public static DateTime FromTicksSinceEpoch(this long value) =>
			new DateTime(UnixEpoch.Ticks + value, DateTimeKind.Utc);

		public static long ToTicksSinceEpoch(this DateTime value) =>
			(value - UnixEpoch).Ticks;
	}
}
