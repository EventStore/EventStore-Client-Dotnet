using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using EventStore.Client.Gossip;
using Grpc.Core;
#if !GRPC_CORE
using Grpc.Net.Client;
#endif
using Polly;
using Xunit;
using EndPoint = System.Net.EndPoint;

#nullable enable
namespace EventStore.Client {
	public class Somerting {
		[Fact]
		public async Task YouSuck() {
			await using var fixture = new EventStoreClientInsecureClusterFixture();
			await fixture.Start();
			using var client = fixture.CreateEventStoreClient(
				Array.ConvertAll(fixture.Ports, port => new IPEndPoint(IPAddress.Loopback, port)));

			var leader = await fixture.GetLeaderNode();

			leader!.ToString();
		}
	}

	public class EventStoreClientInsecureClusterFixture : EventStoreClientClusterFixture {
		public EventStoreClientInsecureClusterFixture() : base(new UriBuilder {
			Scheme = Uri.UriSchemeHttp,
			Port = 2113,
			Query = "?tls=false"
		}.Uri, "insecure") {
		}
	}

	public abstract class EventStoreClientClusterFixture : IAsyncDisposable {
		private readonly Uri _address;
		private readonly EventStoreTestServer _server;
		public Gossip.Gossip.GossipClient GossipClient => _server.GossipClient;

		public int[] Ports => _server.Ports.ToArray();

		protected EventStoreClientClusterFixture(Uri address, string composeFileName) {
			_address = address;
			_server = new EventStoreTestServer(address, composeFileName);
		}

		public EventStoreClient CreateEventStoreClient(DnsEndPoint[] dnsGossipSeeds) {
			var settings = EventStoreClientSettings.Create(_address.ToString());
			settings.ConnectivitySettings.DnsGossipSeeds = dnsGossipSeeds;
			return new EventStoreClient(settings);
		}

		public EventStoreClient CreateEventStoreClient(IPEndPoint[] ipGossipSeeds) {
			var settings = EventStoreClientSettings.Create(new UriBuilder(_address) {
				Scheme = "esdb+discover"
			}.Uri.ToString());
			settings.ConnectivitySettings.IpGossipSeeds = ipGossipSeeds;
			return new EventStoreClient(settings);
		}

		public async ValueTask<EndPoint?> GetLeaderNode() {
			var clusterInfo = await GossipClient.ReadAsync(new Empty());
			return clusterInfo.Members.Where(x => x.State == MemberInfo.Types.VNodeState.Leader)
				.Select(x => new DnsEndPoint(x.HttpEndPoint.Address, (int)x.HttpEndPoint.Port))
				.FirstOrDefault();
		}

		public ValueTask Start() => _server.Start();
		public ValueTask DisposeAsync() => _server.DisposeAsync();

		private class EventStoreTestServer : IAsyncDisposable {
			private readonly ICompositeService _eventStore;
			private readonly HttpClient _httpClient;
			private readonly ChannelBase _channel;
			public Gossip.Gossip.GossipClient GossipClient { get; }

			public int[] Ports =>
				(from container in _eventStore.Containers
					where container.Name.Contains("esdb-node")
					from pair in container.GetConfiguration().NetworkSettings.Ports
					where pair.Key == "2113/tcp"
					select pair.Value.First().Port)
				.ToArray();

			public EventStoreTestServer(Uri address, string mode) {
				_httpClient = new HttpClient(new SocketsHttpHandler {
					SslOptions = {
						RemoteCertificateValidationCallback = delegate { return true; }
					}
				}) {
					BaseAddress = address
				};

				_eventStore = new Builder()
					.UseContainer()
					.UseCompose()
					.FromFile($"docker-compose.{mode}.yml")
					.Build();
				_channel = CreateChannel(address);
				GossipClient = new Gossip.Gossip.GossipClient(_channel);
			}

			private ChannelBase CreateChannel(Uri address) {
#if !GRPC_CORE
				return GrpcChannel.ForAddress(address);
#else
				return new Channel(address.Host, address.Port,
					address.Scheme == Uri.UriSchemeHttps
						? new SslCredentials()
						: ChannelCredentials.Insecure);
#endif
			}

			public ValueTask DisposeAsync() {
				if (_channel is IDisposable disposable) {
					disposable.Dispose();
				}

				try {
					_eventStore.Dispose();
				}
				catch {}

				return new ValueTask();
			}

			public async ValueTask Start(CancellationToken cancellationToken = default) {
				_eventStore.Start();
				try {
					await Policy.Handle<Exception>()
						.WaitAndRetryAsync(5, retryCount => TimeSpan.FromSeconds(retryCount * retryCount))
						.ExecuteAsync(async () => {
							using var response = await _httpClient.GetAsync("/health/live", cancellationToken);
							if (response.StatusCode >= HttpStatusCode.BadRequest) {
								throw new Exception($"Health check failed with status code: {response.StatusCode}.");
							}
						});
				} catch (Exception) {
					_httpClient.Dispose();
					_eventStore.Dispose();
					throw;
				}
			}
		}
	}
}
