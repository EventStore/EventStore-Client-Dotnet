using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client {
	public class ClusterAwareHttpHandlerTests {
		[Theory,
		 InlineData(true, true),
		 InlineData(true, false),
		 InlineData(false, true),
		 InlineData(false, false)
		]
		public async Task should_set_requires_leader_header(bool useHttps, bool requiresLeader) {
			var sut = new ClusterAwareHttpHandler(
				useHttps, requiresLeader,
				new FakeEndpointDiscoverer(() => new IPEndPoint(IPAddress.Parse("0.0.0.0"), 2113))) {
				InnerHandler = new TestMessageHandler()
			};

			var client = new HttpClient(sut);

			var request = new HttpRequestMessage(HttpMethod.Get, new UriBuilder().Uri);

			await client.SendAsync(request);

			Assert.True(request.Headers.TryGetValues("requires-leader", out var value));
			Assert.True(bool.Parse(value.First()) == requiresLeader);
		}

		[Theory, InlineData(true), InlineData(false)]
		public async Task should_issue_request_to_discovered_endpoint(bool useHttps) {
			var discoveredEndpoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 2113);

			var sut = new ClusterAwareHttpHandler(
				useHttps, true, new FakeEndpointDiscoverer(() => discoveredEndpoint)) {
				InnerHandler = new TestMessageHandler()
			};

			var client = new HttpClient(sut);

			await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, new UriBuilder().Uri));

			var request = new HttpRequestMessage(HttpMethod.Get, new UriBuilder().Uri);
			await client.SendAsync(request);

			Assert.Equal(discoveredEndpoint.Address.ToString(), request.RequestUri.Host);
			Assert.Equal(discoveredEndpoint.Port, request.RequestUri.Port);
		}

		[Theory, InlineData(true), InlineData(false)]
		public async Task should_attempt_endpoint_discovery_on_next_request_when_request_fails(bool useHttps) {
			int discoveryAttempts = 0;

			var sut = new ClusterAwareHttpHandler(
				useHttps, true, new FakeEndpointDiscoverer(() => {
					discoveryAttempts++;
					throw new Exception();
				})) {
				InnerHandler = new TestMessageHandler()
			};

			var client = new HttpClient(sut);

			await Assert.ThrowsAsync<Exception>(() =>
				client.SendAsync(new HttpRequestMessage(HttpMethod.Get, new UriBuilder().Uri)));
			await Assert.ThrowsAsync<Exception>(() =>
				client.SendAsync(new HttpRequestMessage(HttpMethod.Get, new UriBuilder().Uri)));

			Assert.Equal(2, discoveryAttempts);
		}

		[Theory, ClassData(typeof(EndPoints))]
		public async Task should_set_endpoint_to_leader_endpoint_on_exception(bool useHttps, EndPoint endpoint) {
			var sut = new ClusterAwareHttpHandler(
				useHttps, true, new FakeEndpointDiscoverer(() => new IPEndPoint(IPAddress.Loopback, 2113))) {
				InnerHandler = new TestNotLeaderMessageHandler(endpoint)
			};

			using var client = new HttpClient(sut);

			using var _ = await client.GetAsync(new UriBuilder().Uri); // first request results in leadernotfound

			using var request = new HttpRequestMessage(HttpMethod.Get, new UriBuilder().Uri);

			using var __ = await client.SendAsync(request);

			Assert.Equal(endpoint.GetHost(), request.RequestUri.Host);
			Assert.Equal(endpoint.GetPort(), request.RequestUri.Port);
		}
	}

	public class EndPoints : IEnumerable<object[]> {
		public IEnumerator<object[]> GetEnumerator() {
			yield return new object[] {true, new IPEndPoint(IPAddress.Any, 2113)};
			yield return new object[] {true, new DnsEndPoint("nodea.eventstore.dev", 2113),};
			yield return new object[] {false, new IPEndPoint(IPAddress.Any, 2113)};
			yield return new object[] {false, new DnsEndPoint("nodea.eventstore.dev", 2113),};
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	internal class FakeEndpointDiscoverer : IEndpointDiscoverer {
		private readonly Func<EndPoint> _function;

		public FakeEndpointDiscoverer(Func<EndPoint> function) {
			_function = function;
		}

		public Task<EndPoint> DiscoverAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult(_function());
	}

	internal class TestMessageHandler : HttpMessageHandler {
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
	}

	internal class TestNotLeaderMessageHandler : TestMessageHandler {
		private readonly EndPoint _endpoint;
		private int _messageCount = -1;

		public TestNotLeaderMessageHandler(EndPoint endpoint) {
			_endpoint = endpoint;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken) => Interlocked.Increment(ref _messageCount) == 0
			? Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
				TrailingHeaders = {
					{Constants.Exceptions.ExceptionKey, Constants.Exceptions.NotLeader},
					{Constants.Exceptions.LeaderEndpointHost, _endpoint.GetHost()},
					{Constants.Exceptions.LeaderEndpointPort, _endpoint.GetPort().ToString()}
				}
			})
			: base.SendAsync(request, cancellationToken);
	}
}
