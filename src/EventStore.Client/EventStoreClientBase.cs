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
		protected async Task<CallInvoker> SelectCallInvoker(CancellationToken cancellationToken) =>
			(Settings.Interceptors ?? Array.Empty<Interceptor>()).Aggregate(
				(await _channels.GetCurrentChannel(cancellationToken).ConfigureAwait(false)).CreateCallInvoker()
				.Intercept(new TypedExceptionInterceptor(_exceptionMap))
				.Intercept(new ConnectionNameInterceptor(ConnectionName))
				.Intercept(new ReportLeaderInterceptor(_channels.SetEndPoint)),
				(invoker, interceptor) => invoker.Intercept(interceptor));

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
