using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Client.Interceptors;
using Grpc.Core;
using Grpc.Core.Interceptors;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// The base class used by clients used to communicate with the EventStoreDB.
	/// </summary>
	public abstract class EventStoreClientBase : IDisposable {
		private readonly ChannelBase _channel;

		/// <summary>
		/// The <see cref="CallInvoker"/>.
		/// </summary>
		protected CallInvoker CallInvoker { get; }

		/// <summary>
		/// The <see cref="EventStoreClientSettings"/>.
		/// </summary>
		protected EventStoreClientSettings Settings { get; }

		/// <summary>
		/// Constructs a new <see cref="EventStoreClientBase"/>.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="exceptionMap"></param>
		protected EventStoreClientBase(EventStoreClientSettings? settings,
			IDictionary<string, Func<RpcException, Exception>> exceptionMap) {
			Settings = settings ?? new EventStoreClientSettings();

			var connectionName = Settings.ConnectionName ?? $"ES-{Guid.NewGuid()}";

			_channel = ChannelFactory.CreateChannel(Settings, Settings.ConnectivitySettings.Address);

			CallInvoker = (Settings.Interceptors ?? Array.Empty<Interceptor>()).Aggregate(
				_channel.CreateCallInvoker()
					.Intercept(new TypedExceptionInterceptor(exceptionMap, ex => { }))
					.Intercept(new ConnectionNameInterceptor(connectionName))
#if GRPC_CORE
					.Intercept(new HostSelectorInterceptor(Settings.ConnectivitySettings.IsSingleNode
							? (IEndpointDiscoverer)new SingleNodeEndpointDiscoverer(
								Settings.ConnectivitySettings.Address)
							: new GossipBasedEndpointDiscoverer(Settings.ConnectivitySettings,
								new GrpcGossipClient(Settings)),
						Settings.ConnectivitySettings.NodePreference))
#endif
				,
				(invoker, interceptor) => invoker.Intercept(interceptor));
		}

		/// <inheritdoc />
		public void Dispose() {
			// ReSharper disable SuspiciousTypeConversion.Global
			if (_channel is IDisposable disposable) {
				// ReSharper restore SuspiciousTypeConversion.Global
				disposable.Dispose();
			}
		}
	}
}
