using System;
using Grpc.Core;
using EndPoint = System.Net.EndPoint;
#if !GRPC_CORE
using System.Net.Http;
using Grpc.Net.Client;
using TChannel = Grpc.Net.Client.GrpcChannel;
#else
using System.Collections.Generic;
using TChannel = Grpc.Core.ChannelBase;
#endif

#nullable enable
namespace EventStore.Client {
	internal static class ChannelFactory {
		private const int MaxReceiveMessageLength = 17 * 1024 * 1024;

		public static TChannel CreateChannel(EventStoreClientSettings settings, EndPoint endPoint, bool https) =>
			CreateChannel(settings, endPoint.ToUri(https));

		public static TChannel CreateChannel(EventStoreClientSettings settings, Uri? address) {
			address ??= settings.ConnectivitySettings.Address;

#if !GRPC_CORE
			if (address.Scheme == Uri.UriSchemeHttp ||settings.ConnectivitySettings.Insecure) {
				//this must be switched on before creation of the HttpMessageHandler
				AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
			}

			return TChannel.ForAddress(address, new GrpcChannelOptions {
				HttpClient = new HttpClient(CreateHandler(), true) {
					Timeout = System.Threading.Timeout.InfiniteTimeSpan,
					DefaultRequestVersion = new Version(2, 0),
				},
				LoggerFactory = settings.LoggerFactory,
				Credentials = settings.ChannelCredentials,
				DisposeHttpClient = true,
				MaxReceiveMessageSize = MaxReceiveMessageLength
			});

			HttpMessageHandler CreateHandler() {
				if (settings.CreateHttpMessageHandler != null) {
					return settings.CreateHttpMessageHandler.Invoke();
				}

				return new SocketsHttpHandler {
					KeepAlivePingDelay = settings.ConnectivitySettings.KeepAliveInterval,
					KeepAlivePingTimeout = settings.ConnectivitySettings.KeepAliveTimeout,
					EnableMultipleHttp2Connections = true
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

				yield return new ChannelOption("grpc.max_receive_message_length", MaxReceiveMessageLength);
			}

			static int GetValue(int value) => value switch {
				{ } v when v < 0 => int.MaxValue,
				_ => value
			};
#endif
		}
	}
}
