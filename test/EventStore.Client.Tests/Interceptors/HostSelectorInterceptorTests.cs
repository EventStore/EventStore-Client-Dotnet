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
	public class HostSelectorInterceptorTests {
		private static readonly Marshaller<object> _marshaller =
			new Marshaller<object>(_ => Array.Empty<byte>(), _ => new object());

		public delegate Task<(Metadata metadata, string host)> GrpcCall(Interceptor interceptor,
			Task<object> response = null);

		private static IEnumerable<GrpcCall> Calls() {
			yield return MakeUnaryCall;
			yield return MakeClientStreamingCall;
			yield return MakeServerStreamingCall;
			yield return MakeDuplexStreamingCall;
		}

		private static IEnumerable<(NodePreference nodePreference, bool requriesLeader)> NodePreferences() {
			yield return (NodePreference.Leader, true);
			yield return (NodePreference.Follower, false);
			yield return (NodePreference.Random, false);
			yield return (NodePreference.ReadOnlyReplica, false);
		}

		public static IEnumerable<object[]> RequiresLeaderCases() =>
			from _ in NodePreferences()
			from call in Calls()
			select new object[] {_.nodePreference, _.requriesLeader, call};

		[Theory, MemberData(nameof(RequiresLeaderCases))]
		public async Task RequiresLeaderSetToExpectedResult(NodePreference nodePreference, bool expectedRequiresLeader,
			GrpcCall makeCall) {
			var sut = new HostSelectorInterceptor(new TestEndpointDiscoverer(new IPEndPoint(IPAddress.Any, 2113)),
				nodePreference);
			var (metadata, host) = await makeCall(sut);
			Assert.True(metadata.TryGetValue("requires-leader", out var requiresLeaderValue));
			Assert.True(bool.TryParse(requiresLeaderValue, out var requiresLeader));
			Assert.Equal(expectedRequiresLeader, requiresLeader);
			Assert.Equal($"{IPAddress.Any}:2113", host);
		}

		public static IEnumerable<object[]> SelectsGossipCases() =>
			from call in Calls()
			select new object[] {call};

		[Theory, MemberData(nameof(SelectsGossipCases))]
		public async Task SelectsNewGossipEndPointAfterFailure(GrpcCall makeCall) {
			var endpoints = new EndPoint[] {
				new IPEndPoint(IPAddress.Loopback, 2113),
				new IPEndPoint(IPAddress.Loopback, 2114),
				new IPEndPoint(IPAddress.Loopback, 2115),
			};

			var sut = new HostSelectorInterceptor(new TestEndpointDiscoverer(endpoints), NodePreference.Leader);

			await Assert.ThrowsAsync<DummyException>(() =>
				makeCall(sut, Task.FromException<object>(new DummyException())));

			var (_, host) = await makeCall(sut, Task.FromResult(new object()));

			Assert.Equal($"{IPAddress.Loopback}:2114", host);
		}

		private static async Task<(Metadata metadata, string host)> MakeUnaryCall(Interceptor interceptor,
			Task<object> response = null) {
			var metadata = new Metadata();
			string host = null;

			using var call = interceptor.AsyncUnaryCall(new object(),
				CreateClientInterceptorContext(metadata, MethodType.Unary),
				(_, context) => {
					host = context.Host;
					return new AsyncUnaryCall<object>(response ?? Task.FromResult(new object()),
						Task.FromResult(context.Options.Headers), GetSuccess, GetTrailers, OnDispose);
				});
			await call.ResponseAsync;
			return (metadata, host);
		}

		private static async Task<(Metadata metadata, string host)> MakeClientStreamingCall(Interceptor interceptor,
			Task<object> response = null) {
			var metadata = new Metadata();
			string host = null;

			using var call = interceptor.AsyncClientStreamingCall(
				CreateClientInterceptorContext(metadata, MethodType.ClientStreaming),
				context => {
					host = context.Host;
					return new AsyncClientStreamingCall<object, object>(null, response ?? Task.FromResult(new object()),
						Task.FromResult(context.Options.Headers), GetSuccess, GetTrailers, OnDispose);
				});
			await call.ResponseAsync;
			return (metadata, host);
		}

		private static async Task<(Metadata metadata, string host)> MakeServerStreamingCall(Interceptor interceptor,
			Task<object> response = null) {
			var metadata = new Metadata();
			string host = null;

			using var call = interceptor.AsyncServerStreamingCall(new object(),
				CreateClientInterceptorContext(metadata, MethodType.ServerStreaming),
				(_, context) => {
					host = context.Host;
					return new AsyncServerStreamingCall<object>(new TestAsyncStreamReader(response),
						Task.FromResult(context.Options.Headers), GetSuccess, GetTrailers, OnDispose);
				});
			await call.ResponseStream.ReadAllAsync().ToArrayAsync();
			return (metadata, host);
		}

		private static async Task<(Metadata metadata, string host)> MakeDuplexStreamingCall(Interceptor interceptor,
			Task<object> response = null) {
			var metadata = new Metadata();
			string host = null;

			using var call = interceptor.AsyncDuplexStreamingCall(
				CreateClientInterceptorContext(metadata, MethodType.ServerStreaming),
				context => {
					host = context.Host;
					return new AsyncDuplexStreamingCall<object, object>(null, new TestAsyncStreamReader(response),
						Task.FromResult(context.Options.Headers), GetSuccess, GetTrailers, OnDispose);
				});
			await call.ResponseStream.ReadAllAsync().ToArrayAsync();
			return (metadata, host);
		}

		private static Status GetSuccess() => Status.DefaultSuccess;

		private static Metadata GetTrailers() => Metadata.Empty;

		private static void OnDispose() { }

		private static ClientInterceptorContext<object, object> CreateClientInterceptorContext(Metadata metadata,
			MethodType methodType) => new ClientInterceptorContext<object, object>(
			new Method<object, object>(methodType, string.Empty, string.Empty, _marshaller, _marshaller),
			null, new CallOptions(metadata));


		private class TestEndpointDiscoverer : IEndpointDiscoverer {
			private readonly EndPoint[] _endPoints;
			private int _index = -1;

			public TestEndpointDiscoverer(params EndPoint[] endPoints) {
				_endPoints = endPoints;
			}

			public Task<EndPoint> DiscoverAsync(CancellationToken cancellationToken = default) =>
				Task.FromResult(_endPoints[Interlocked.Increment(ref _index) % _endPoints.Length]);
		}

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

		private class DummyException : Exception {
		}
	}
}
