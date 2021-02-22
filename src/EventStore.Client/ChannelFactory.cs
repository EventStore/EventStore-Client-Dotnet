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

				return new SocketsHttpHandler {
					KeepAlivePingDelay = settings.ConnectivitySettings.KeepAliveInterval,
					KeepAlivePingTimeout = settings.ConnectivitySettings.KeepAliveTimeout
				};
			}
#else
			return new Channel(address.Host, address.Port, settings.ChannelCredentials ?? ChannelCredentials.Insecure,
				GetChannelOptions());

			IEnumerable<ChannelOption> GetChannelOptions() {
				yield return new ChannelOption("grpc.keepalive_time_ms",
					GetValue((int)settings.ConnectivitySettings.KeepAliveInterval.TotalMilliseconds));

				yield return new ChannelOption("grpc.keepalive_timeout_ms",
					GetValue((int)settings.ConnectivitySettings.KeepAliveTimeout.TotalMilliseconds));
			}

			static int GetValue(int value) => value switch {
				{ } v when v < 0 => int.MaxValue,
				_ => value
			};
#endif
		}
	}
}
