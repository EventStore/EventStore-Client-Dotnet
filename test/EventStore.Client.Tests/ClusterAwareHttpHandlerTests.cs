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
		[Theory, InlineData(true), InlineData(false)]
		public async Task should_set_requires_leader_header(bool requiresLeader) {
			var sut = new ClusterAwareHttpHandler(
				requiresLeader, new FakeEndpointDiscoverer(() => new IPEndPoint(IPAddress.Parse("0.0.0.0"), 2113))) {
				InnerHandler = new TestMessageHandler()
			};

			var client = new HttpClient(sut);

			var request = new HttpRequestMessage(HttpMethod.Get, new UriBuilder().Uri);

			await client.SendAsync(request);

			Assert.True(request.Headers.TryGetValues("requires-leader", out var value));
			Assert.True(bool.Parse(value.First()) == requiresLeader);
		}

		[Fact]
		public async Task should_issue_request_to_discovered_endpoint() {
			var discoveredEndpoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 2113);

			var sut = new ClusterAwareHttpHandler(
				true, new FakeEndpointDiscoverer(() => discoveredEndpoint)) {
				InnerHandler = new TestMessageHandler()
			};

			var client = new HttpClient(sut);

			await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, new UriBuilder().Uri));

			var request = new HttpRequestMessage(HttpMethod.Get, new UriBuilder().Uri);
			await client.SendAsync(request);

			Assert.Equal(discoveredEndpoint.Address.ToString(), request.RequestUri.Host);
			Assert.Equal(discoveredEndpoint.Port, request.RequestUri.Port);
		}

		[Fact]
		public async Task should_attempt_endpoint_discovery_on_next_request_when_request_fails() {
			int discoveryAttempts = 0;

			var sut = new ClusterAwareHttpHandler(
				true, new FakeEndpointDiscoverer(() => {
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
		public async Task should_set_endpoint_to_leader_endpoint_on_exception(EndPoint endpoint) {
			var sut = new ClusterAwareHttpHandler(
				true, new FakeEndpointDiscoverer(() => new IPEndPoint(IPAddress.Parse("0.0.0.0"), 2113))) {
				InnerHandler = new TestMessageHandler()
			};

			var client = new HttpClient(sut);

			var request = new HttpRequestMessage(HttpMethod.Get, new UriBuilder().Uri);

			sut.ExceptionOccurred(new NotLeaderException(endpoint.GetHost(), endpoint.GetPort()));
			await client.SendAsync(request);
			
			Assert.Equal(endpoint.GetHost(), request.RequestUri.Host);
			Assert.Equal(endpoint.GetPort(), request.RequestUri.Port);
		}
	}
	
	public class EndPoints : IEnumerable<object[]> {
		public IEnumerator<object[]> GetEnumerator() {
			yield return new object[] {new IPEndPoint(IPAddress.Parse("0.0.0.0"), 2113)};
			yield return new object[] {new DnsEndPoint("nodea.eventstore.dev", 2113), };
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	internal class FakeEndpointDiscoverer : IEndpointDiscoverer {
		private readonly Func<EndPoint> _function;

		public FakeEndpointDiscoverer(Func<EndPoint> function) {
			_function = function;
		}

		public Task<EndPoint> DiscoverAsync() {
			return Task.FromResult(_function());
		}
	}

	internal class TestMessageHandler : HttpMessageHandler {
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken) {
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
		}
	}
}
