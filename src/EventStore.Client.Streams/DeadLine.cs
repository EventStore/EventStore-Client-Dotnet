using System;
using System.Threading;

#nullable enable
namespace EventStore.Client {
	internal static class DeadLine {
		public static DateTime? After(TimeSpan? timeoutAfter){
			if(!timeoutAfter.HasValue) return null;
			if(timeoutAfter.Value == TimeSpan.MaxValue || timeoutAfter.Value == Timeout.InfiniteTimeSpan) return DateTime.MaxValue;
			return DateTime.UtcNow.Add(timeoutAfter.Value);
		}

		public static TimeSpan? None = null;
	}
}
