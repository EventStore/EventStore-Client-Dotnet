using Grpc.Core;
using Grpc.Core.Interceptors;

namespace EventStore.Client.Interceptors {
	// this has become more general than just detecting leader changes.
	// triggers the action on any rpc exception with StatusCode.Unavailable
	internal class ReportLeaderInterceptor : Interceptor {
		private readonly Action<ReconnectionRequired> _onReconnectionRequired;

		private const TaskContinuationOptions ContinuationOptions =
			TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted;

		internal ReportLeaderInterceptor(Action<ReconnectionRequired> onReconnectionRequired) {
			_onReconnectionRequired = onReconnectionRequired;
		}

		public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncUnaryCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(request, context);

			response.ResponseAsync.ContinueWith(OnReconnectionRequired, ContinuationOptions);

			return new AsyncUnaryCall<TResponse>(response.ResponseAsync, response.ResponseHeadersAsync,
				response.GetStatus, response.GetTrailers, response.Dispose);
		}

		public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(context);

			response.ResponseAsync.ContinueWith(OnReconnectionRequired, ContinuationOptions);

			return new AsyncClientStreamingCall<TRequest, TResponse>(
				new StreamWriter<TRequest>(response.RequestStream, OnReconnectionRequired),
				response.ResponseAsync,
				response.ResponseHeadersAsync, response.GetStatus, response.GetTrailers, response.Dispose);
		}

		public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(context);

			return new AsyncDuplexStreamingCall<TRequest, TResponse>(
				new StreamWriter<TRequest>(response.RequestStream, OnReconnectionRequired),
				new StreamReader<TResponse>(response.ResponseStream, OnReconnectionRequired),
				response.ResponseHeadersAsync,
				response.GetStatus, response.GetTrailers, response.Dispose);
		}

		public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
			TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
			AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(request, context);

			return new AsyncServerStreamingCall<TResponse>(
				new StreamReader<TResponse>(response.ResponseStream, OnReconnectionRequired),
				response.ResponseHeadersAsync,
				response.GetStatus, response.GetTrailers, response.Dispose);
		}

		private void OnReconnectionRequired(Task task) {
			ReconnectionRequired reconnectionRequired = task.Exception?.InnerException switch {
				NotLeaderException ex => new ReconnectionRequired.NewLeader(ex.LeaderEndpoint),
				RpcException {
					StatusCode: StatusCode.Unavailable
					// or StatusCode.Unknown or TODO: use RPC exceptions on server 
				} => ReconnectionRequired.Rediscover.Instance,
				_ => ReconnectionRequired.None.Instance
			};

			if (reconnectionRequired is not ReconnectionRequired.None)
				_onReconnectionRequired(reconnectionRequired);
		}

		private class StreamWriter<T>(IClientStreamWriter<T> inner, Action<Task> reportNewLeader) : IClientStreamWriter<T> {
			public WriteOptions? WriteOptions {
				get => inner.WriteOptions;
				set => inner.WriteOptions = value;
			}

			public Task CompleteAsync() {
				var task = inner.CompleteAsync();
				task.ContinueWith(reportNewLeader, ContinuationOptions);
				return task;
			}

			public Task WriteAsync(T message) {
				var task = inner.WriteAsync(message);
				task.ContinueWith(reportNewLeader, ContinuationOptions);
				return task;
			}
		}

		private class StreamReader<T>(IAsyncStreamReader<T> inner, Action<Task> reportNewLeader) : IAsyncStreamReader<T> {
			public Task<bool> MoveNext(CancellationToken cancellationToken) {
				var task = inner.MoveNext(cancellationToken);
				task.ContinueWith(reportNewLeader, ContinuationOptions);
				return task;
			}

			public T Current => inner.Current;
		}
	}
}
