using EventStore.Client.Interceptors;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace EventStore.Client.Tests.Interceptors;

public class ReportLeaderInterceptorTests {
	public delegate Task GrpcCall(Interceptor interceptor, Task<object>? response = null);

	static readonly StatusCode[] ForcesRediscoveryStatusCodes = {
		//StatusCode.Unknown, TODO: use RPC exceptions on server
		StatusCode.Unavailable
	};

	static readonly Marshaller<object> Marshaller = new(_ => Array.Empty<byte>(), _ => new());

	static IEnumerable<GrpcCall> GrpcCalls() {
		yield return MakeUnaryCall;
		yield return MakeClientStreamingCall;
		yield return MakeDuplexStreamingCall;
		yield return MakeServerStreamingCall;
		yield return MakeClientStreamingCallForWriting;
		yield return MakeDuplexStreamingCallForWriting;
	}

	public static IEnumerable<object?[]> ReportsNewLeaderCases() => GrpcCalls().Select(call => new object[] { call });

	[Theory]
	[MemberData(nameof(ReportsNewLeaderCases))]
	public async Task ReportsNewLeader(GrpcCall call) {
		GrpcChannelInput? actual = default;

		var sut = new ReportLeaderInterceptor(result => actual = result);

		var result = await Assert.ThrowsAsync<NotLeaderException>(() => call(sut, Task.FromException<object>(new NotLeaderException("a.host", 2112))));

		Assert.Equal(new ReconnectionRequired.NewLeader(result.LeaderEndpoint), actual?.ReconnectionRequired);
	}

	public static IEnumerable<object?[]> ForcesRediscoveryCases() =>
		from call in GrpcCalls()
		from statusCode in ForcesRediscoveryStatusCodes
		select new object[] { call, statusCode };

	[Theory]
	[MemberData(nameof(ForcesRediscoveryCases))]
	public async Task ForcesRediscovery(GrpcCall call, StatusCode statusCode) {
		GrpcChannelInput? actual = default;

		var sut = new ReportLeaderInterceptor(result => actual = result);

		await Assert.ThrowsAsync<RpcException>(() => call(sut, Task.FromException<object>(new RpcException(new(statusCode, "oops")))));

		Assert.NotNull(actual);
	}

	public static IEnumerable<object?[]> DoesNotForceRediscoveryCases() =>
		from call in GrpcCalls()
		from statusCode in Enum.GetValues(typeof(StatusCode))
			.OfType<StatusCode>()
			.Except(ForcesRediscoveryStatusCodes)
		select new object[] { call, statusCode };

	[Theory]
	[MemberData(nameof(DoesNotForceRediscoveryCases))]
	public async Task DoesNotForceRediscovery(GrpcCall call, StatusCode statusCode) {
		GrpcChannelInput? actual = default;

		var sut = new ReportLeaderInterceptor(result => actual = result);

		await Assert.ThrowsAsync<RpcException>(() => call(sut, Task.FromException<object>(new RpcException(new(statusCode, "oops")))));

		Assert.Null(actual);
	}

	static async Task MakeUnaryCall(Interceptor interceptor, Task<object>? response = null) {
		using var call = interceptor.AsyncUnaryCall(
			new(),
			CreateClientInterceptorContext(MethodType.Unary),
			(_, context) => new(
				response ?? Task.FromResult(new object()),
				Task.FromResult(context.Options.Headers!),
				GetSuccess,
				GetTrailers,
				OnDispose
			)
		);

		await call.ResponseAsync;
	}

	static async Task MakeClientStreamingCall(Interceptor interceptor, Task<object>? response = null) {
		using var call = interceptor.AsyncClientStreamingCall(
			CreateClientInterceptorContext(MethodType.ClientStreaming),
			context => new(
				null!,
				response ?? Task.FromResult(new object()),
				Task.FromResult(context.Options.Headers!),
				GetSuccess,
				GetTrailers,
				OnDispose
			)
		);

		await call.ResponseAsync;
	}

	static async Task MakeServerStreamingCall(Interceptor interceptor, Task<object>? response = null) {
		using var call = interceptor.AsyncServerStreamingCall(
			new(),
			CreateClientInterceptorContext(MethodType.ServerStreaming),
			(_, context) => new(
				new TestAsyncStreamReader(response),
				Task.FromResult(context.Options.Headers!),
				GetSuccess,
				GetTrailers,
				OnDispose
			)
		);

		await call.ResponseStream.ReadAllAsync().ToArrayAsync();
	}

	static async Task MakeDuplexStreamingCall(Interceptor interceptor, Task<object>? response = null) {
		using var call = interceptor.AsyncDuplexStreamingCall(
			CreateClientInterceptorContext(MethodType.ServerStreaming),
			context => new(
				null!,
				new TestAsyncStreamReader(response),
				Task.FromResult(context.Options.Headers!),
				GetSuccess,
				GetTrailers,
				OnDispose
			)
		);

		await call.ResponseStream.ReadAllAsync().ToArrayAsync();
	}

	// we might write to the server before listening to its response. if that write fails because
	// the server is down then we will never listen to its response, so the failed write should
	// trigger rediscovery itself
	static async Task MakeClientStreamingCallForWriting(Interceptor interceptor, Task<object>? response = null) {
		using var call = interceptor.AsyncClientStreamingCall(
			CreateClientInterceptorContext(MethodType.ClientStreaming),
			context => new(
				new TestAsyncStreamWriter(response),
				Task.FromResult(new object()),
				Task.FromResult(context.Options.Headers!),
				GetSuccess,
				GetTrailers,
				OnDispose
			)
		);

		await call.RequestStream.WriteAsync(new());
	}

	static async Task MakeDuplexStreamingCallForWriting(Interceptor interceptor, Task<object>? response = null) {
		using var call = interceptor.AsyncDuplexStreamingCall(
			CreateClientInterceptorContext(MethodType.ServerStreaming),
			_ => new(
				new TestAsyncStreamWriter(response),
				null!,
				null!,
				GetSuccess,
				GetTrailers,
				OnDispose
			)
		);

		await call.RequestStream.WriteAsync(new());
	}

	static Status GetSuccess() => Status.DefaultSuccess;

	static Metadata GetTrailers() => Metadata.Empty;

	static void OnDispose() { }

	static ClientInterceptorContext<object, object> CreateClientInterceptorContext(MethodType methodType) =>
		new(
			new(
				methodType,
				string.Empty,
				string.Empty,
				Marshaller,
				Marshaller
			),
			null,
			new(new())
		);

	class TestAsyncStreamReader : IAsyncStreamReader<object> {
		readonly Task<object> _response;

		public TestAsyncStreamReader(Task<object>? response = null) => _response = response ?? Task.FromResult(new object());

		public Task<bool> MoveNext(CancellationToken cancellationToken) =>
			_response.IsFaulted
				? Task.FromException<bool>(_response.Exception!.GetBaseException())
				: Task.FromResult(false);

		public object Current => _response.Result;
	}

	class TestAsyncStreamWriter : IClientStreamWriter<object> {
		readonly Task<object> _response;

		public TestAsyncStreamWriter(Task<object>? response = null) => _response = response ?? Task.FromResult(new object());

		public WriteOptions? WriteOptions {
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public Task CompleteAsync() => throw new NotImplementedException();

		public Task WriteAsync(object message) =>
			_response.IsFaulted
				? Task.FromException<bool>(_response.Exception!.GetBaseException())
				: Task.FromResult(false);
	}
}
