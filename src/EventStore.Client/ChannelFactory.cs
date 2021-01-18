using System;
using System.Net;
using Grpc.Core;

#if !GRPC_CORE
using System.Net.Http;
using System.Threading;
using Grpc.Net.Client;
#else
using System.Collections.Generic;
#endif

#nullable enable
namespace EventStore.Client {
	internal static class ChannelFactory {
		public static ChannelBase CreateChannel(EventStoreClientSettings settings, EndPoint endPoint) =>
			CreateChannel(settings, endPoint.ToUri(!settings.ConnectivitySettings.Insecure));

		public static ChannelBase CreateChannel(EventStoreClientSettings settings, Uri? address) {
			address ??= settings.ConnectivitySettings.Address;

#if !GRPC_CORE
			if (address.Scheme == Uri.UriSchemeHttp ||settings.ConnectivitySettings.Insecure) {
				//this must be switched on before creation of the HttpMessageHandler
				AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
			}

			return GrpcChannel.ForAddress(address, new GrpcChannelOptions {
				HttpClient = new HttpClient(CreateHandler(), true) {
					Timeout = Timeout.InfiniteTimeSpan,
					DefaultRequestVersion = new Version(2, 0),
				},
				LoggerFactory = settings.LoggerFactory,
				Credentials = settings.ChannelCredentials,
				DisposeHttpClient = true
			});

			HttpMessageHandler CreateHandler() {
				if (settings.CreateHttpMessageHandler != null) {
					return settings.CreateHttpMessageHandler.Invoke();
				}

				var handler = new SocketsHttpHandler();
				if (settings.ConnectivitySettings.KeepAlive.HasValue) {
					handler.KeepAlivePingDelay = settings.ConnectivitySettings.KeepAlive.Value;
				}

				return handler;
			}
#else
			return new Channel(address.Host, address.Port, settings.ChannelCredentials ?? ChannelCredentials.Insecure,
				GetChannelOptions());

			IEnumerable<ChannelOption> GetChannelOptions() {
				if (settings.ConnectivitySettings.KeepAlive.HasValue) {
					yield return new ChannelOption("grpc.keepalive_time_ms",
						(int)settings.ConnectivitySettings.KeepAlive.Value.TotalMilliseconds);
				}
			}
#endif
		}
	}
}
