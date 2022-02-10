using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Xunit;

namespace EventStore.Client.Interceptors {
	public class ReportLeaderInterceptorTests {
		public delegate Task GrpcCall(Interceptor interceptor, Task<object> response = null);

		private static readonly Marshaller<object> _marshaller = new(_ => Array.Empty<byte>(), _ => new object());

		private static readonly StatusCode[] ForcesRediscoveryStatusCodes = {
			StatusCode.Aborted, 
			//StatusCode.Unknown, TODO: use RPC exceptions on server
			StatusCode.Unavailable
		};


		private static IEnumerable<GrpcCall> GrpcCalls() {
			yield return MakeUnaryCall;
			yield return MakeClientStreamingCall;
			yield return MakeDuplexStreamingCall;
			yield return MakeServerStreamingCall;
		}

		public static IEnumerable<object[]> ReportsNewLeaderCases() => GrpcCalls().Select(call => new object[] {call});

		[Theory, MemberData(nameof(ReportsNewLeaderCases))]
		public async Task ReportsNewLeader(GrpcCall call) {
			EndPoint actual = default;
			var sut = new ReportLeaderInterceptor(ep => actual = ep);

			var result = await Assert.ThrowsAsync<NotLeaderException>(() =>
				call(sut, Task.FromException<object>(new NotLeaderException("a.host", 2112))));
			Assert.Equal(result.LeaderEndpoint, actual);
		}

		public static IEnumerable<object[]> ForcesRediscoveryCases() => from call in GrpcCalls()
			from statusCode in ForcesRediscoveryStatusCodes
			select new object[] {call, statusCode};

		[Theory, MemberData(nameof(ForcesRediscoveryCases))]
		public async Task ForcesRediscovery(GrpcCall call, StatusCode statusCode) {
			EndPoint actual = default;
			bool invoked = false;

			var sut = new ReportLeaderInterceptor(ep => {
				invoked = true;
				actual = ep;
			});

			var result = await Assert.ThrowsAsync<RpcException>(() => call(sut,
				Task.FromException<object>(new RpcException(new Status(statusCode, "oops")))));
			Assert.Null(actual);
			Assert.True(invoked);
		}
		
		public static IEnumerable<object[]> DoesNotForceRediscoveryCases() => from call in GrpcCalls()
			from statusCode in Enum.GetValues(typeof(StatusCode))
				.OfType<StatusCode>()
				.Except(ForcesRediscoveryStatusCodes)
			select new object[] {call, statusCode};

		[Theory, MemberData(nameof(DoesNotForceRediscoveryCases))]
		public async Task DoesNotForceRediscovery(GrpcCall call, StatusCode statusCode) {
			bool invoked = false;
			var sut = new ReportLeaderInterceptor(ep => invoked = true);

			var result = await Assert.ThrowsAsync<RpcException>(() => call(sut,
				Task.FromException<object>(new RpcException(new Status(statusCode, "oops")))));
			Assert.False(invoked);
		}
		

		private static async Task MakeUnaryCall(Interceptor interceptor, Task<object> response = null) {
			using var call = interceptor.AsyncUnaryCall(new object(),
				CreateClientInterceptorContext(MethodType.Unary),
				(_, context) => new AsyncUnaryCall<object>(response ?? Task.FromResult(new object()),
					Task.FromResult(context.Options.Headers), GetSuccess, GetTrailers, OnDispose));
			await call.ResponseAsync;
		}

		private static async Task MakeClientStreamingCall(Interceptor interceptor, Task<object> response = null) {
			using var call = interceptor.AsyncClientStreamingCall(
				CreateClientInterceptorContext(MethodType.ClientStreaming),
				context => new AsyncClientStreamingCall<object, object>(null, response ?? Task.FromResult(new object()),
					Task.FromResult(context.Options.Headers), GetSuccess, GetTrailers, OnDispose));
			await call.ResponseAsync;
		}

		private static async Task MakeServerStreamingCall(Interceptor interceptor, Task<object> response = null) {
			using var call = interceptor.AsyncServerStreamingCall(new object(),
				CreateClientInterceptorContext(MethodType.ServerStreaming),
				(_, context) => new AsyncServerStreamingCall<object>(new TestAsyncStreamReader(response),
					Task.FromResult(context.Options.Headers), GetSuccess, GetTrailers, OnDispose));
			await call.ResponseStream.ReadAllAsync().ToArrayAsync();
		}

		private static async Task MakeDuplexStreamingCall(Interceptor interceptor, Task<object> response = null) {
			using var call = interceptor.AsyncDuplexStreamingCall(
				CreateClientInterceptorContext(MethodType.ServerStreaming),
				context => new AsyncDuplexStreamingCall<object, object>(null, new TestAsyncStreamReader(response),
					Task.FromResult(context.Options.Headers), GetSuccess, GetTrailers, OnDispose));
			await call.ResponseStream.ReadAllAsync().ToArrayAsync();
		}

		private static Status GetSuccess() => Status.DefaultSuccess;

		private static Metadata GetTrailers() => Metadata.Empty;

		private static void OnDispose() { }

		private static ClientInterceptorContext<object, object> CreateClientInterceptorContext(MethodType methodType) =>
			new(new Method<object, object>(methodType, string.Empty, string.Empty, _marshaller, _marshaller),
				null, new CallOptions(new Metadata()));

		private class TestAsyncStreamReader : IAsyncStreamReader<object> {
			private readonly Task<object> _response;

			public Task<bool> MoveNext(CancellationToken cancellationToken) => _response.IsFaulted
				? Task.FromException<bool>(_response.Exception!.GetBaseException())
				: Task.FromResult(false);

			public object Current => _response.Result;

			public TestAsyncStreamReader(Task<object> response = null) {
				_response = response ?? Task.FromResult(new object());
			}
		}
	}
}
