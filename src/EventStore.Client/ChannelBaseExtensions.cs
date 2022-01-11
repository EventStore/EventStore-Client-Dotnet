using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace EventStore.Client {
	internal static class ChannelBaseExtensions {
		public static CancellationTokenSource GetCancellationTokenSource(this ChannelBase channel) {
			return
#if GRPC_CORE
			CancellationTokenSource.CreateLinkedTokenSource(((Channel) channel).ShutdownToken)
#else
			new CancellationTokenSource()
#endif
				;

		}
		public static async ValueTask DisposeAsync(this ChannelBase channel) {
			// for grpc.core, shutdown does the cleanup and the cast returns null
			// for grpc.net shutdown does nothing and dispose does the cleanup
			await channel.ShutdownAsync().ConfigureAwait(false);

			(channel as IDisposable)?.Dispose();
		}
	}
}
