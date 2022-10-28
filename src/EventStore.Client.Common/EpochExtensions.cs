using System;

namespace EventStore.Client {
	internal static class EpochExtensions {
#if !GRPC_NETSTANDARD
		private static readonly DateTime UnixEpoch = DateTime.UnixEpoch;
#else
		private static readonly DateTime UnixEpoch = DateTimeOffset.FromUnixTimeSeconds(0).DateTime;
#endif

		public static DateTime FromTicksSinceEpoch(this long value) =>
			new DateTime(UnixEpoch.Ticks + value, DateTimeKind.Utc);

		public static long ToTicksSinceEpoch(this DateTime value) =>
			(value - UnixEpoch).Ticks;
	}
}
