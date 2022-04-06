using System;

namespace EventStore.Client {
	internal static class EpochExtensions {
		private static readonly DateTime UnixEpoch = DateTime.UnixEpoch;

		public static DateTime FromTicksSinceEpoch(this long value) =>
			new DateTime(UnixEpoch.Ticks + value, DateTimeKind.Utc);

		public static long ToTicksSinceEpoch(this DateTime value) =>
			(value - UnixEpoch).Ticks;
	}
}
