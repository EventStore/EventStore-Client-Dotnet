using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace EventStore.Client.Interceptors {
	internal class ReportLeaderInterceptor : Interceptor {
		private readonly Action<EndPoint> _reportNewLeader;

		private const TaskContinuationOptions ContinuationOptions =
			TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted;

		public ReportLeaderInterceptor(Action<EndPoint> reportNewLeader) {
			_reportNewLeader = reportNewLeader;
		}

		public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncUnaryCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(request, context);

			response.ResponseAsync.ContinueWith(ReportNewLeader, ContinuationOptions);

			return new AsyncUnaryCall<TResponse>(response.ResponseAsync, response.ResponseHeadersAsync,
				response.GetStatus, response.GetTrailers, response.Dispose);
		}

		public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(context);

			response.ResponseAsync.ContinueWith(ReportNewLeader, ContinuationOptions);

			return new AsyncClientStreamingCall<TRequest, TResponse>(response.RequestStream, response.ResponseAsync,
				response.ResponseHeadersAsync, response.GetStatus, response.GetTrailers, response.Dispose);
		}

		public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(context);

			return new AsyncDuplexStreamingCall<TRequest, TResponse>(response.RequestStream,
				new StreamReader<TResponse>(response.ResponseStream, ReportNewLeader), response.ResponseHeadersAsync,
				response.GetStatus, response.GetTrailers, response.Dispose);
		}

		public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
			TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
			AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(request, context);

			return new AsyncServerStreamingCall<TResponse>(
				new StreamReader<TResponse>(response.ResponseStream, ReportNewLeader), response.ResponseHeadersAsync,
				response.GetStatus, response.GetTrailers, response.Dispose);
		}

		private void ReportNewLeader<TResponse>(Task<TResponse> task) {
			if (task.Exception!.InnerException is NotLeaderException ex) {
				_reportNewLeader(ex.LeaderEndpoint);
			}
		}

		private class StreamReader<T> : IAsyncStreamReader<T> {
			private readonly IAsyncStreamReader<T> _inner;
			private readonly Action<Task<bool>> _reportNewLeader;

			public StreamReader(IAsyncStreamReader<T> inner, Action<Task<bool>> reportNewLeader) {
				_inner = inner;
				_reportNewLeader = reportNewLeader;
			}

			public Task<bool> MoveNext(CancellationToken cancellationToken) {
				var task = _inner.MoveNext(cancellationToken);
				task.ContinueWith(_reportNewLeader, ContinuationOptions);
				return task;
			}

			public T Current => _inner.Current;
		}
	}
}
