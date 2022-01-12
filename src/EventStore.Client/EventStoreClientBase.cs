using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Interceptors;
using Grpc.Core;
using Grpc.Core.Interceptors;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// The base class used by clients used to communicate with the EventStoreDB.
	/// </summary>
	public abstract class EventStoreClientBase :
#if !GRPC_CORE
		IDisposable, // for grpc.net we can dispose synchronously, but not for grpc.core
#endif
		IAsyncDisposable {

		private readonly IDictionary<string, Func<RpcException, Exception>> _exceptionMap;
		private readonly CancellationTokenSource _cts;
		private readonly ChannelCache _channelCache;
		private readonly SharingProvider<DnsEndPoint?, ChannelInfo> _channelInfoProvider;

		/// <summary>
		/// The name of the connection.
		/// </summary>
		public string ConnectionName { get; }

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
			_exceptionMap = exceptionMap;
			_cts = new CancellationTokenSource();
			_channelCache = new(Settings);

			ConnectionName = Settings.ConnectionName ?? $"ES-{Guid.NewGuid()}";

			var channelSelector = new ChannelSelector(Settings, _channelCache);
			_channelInfoProvider = new SharingProvider<DnsEndPoint?, ChannelInfo>(
				factory: (endPoint, onBroken) => GetChannelInfo(endPoint, onBroken, channelSelector, _cts.Token),
				initialInput: null);
		}

		// Select a channel and query its capabilities. This is an expensive call that
		// we don't want to do often.
		private async Task<ChannelInfo> GetChannelInfo(
			DnsEndPoint? endPoint,
			Action<DnsEndPoint?> onBroken,
			IChannelSelector channelSelector,
			CancellationToken cancellationToken) {

			var channel = endPoint is null
				? await channelSelector.SelectChannelAsync(cancellationToken).ConfigureAwait(false)
				: channelSelector.SelectChannel(endPoint);

			var invoker = channel.CreateCallInvoker()
				.Intercept(new TypedExceptionInterceptor(_exceptionMap))
				.Intercept(new ConnectionNameInterceptor(ConnectionName))
				.Intercept(new ReportLeaderInterceptor(onBroken));

			if (Settings.Interceptors is not null) {
				foreach (var interceptor in Settings.Interceptors) {
					invoker = invoker.Intercept(interceptor);
				}
			}

			var caps = await new GrpcServerCapabilitiesClient(Settings)
				.GetAsync(invoker, cancellationToken)
				.ConfigureAwait(false);

			return new(channel, caps, invoker);
		}

#pragma warning disable 1591
		protected async ValueTask<ChannelInfo> GetChannelInfo(CancellationToken cancellationToken) =>
			await _channelInfoProvider.CurrentAsync.WithCancellation(cancellationToken).ConfigureAwait(false);
#pragma warning restore 1591

		// only exists so that we can manually trigger rediscovery in the tests (by reflection)
		// in cases where the server doesn't yet let the client know that it needs to.
		// see EventStoreClientExtensions.WarmUpWith.
		// note if rediscovery is already in progress it will continue, not restart.
		private void Rediscover() {
			_channelInfoProvider.Reset();
		}

#if !GRPC_CORE
		/// <inheritdoc />
		public void Dispose() {
			_cts.Cancel();
			_cts.Dispose();
			_channelCache.Dispose();
		}
#endif

		/// <inheritdoc />
		public async ValueTask DisposeAsync() {
			_cts.Cancel();
			_cts.Dispose();
			await _channelCache.DisposeAsync().ConfigureAwait(false);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="option">The invalid option</param>
		/// <typeparam name="T">The type of the option</typeparam>
		protected Exception InvalidOption<T>(T option) where T : Enum =>
			new InvalidOperationException($"The {typeof(T).Name} {option:x} was not valid.");
	}
}
