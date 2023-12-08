
using System.Diagnostics.CodeAnalysis;
using Grpc.Core;
using Grpc.Core.Interceptors;
using static EventStore.Client.Constants;
using static Grpc.Core.StatusCode;

namespace EventStore.Client.Interceptors;

class TypedExceptionInterceptor : Interceptor {
	static readonly Dictionary<string, Func<RpcException, Exception>> DefaultExceptionMap = new() {
		[Exceptions.AccessDenied] = ex => ex.ToAccessDeniedException(),
		[Exceptions.NotLeader]    = ex => ex.ToNotLeaderException(),
	};

	public TypedExceptionInterceptor(Dictionary<string, Func<RpcException, Exception>> customExceptionMap) {
		var map = new Dictionary<string, Func<RpcException, Exception>>(DefaultExceptionMap.Concat(customExceptionMap));

		ConvertRpcException = rpcEx => {
			if (rpcEx.TryMapException(map, out var ex))
				throw ex;

			throw rpcEx.StatusCode switch {
				Unavailable when rpcEx.Status.Detail == "Deadline Exceeded" => rpcEx.ToDeadlineExceededRpcException(),
				Unauthenticated                                             => rpcEx.ToNotAuthenticatedException(),
				_                                                           => rpcEx
			};
		};
	}

	Func<RpcException, Exception> ConvertRpcException { get; }

	public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		var response = continuation(request, context);

		return new AsyncServerStreamingCall<TResponse>(
			response.ResponseStream.Apply(ConvertRpcException),
			response.ResponseHeadersAsync,
			response.GetStatus,
			response.GetTrailers,
			response.Dispose
		);
	}

	public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		var response = continuation(context);

		return new AsyncClientStreamingCall<TRequest, TResponse>(
			response.RequestStream,
			response.ResponseAsync.Apply(ConvertRpcException),
			response.ResponseHeadersAsync,
			response.GetStatus,
			response.GetTrailers,
			response.Dispose
		);
	}

	public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncUnaryCallContinuation<TRequest, TResponse> continuation
	) {
		var response = continuation(request, context);

		return new AsyncUnaryCall<TResponse>(
			response.ResponseAsync.Apply(ConvertRpcException),
			response.ResponseHeadersAsync,
			response.GetStatus,
			response.GetTrailers,
			response.Dispose
		);
	}

	public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		var response = continuation(context);

		return new AsyncDuplexStreamingCall<TRequest, TResponse>(
			response.RequestStream,
			response.ResponseStream.Apply(ConvertRpcException),
			response.ResponseHeadersAsync,
			response.GetStatus,
			response.GetTrailers,
			response.Dispose
		);
	}
}

static class RpcExceptionConversionExtensions {
	public static IAsyncStreamReader<TRequest> Apply<TRequest>(this IAsyncStreamReader<TRequest> reader, Func<RpcException, Exception> convertException) => 
		new ExceptionConverterStreamReader<TRequest>(reader, convertException);

	// public static Task<TResponse> Apply<TResponse>(this Task<TResponse> task, Func<RpcException, Exception> convertException) => 
	// 	task.ContinueWith(t => t.Exception?.InnerException is RpcException ex ? throw convertException(ex) : t.Result);
	//
	public static async Task<TResponse> Apply<TResponse>(this Task<TResponse> task, Func<RpcException, Exception> convertException) {
		try {
			return await task.ConfigureAwait(false);
		}
		catch (Exception ex) {
			if (ex is RpcException rpcex)
				throw convertException(rpcex);

			if(ex is not OperationCanceledException)
				throw new Exception($"WTF!? {ex.Message}", ex);

			throw;
		}
	}
	
	public static AccessDeniedException ToAccessDeniedException(this RpcException exception) => 
		new(exception.Message, exception);

	public static NotLeaderException ToNotLeaderException(this RpcException exception) {
		var host = exception.Trailers.FirstOrDefault(x => x.Key == Exceptions.LeaderEndpointHost)?.Value!;
		var port = exception.Trailers.GetIntValueOrDefault(Exceptions.LeaderEndpointPort);
		return new NotLeaderException(host, port, exception);
	}
	
	public static NotAuthenticatedException ToNotAuthenticatedException(this RpcException exception) => 
		new(exception.Message, exception);

	public static RpcException ToDeadlineExceededRpcException(this RpcException exception) =>
		new(new Status(DeadlineExceeded, exception.Status.Detail, exception.Status.DebugException));

	public static bool TryMapException(this RpcException exception, Dictionary<string, Func<RpcException, Exception>> map, [MaybeNullWhen(false)] out Exception createdException) {
		if (exception.Trailers.TryGetValue(Exceptions.ExceptionKey, out var key) && map.TryGetValue(key!, out var factory)) {
			createdException = factory.Invoke(exception);
			return true;
		}

		createdException = null;
		return false;
	}
}

class ExceptionConverterStreamReader<TResponse>(IAsyncStreamReader<TResponse> reader, Func<RpcException, Exception> convertException) : IAsyncStreamReader<TResponse> {
	public TResponse Current => reader.Current;

	public async Task<bool> MoveNext(CancellationToken cancellationToken) {
		try {
			return await reader.MoveNext(cancellationToken).ConfigureAwait(false);
		}
		catch (RpcException ex) {
			throw convertException(ex);
		}
	}
}


internal class TypedExceptionInterceptorOld : Interceptor {
	private static readonly IDictionary<string, Func<RpcException, Exception>> DefaultExceptionMap =
		new Dictionary<string, Func<RpcException, Exception>> {
			[Constants.Exceptions.AccessDenied] = ex => new AccessDeniedException(ex.Message, ex),
			[Constants.Exceptions.NotLeader] = ex => new NotLeaderException(
					ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.LeaderEndpointHost)?.Value!,
					ex.Trailers.GetIntValueOrDefault(Constants.Exceptions.LeaderEndpointPort), ex)
		};

	private readonly IDictionary<string, Func<RpcException, Exception>> _exceptionMap;

	public TypedExceptionInterceptorOld(IDictionary<string, Func<RpcException, Exception>> exceptionMap) {
		_exceptionMap = new Dictionary<string, Func<RpcException, Exception>>(DefaultExceptionMap);
		foreach (var pair in exceptionMap) {
			_exceptionMap.Add(pair);
		}
	}

	public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) {
		var response = continuation(request, context);

		return new AsyncServerStreamingCall<TResponse>(
			new AsyncStreamReader<TResponse>(_exceptionMap, response.ResponseStream),
			response.ResponseHeadersAsync, response.GetStatus, response.GetTrailers, response.Dispose);
	}

	public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) {
		var response = continuation(context);

		return new AsyncClientStreamingCall<TRequest, TResponse>(
			response.RequestStream,
			response.ResponseAsync.ContinueWith(t => t.Exception?.InnerException is RpcException ex
				? throw ConvertRpcException(ex, _exceptionMap)
				: t.Result),
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

		return new AsyncUnaryCall<TResponse>(
			response.ResponseAsync.ContinueWith(
				t =>
					t.Exception?.InnerException is RpcException ex
						? throw ConvertRpcException(ex, _exceptionMap)
						: t.Result
			),
			response.ResponseHeadersAsync,
			response.GetStatus,
			response.GetTrailers,
			response.Dispose
		);
	}

	public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) {
		var response = continuation(context);

		return new AsyncDuplexStreamingCall<TRequest, TResponse>(
			response.RequestStream,
			new AsyncStreamReader<TResponse>(_exceptionMap, response.ResponseStream),
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
			false => (ex.StatusCode, ex.Status.Detail) switch {
				(StatusCode.Unavailable, "Deadline Exceeded") => new RpcException(new Status(
					StatusCode.DeadlineExceeded, ex.Status.Detail, ex.Status.DebugException)),
				(StatusCode.DeadlineExceeded, _) => ex,
				(StatusCode.Unauthenticated, _) => new NotAuthenticatedException(ex.Message, ex),
				_ => ex
			}
		};
	}

	private class AsyncStreamReader<TResponse> : IAsyncStreamReader<TResponse> {
		private readonly IDictionary<string, Func<RpcException, Exception>> _exceptionMap;
		private readonly IAsyncStreamReader<TResponse> _inner;

		public AsyncStreamReader(IDictionary<string, Func<RpcException, Exception>> exceptionMap,
			IAsyncStreamReader<TResponse> inner) {
			_exceptionMap = exceptionMap;
			_inner = inner;
		}

		public async Task<bool> MoveNext(CancellationToken cancellationToken) {
			try {
				return await _inner.MoveNext(cancellationToken).ConfigureAwait(false);
			} catch (RpcException ex) {
				throw ConvertRpcException(ex, _exceptionMap);
			}
		}

		public TResponse Current => _inner.Current;
	}
}

