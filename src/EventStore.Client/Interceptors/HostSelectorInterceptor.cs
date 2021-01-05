using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;

#nullable enable
namespace EventStore.Client.Interceptors {
	internal class HostSelectorInterceptor : Interceptor {
		private readonly string _requiresLeaderHeaderValue;
		private readonly IEndpointDiscoverer _endpointDiscoverer;
		private Lazy<Task<EndPoint>> _selectedEndpoint;

		public HostSelectorInterceptor(IEndpointDiscoverer endpointDiscoverer, NodePreference nodePreference) {
			_endpointDiscoverer = endpointDiscoverer;
			_requiresLeaderHeaderValue = nodePreference == NodePreference.Leader ? bool.TrueString : bool.FalseString;
			_selectedEndpoint = DeferEndpointSelection();
		}

		private Lazy<Task<EndPoint>> DeferEndpointSelection(CancellationToken cancellationToken = default) =>
			new Lazy<Task<EndPoint>>(() => _endpointDiscoverer.DiscoverAsync(cancellationToken),
				LazyThreadSafetyMode.ExecutionAndPublication);

		private void ScheduleEndpointSelection<T>(Task<T> _) =>
			Interlocked.Exchange(ref _selectedEndpoint, DeferEndpointSelection());

		public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncUnaryCallContinuation<TRequest, TResponse> continuation) {
			var call = continuation(request, CreateClientInterceptorContext(context));

			call.ResponseAsync.ContinueWith(ScheduleEndpointSelection,
				TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

			return new AsyncUnaryCall<TResponse>(call.ResponseAsync, call.ResponseHeadersAsync, call.GetStatus,
				call.GetTrailers,
				call.Dispose);
		}

		public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) {
			var call = continuation(CreateClientInterceptorContext(context));

			call.ResponseAsync.ContinueWith(ScheduleEndpointSelection,
				TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

			return new AsyncClientStreamingCall<TRequest, TResponse>(call.RequestStream, call.ResponseAsync,
				call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
		}

		public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
			TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
			AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) {
			var call = continuation(request, CreateClientInterceptorContext(context));

			return new AsyncServerStreamingCall<TResponse>(
				new StreamReader<TResponse>(call.ResponseStream, ScheduleEndpointSelection), call.ResponseHeadersAsync,
				call.GetStatus, call.GetTrailers, call.Dispose);
		}

		public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) {
			var call = continuation(CreateClientInterceptorContext(context));

			return new AsyncDuplexStreamingCall<TRequest, TResponse>(call.RequestStream,
				new StreamReader<TResponse>(call.ResponseStream, ScheduleEndpointSelection), call.ResponseHeadersAsync,
				call.GetStatus, call.GetTrailers, call.Dispose);
		}

		private ClientInterceptorContext<TRequest, TResponse> CreateClientInterceptorContext<TRequest, TResponse>(
			ClientInterceptorContext<TRequest, TResponse> context) where TRequest : class where TResponse : class {
			context.Options.Headers.Add("requires-leader", _requiresLeaderHeaderValue);

			return new ClientInterceptorContext<TRequest, TResponse>(context.Method, SelectHost(), context.Options);
		}

		private string SelectHost() =>
			$"{_selectedEndpoint.Value.Result.GetHost()}:{_selectedEndpoint.Value.Result.GetPort()}";

		private class StreamReader<T> : IAsyncStreamReader<T> {
			private readonly IAsyncStreamReader<T> _inner;
			private readonly Action<Task<bool>> _callback;

			public StreamReader(IAsyncStreamReader<T> inner, Action<Task<bool>> callback) {
				_inner = inner;
				_callback = callback;
			}

			public Task<bool> MoveNext(CancellationToken cancellationToken) {
				var task = _inner.MoveNext(cancellationToken);

				task.ContinueWith(_callback,
					TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

				return task;
			}

			public T Current { get; } = default!;
		}
	}
}
