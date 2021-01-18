using System;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace connecting_to_a_single_node {
	#region interceptor
	public class DemoInterceptor : Interceptor {
		public override AsyncServerStreamingCall<TResponse>
			AsyncServerStreamingCall<TRequest, TResponse>(
				TRequest request,
				ClientInterceptorContext<TRequest, TResponse> context,
				AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) {
			Console.WriteLine($"AsyncServerStreamingCall: {context.Method.FullName}");

			return base.AsyncServerStreamingCall(request, context, continuation);
		}

		public override AsyncClientStreamingCall<TRequest, TResponse>
			AsyncClientStreamingCall<TRequest, TResponse>(
				ClientInterceptorContext<TRequest, TResponse> context,
				AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) {
			Console.WriteLine($"AsyncClientStreamingCall: {context.Method.FullName}");

			return base.AsyncClientStreamingCall(context, continuation);
		}
	}
	#endregion interceptor
}
