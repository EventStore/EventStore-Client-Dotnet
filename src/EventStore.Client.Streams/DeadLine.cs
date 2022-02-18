using System;

#nullable enable
namespace EventStore.Client {
#pragma warning disable CS1591
	public static class DeadLine {
#pragma warning restore CS1591
		/// <summary>
		/// Represents no deadline (i.e., wait infinitely)
		/// </summary>
		public static TimeSpan? None = null;
	}
}
