using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace EventStore.Client {
	internal static class ChannelBaseExtensions {
		public static async ValueTask DisposeAsync(this ChannelBase channel) {
			await channel.ShutdownAsync().ConfigureAwait(false);

			(channel as IDisposable)?.Dispose();
		}
	}
}
