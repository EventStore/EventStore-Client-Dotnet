using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;

#nullable enable
namespace EventStore.Client.Interceptors {
	internal class TypedExceptionInterceptor : Interceptor {
		private static readonly IDictionary<string, Func<RpcException, Exception>> DefaultExceptionMap =
			new Dictionary<string, Func<RpcException, Exception>> {
				[Constants.Exceptions.AccessDenied] = ex => new AccessDeniedException(ex.Message, ex),
				[Constants.Exceptions.NotLeader] = ex => new NotLeaderException(IPEndPoint.Parse(ex.Trailers
					.FirstOrDefault(x => x.Key == Constants.Exceptions.LeaderEndpoint)?.Value))
			};

		private readonly Action<Exception>? _exceptionOccurred;
		private readonly IDictionary<string, Func<RpcException, Exception>> _exceptionMap;

		public TypedExceptionInterceptor(IDictionary<string, Func<RpcException, Exception>> exceptionMap,
			Action<Exception>? exceptionOccurred = null) {
			_exceptionMap = new Dictionary<string, Func<RpcException, Exception>>(DefaultExceptionMap);
			foreach (var pair in exceptionMap) {
				_exceptionMap.Add(pair);
			}
			_exceptionOccurred = exceptionOccurred;
		}

		public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
			TRequest request,
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(request, context);

			return new AsyncServerStreamingCall<TResponse>(
				new AsyncStreamReader<TResponse>(_exceptionOccurred, _exceptionMap, response.ResponseStream),
				response.ResponseHeadersAsync, response.GetStatus, response.GetTrailers, response.Dispose);
		}

		public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(context);

			return new AsyncClientStreamingCall<TRequest, TResponse>(
				response.RequestStream,
				response.ResponseAsync.ContinueWith(t => {
					if (t.Exception?.InnerException is RpcException ex) {
						var exception = ConvertRpcException(ex, _exceptionMap);
						_exceptionOccurred?.Invoke(exception);
						throw exception;
					}

					return t.Result;
				}),
				response.ResponseHeadersAsync,
				response.GetStatus,
				response.GetTrailers,
				response.Dispose);
		}

		public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
			TRequest request,
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncUnaryCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(request, context);

			return new AsyncUnaryCall<TResponse>(response.ResponseAsync.ContinueWith(t => {
				if (t.Exception?.InnerException is RpcException ex) {
					var exception = ConvertRpcException(ex, _exceptionMap);
					_exceptionOccurred?.Invoke(exception);
					throw exception;
				}

				return t.Result;
			}), response.ResponseHeadersAsync, response.GetStatus, response.GetTrailers, response.Dispose);
		}

		public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(context);

			return new AsyncDuplexStreamingCall<TRequest, TResponse>(
				response.RequestStream,
				new AsyncStreamReader<TResponse>(_exceptionOccurred, _exceptionMap, response.ResponseStream),
				response.ResponseHeadersAsync,
				response.GetStatus,
				response.GetTrailers,
				response.Dispose);
		}

		private static Exception ConvertRpcException(RpcException ex,
			IDictionary<string, Func<RpcException, Exception>> exceptionMap) {
			Func<RpcException, Exception>? factory = null;
			return (ex.Trailers.TryGetValue(Constants.Exceptions.ExceptionKey, out var key) &&
			        exceptionMap.TryGetValue(key!, out factory)) switch {
				true => factory!.Invoke(ex),
				false => ex.StatusCode switch {
					StatusCode.DeadlineExceeded => ex,
					StatusCode.Unauthenticated => new NotAuthenticatedException(ex.Message, ex),
					_ => new InvalidOperationException(ex.Message, ex)
				}
			};
		}


		/*		private static Exception ConvertRpcException(RpcException ex) {
			return ex.Trailers.TryGetValue(Constants.Exceptions.ExceptionKey, out var value) switch {
				true => value switch {
					Constants.Exceptions.PersistentSubscriptionDoesNotExist => new
						PersistentSubscriptionNotFoundException(
							ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value,
							ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.GroupName)?.Value, ex),
					Constants.Exceptions.MaximumSubscribersReached => new
						MaximumSubscribersReachedException(
							ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value,
							ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.GroupName)?.Value, ex),
					Constants.Exceptions.PersistentSubscriptionDropped => new
						PersistentSubscriptionDroppedByServerException(
							ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value,
							ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.GroupName)?.Value, ex),
				},
				false => ex.StatusCode switch {
					StatusCode.DeadlineExceeded => ex,
					StatusCode.Unauthenticated => new NotAuthenticatedException(ex.Message, ex),
					_ => new InvalidOperationException(ex.Message, ex)
				}
			};
		}
*/
		class AsyncStreamReader<TResponse> : IAsyncStreamReader<TResponse> {
			private readonly Action<Exception>? _exceptionOccurred;
			private readonly IDictionary<string, Func<RpcException, Exception>> _exceptionMap;
			private readonly IAsyncStreamReader<TResponse> _inner;

			public AsyncStreamReader(Action<Exception>? exceptionOccurred,
				IDictionary<string, Func<RpcException, Exception>> exceptionMap, IAsyncStreamReader<TResponse> inner) {
				_exceptionOccurred = exceptionOccurred;
				_exceptionMap = exceptionMap;
				_inner = inner;
			}

			public async Task<bool> MoveNext(CancellationToken cancellationToken) {
				try {
					return await _inner.MoveNext(cancellationToken).ConfigureAwait(false);
				} catch (RpcException ex) {
					var exception = ConvertRpcException(ex, _exceptionMap);
					_exceptionOccurred?.Invoke(exception);
					throw exception;
				}
			}

			public TResponse Current => _inner.Current;
		}
	}
}
