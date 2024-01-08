using Grpc.Core;

namespace EventStore.Client;

static class ChannelBaseExtensions {
	public static async ValueTask DisposeAsync(this ChannelBase channel) {
		await channel.ShutdownAsync().ConfigureAwait(false);
		(channel as IDisposable)?.Dispose();
	}
}