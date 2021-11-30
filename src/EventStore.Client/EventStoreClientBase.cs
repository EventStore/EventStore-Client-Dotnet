using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Interceptors;
using Grpc.Core;
using Grpc.Core.Interceptors;
#if !GRPC_CORE
using Grpc.Net.Client;
#endif

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// The base class used by clients used to communicate with the EventStoreDB.
	/// </summary>
	public abstract class EventStoreClientBase : IDisposable, IAsyncDisposable {
		private readonly IDictionary<string, Func<RpcException, Exception>> _exceptionMap;
		private readonly IChannelSelector _channelSelector;
		private readonly CancellationTokenSource _cts;
		private Lazy<Task<ChannelInfo>> _channelInfoLazy;

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
			_channelSelector = new ChannelSelector(Settings, _cts.Token);
			_channelInfoLazy = new Lazy<Task<ChannelInfo>>(() => _channelSelector.SelectChannel(_cts.Token),
				LazyThreadSafetyMode.PublicationOnly);

			ConnectionName = Settings.ConnectionName ?? $"ES-{Guid.NewGuid()}";
		}

#pragma warning disable 1591
		protected Task<ChannelInfo> GetCurrentChannelInfo() => _channelInfoLazy.Value;
#pragma warning restore 1591

#pragma warning disable 1591
		protected CallInvoker CreateCallInvoker(ChannelBase channel) =>
			(Settings.Interceptors ?? Array.Empty<Interceptor>()).Aggregate(
				channel.CreateCallInvoker()
					.Intercept(new TypedExceptionInterceptor(_exceptionMap))
					.Intercept(new ConnectionNameInterceptor(ConnectionName))
					.Intercept(new ReportLeaderInterceptor(endPoint => {
						_channelInfoLazy = new Lazy<Task<ChannelInfo>>(
							() => _channelSelector.SelectChannel(_cts.Token),
							LazyThreadSafetyMode.PublicationOnly);
						_channelSelector.SetEndPoint(endPoint);
					})),
				(invoker, interceptor) => invoker.Intercept(interceptor));
#pragma warning restore 1591

		/// <inheritdoc />
		public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

		/// <inheritdoc />
		public ValueTask DisposeAsync() {
			_cts.Cancel();
			_cts.Dispose();
			return _channelSelector.DisposeAsync();
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
