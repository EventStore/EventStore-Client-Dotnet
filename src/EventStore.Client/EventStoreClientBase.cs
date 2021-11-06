using System;
using System.Collections.Generic;
using System.Linq;
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
	public abstract class EventStoreClientBase : IDisposable, IAsyncDisposable {
		private readonly IDictionary<string, Func<RpcException, Exception>> _exceptionMap;
		private readonly MultiChannel _channels;
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
			_exceptionMap = exceptionMap;
			Settings = settings ?? new EventStoreClientSettings();

			ConnectionName = Settings.ConnectionName ?? $"ES-{Guid.NewGuid()}";

			_channels = new MultiChannel(Settings);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		protected async Task<CallInvoker> SelectCallInvoker(CancellationToken cancellationToken) {
			var (invoker, _) =  await SelectCallInvokerAndCapabilities(cancellationToken).ConfigureAwait(false);
			return invoker;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		protected async Task<(CallInvoker, ServerCapabilities)> SelectCallInvokerAndCapabilities(CancellationToken cancellationToken) {
			var (endpoint, channel, capabilities) = await _channels.GetCurrentChannel(cancellationToken).ConfigureAwait(false);

			return ((Settings.Interceptors ?? Array.Empty<Interceptor>()).Aggregate(
				channel.CreateCallInvoker()
					.Intercept(new TypedExceptionInterceptor(_exceptionMap))
					.Intercept(new ConnectionNameInterceptor(ConnectionName))
					.Intercept(new ReportLeaderInterceptor(_channels.SetEndPoint))
					.Intercept(new EvictChannelInterceptor(endpoint, _channels.EvictChannel)),
				(invoker, interceptor) => invoker.Intercept(interceptor)), await capabilities.ConfigureAwait(false));
		}

		/// <inheritdoc />
		public void Dispose() => _channels.Dispose();

		/// <inheritdoc />
		public ValueTask DisposeAsync() => _channels.DisposeAsync();

		/// <summary>
		///
		/// </summary>
		/// <param name="option">The invalid option</param>
		/// <typeparam name="T">The type of the option</typeparam>
		protected Exception InvalidOption<T>(T option) where T : Enum => new InvalidOperationException($"The {typeof(T).Name} {option:x} was not valid.");
	}
}
