
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

#if NET48
	public TypedExceptionInterceptor(Dictionary<string, Func<RpcException, Exception>> customExceptionMap) {
		var map = new Dictionary<string, Func<RpcException, Exception>>(DefaultExceptionMap);
#else
	public TypedExceptionInterceptor(Dictionary<string, Func<RpcException, Exception>> customExceptionMap) {
		var map = new Dictionary<string, Func<RpcException, Exception>>(DefaultExceptionMap.Concat(customExceptionMap));
#endif

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

	public static Task<TResponse> Apply<TResponse>(this Task<TResponse> task, Func<RpcException, Exception> convertException) => 
		task.ContinueWith(t => t.Exception?.InnerException is RpcException ex ? throw convertException(ex) : t.Result);
	
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

#if NET48
	public static bool TryMapException(this RpcException exception, Dictionary<string, Func<RpcException, Exception>> map, out Exception createdException) {
		if (exception.Trailers.TryGetValue(Exceptions.ExceptionKey, out var key) && map.TryGetValue(key!, out var factory)) {
			createdException = factory.Invoke(exception);
			return true;
		}

		createdException = null!;
		return false;
	}
#else
	public static bool TryMapException(this RpcException exception, Dictionary<string, Func<RpcException, Exception>> map, [MaybeNullWhen(false)] out Exception createdException) {
		if (exception.Trailers.TryGetValue(Exceptions.ExceptionKey, out var key) && map.TryGetValue(key!, out var factory)) {
			createdException = factory.Invoke(exception);
			return true;
		}

		createdException = null;
		return false;
	}
#endif
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
