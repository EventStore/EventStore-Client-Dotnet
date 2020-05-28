using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Gossip;
using Grpc.Core;
using Xunit;

#nullable enable
namespace EventStore.Client {
	public class GossipBasedEndpointDiscovererTests: IAsyncLifetime {
		private readonly Fixture _fixture;

		public GossipBasedEndpointDiscovererTests() {
			_fixture = new Fixture();
		}

		[Fact]
		public async Task should_issue_gossip_to_gossip_seed() {
			HttpRequestMessage? request = null;
			var gossip = new ClusterMessages.ClusterInfo {
				Members = new[] {
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Leader,
						InstanceId = Guid.NewGuid(),
						HttpEndPointIp = IPAddress.Any.ToString(),
						HttpEndPointPort = 4444,
						IsAlive = true,
					},
				}
			};

			var handler = new CustomMessageHandler(req => {
				request = req;
				_fixture.CurrentClusterInfo.Members = gossip.Members;
			});

			var gossipSeed = new DnsEndPoint(_fixture.Host, _fixture.Port);

			var sut = new ClusterEndpointDiscoverer(1, new[] {
				gossipSeed,
			}, Timeout.InfiniteTimeSpan, TimeSpan.Zero, NodePreference.Leader, handler);

			await sut.DiscoverAsync();

			Assert.Equal(Uri.UriSchemeHttps, request?.RequestUri.Scheme);
			Assert.Equal(gossipSeed.Host, request?.RequestUri.Host);
			Assert.Equal(gossipSeed.Port, request?.RequestUri.Port);
		}
		
		[Fact]
		public async Task should_be_able_to_discover_twice() {
			bool isFirstGossip = true;
			var firstGossip = new ClusterMessages.ClusterInfo {
				Members = new[] {
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Leader,
						InstanceId = Guid.NewGuid(),
						HttpEndPointIp = IPAddress.Any.ToString(),
						HttpEndPointPort = 1111,
						IsAlive = true,
					},
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Follower,
						InstanceId = Guid.NewGuid(),
						HttpEndPointIp = IPAddress.Any.ToString(),
						HttpEndPointPort = 2222,
						IsAlive = true,
					},
				}
			};
			var secondGossip = new ClusterMessages.ClusterInfo {
				Members = new[] {
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Leader,
						InstanceId = Guid.NewGuid(),
						HttpEndPointIp = IPAddress.Any.ToString(),
						HttpEndPointPort = 1111,
						IsAlive = false,
					},
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Leader,
						InstanceId = Guid.NewGuid(),
						HttpEndPointIp = IPAddress.Any.ToString(),
						HttpEndPointPort = 2222,
						IsAlive = true,
					},
				}
			};

			var handler = new CustomMessageHandler(req => {
				if (isFirstGossip) {
					isFirstGossip = false;
					_fixture.CurrentClusterInfo.Members = firstGossip.Members;
				} else {
					_fixture.CurrentClusterInfo.Members = secondGossip.Members;
				}
			});

			var gossipSeed = new DnsEndPoint(_fixture.Host, _fixture.Port);

			var sut = new ClusterEndpointDiscoverer(5, new[] {
				gossipSeed,
			}, Timeout.InfiniteTimeSpan, TimeSpan.Zero, NodePreference.Leader, handler);

			var result = await sut.DiscoverAsync();

			var expected = firstGossip.Members.First(x => x.HttpEndPointPort == 1111);

			Assert.Equal(expected.HttpEndPointIp, result.GetHost());
			Assert.Equal(expected.HttpEndPointPort, result.GetPort());
			
			result = await sut.DiscoverAsync();

			expected = secondGossip.Members.First(x => x.HttpEndPointPort == 2222);

			Assert.Equal(expected.HttpEndPointIp, result.GetHost());
			Assert.Equal(expected.HttpEndPointPort, result.GetPort());
		}

		[Fact]
		public async Task should_not_exceed_max_discovery_attempts() {
			int maxDiscoveryAttempts = 5;
			int discoveryAttempts = 0;

			var handler = new CustomMessageHandler(request => {
				discoveryAttempts++;
				throw new Exception();
			});

			var sut = new ClusterEndpointDiscoverer(maxDiscoveryAttempts, new[] {
				new DnsEndPoint(_fixture.Host, _fixture.Port),
			}, Timeout.InfiniteTimeSpan, TimeSpan.Zero, NodePreference.Leader, handler);

			await Assert.ThrowsAsync<DiscoveryException>(() => sut.DiscoverAsync());

			Assert.Equal(maxDiscoveryAttempts, discoveryAttempts);
		}

		[Theory,
		 InlineData(ClusterMessages.VNodeState.Manager),
		 InlineData(ClusterMessages.VNodeState.Shutdown),
		 InlineData(ClusterMessages.VNodeState.Unknown),
		 InlineData(ClusterMessages.VNodeState.Initializing),
		 InlineData(ClusterMessages.VNodeState.CatchingUp),
		 InlineData(ClusterMessages.VNodeState.ResigningLeader),
		 InlineData(ClusterMessages.VNodeState.ShuttingDown),
		 InlineData(ClusterMessages.VNodeState.PreLeader),
		 InlineData(ClusterMessages.VNodeState.PreReplica),
		 InlineData(ClusterMessages.VNodeState.PreReadOnlyReplica)]
		public async Task should_not_be_able_to_pick_invalid_node(ClusterMessages.VNodeState invalidState) {
			var gossip = new ClusterMessages.ClusterInfo {
				Members = new[] {
					new ClusterMessages.MemberInfo {
						State = invalidState,
						InstanceId = Guid.NewGuid(),
						HttpEndPointIp = IPAddress.Any.ToString(),
						HttpEndPointPort = 4444,
						IsAlive = true,
					},
				}
			};

			var handler = new CustomMessageHandler(req => {
				_fixture.CurrentClusterInfo.Members = gossip.Members;
			});

			var sut = new ClusterEndpointDiscoverer(1, new[] { new DnsEndPoint(_fixture.Host, _fixture.Port),
			}, Timeout.InfiniteTimeSpan, TimeSpan.Zero, NodePreference.Leader, handler);

			await Assert.ThrowsAsync<DiscoveryException>(() => sut.DiscoverAsync());
		}

		[Theory,
		 InlineData(NodePreference.Leader, ClusterMessages.VNodeState.Leader),
		 InlineData(NodePreference.Follower, ClusterMessages.VNodeState.Follower),
		 InlineData(NodePreference.ReadOnlyReplica, ClusterMessages.VNodeState.ReadOnlyReplica),
		 InlineData(NodePreference.ReadOnlyReplica, ClusterMessages.VNodeState.ReadOnlyLeaderless)]
		public async Task should_pick_node_based_on_preference(NodePreference preference,
			ClusterMessages.VNodeState expectedState) {
			var gossip = new ClusterMessages.ClusterInfo {
				Members = new[] {
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Leader,
						InstanceId = Guid.NewGuid(),
						HttpEndPointIp = IPAddress.Any.ToString(),
						HttpEndPointPort = 1111,
						IsAlive = true,
					},
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Follower,
						InstanceId = Guid.NewGuid(),
						HttpEndPointIp = IPAddress.Any.ToString(),
						HttpEndPointPort = 2222,
						IsAlive = true,
					},
					new ClusterMessages.MemberInfo {
						State = expectedState == ClusterMessages.VNodeState.ReadOnlyLeaderless
							? expectedState
							: ClusterMessages.VNodeState.ReadOnlyReplica,
						InstanceId = Guid.NewGuid(),
						HttpEndPointIp = IPAddress.Any.ToString(),
						HttpEndPointPort = 3333,
						IsAlive = true,
					},
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Manager,
						InstanceId = Guid.NewGuid(),
						HttpEndPointIp = IPAddress.Any.ToString(),
						HttpEndPointPort = 4444,
						IsAlive = true,
					},
				}
			};
			var handler = new CustomMessageHandler(req => {
				_fixture.CurrentClusterInfo.Members = gossip.Members;
			});

			var sut = new ClusterEndpointDiscoverer(1, new[] {
				new DnsEndPoint(_fixture.Host, _fixture.Port)
			}, Timeout.InfiniteTimeSpan, TimeSpan.Zero, preference, handler);

			var result = await sut.DiscoverAsync();
			Assert.Equal(result.GetPort(),
				gossip.Members.Last(x => x.State == expectedState).HttpEndPointPort);
		}

		[Fact]
		public async Task falls_back_to_first_alive_node_if_a_preferred_node_is_not_found() {
			var gossip = new ClusterMessages.ClusterInfo {
				Members = new[] {
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Leader,
						InstanceId = Guid.NewGuid(),
						HttpEndPointIp = IPAddress.Any.ToString(),
						HttpEndPointPort = 1111,
						IsAlive = false,
					},
					new ClusterMessages.MemberInfo {
						State = ClusterMessages.VNodeState.Follower,
						InstanceId = Guid.NewGuid(),
						HttpEndPointIp = IPAddress.Any.ToString(),
						HttpEndPointPort = 2222,
						IsAlive = true,
					},
				}
			};
			var handler = new CustomMessageHandler(req => {
				_fixture.CurrentClusterInfo.Members = gossip.Members;
			});

			var sut = new ClusterEndpointDiscoverer(1, new[] {
				new DnsEndPoint(_fixture.Host, _fixture.Port)
			}, Timeout.InfiniteTimeSpan, TimeSpan.Zero, NodePreference.Leader, handler);

			var result = await sut.DiscoverAsync();
			Assert.Equal(result.GetPort(),
				gossip.Members.Last(x => x.State == ClusterMessages.VNodeState.Follower).HttpEndPointPort);
		}

		private class CustomMessageHandler : HttpClientHandler {
			private readonly Action<HttpRequestMessage> _handle;

			public CustomMessageHandler(Action<HttpRequestMessage> handle) {
				_handle = handle;
				ServerCertificateCustomValidationCallback = delegate { return true; };
			}

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
				CancellationToken cancellationToken) {
				_handle(request);
				return base.SendAsync(request, cancellationToken);
			}
		}

		public Task InitializeAsync() => _fixture.InitializeAsync();
		public Task DisposeAsync() => _fixture.DisposeAsync();

		public class Fixture : IAsyncLifetime {
			public readonly string Host = "localhost";
			public readonly int Port = GetFreePort();
			public readonly ClusterMessages.ClusterInfo CurrentClusterInfo = new ClusterMessages.ClusterInfo();
			private Server? _server;

			private static int GetFreePort() {
				using var socket =
					new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) {
						ExclusiveAddressUse = false
					};
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
				socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
				return ((IPEndPoint)socket.LocalEndPoint).Port;
			}

			private void StartGrpcServer() {
				var keyCertificatePair = GenerateKeyCertificatePair();
				_server = new Server
				{
					Services = { Gossip.Gossip.BindService(new GossipImplementation(CurrentClusterInfo)) },
					Ports = { new ServerPort(Host, Port, new SslServerCredentials(new [] {keyCertificatePair})) }
				};
				_server.Start();
			}

			private KeyCertificatePair GenerateKeyCertificatePair() {
				using (RSA rsa = RSA.Create())
				{
					var certReq = new CertificateRequest("CN=hello", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
					var certificate = certReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddMonths(-1), DateTimeOffset.UtcNow.AddMonths(1));
					var pemCertificateBuilder = new StringBuilder();
					pemCertificateBuilder.AppendLine("-----BEGIN CERTIFICATE-----");
					pemCertificateBuilder.AppendLine(Convert.ToBase64String(certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
					pemCertificateBuilder.AppendLine("-----END CERTIFICATE-----");
					var pemCertificate = pemCertificateBuilder.ToString();

					var pemKeyBuilder = new StringBuilder();
					pemKeyBuilder.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
					pemKeyBuilder.AppendLine(Convert.ToBase64String(rsa.ExportRSAPrivateKey(), Base64FormattingOptions.InsertLineBreaks));
					pemKeyBuilder.AppendLine("-----END RSA PRIVATE KEY-----");
					var pemKey = pemKeyBuilder.ToString();

					return new KeyCertificatePair(pemCertificate, pemKey);
				}
			}

			private class GossipImplementation : Gossip.Gossip.GossipBase {
				private readonly ClusterMessages.ClusterInfo _currentClusterInfo;

				public GossipImplementation(ClusterMessages.ClusterInfo currentClusterInfo) {
					_currentClusterInfo = currentClusterInfo;
				}
				public override Task<ClusterInfo> Read(Empty request, ServerCallContext context) {
					if (_currentClusterInfo.Members == null) {
						return Task.FromResult(new ClusterInfo());
					}
					var members = Array.ConvertAll(_currentClusterInfo.Members, x => new MemberInfo {
						InstanceId = Uuid.FromGuid(x.InstanceId).ToDto(),
						State = (MemberInfo.Types.VNodeState)x.State,
						IsAlive = x.IsAlive,
						HttpEndPoint = new Gossip.EndPoint {
							Address = x.HttpEndPointIp,
							Port = (uint) x.HttpEndPointPort
						}
					}).ToArray();
					var info = new ClusterInfo();
					info.Members.AddRange(members);
					return Task.FromResult(info);
				}
			}

			public Task InitializeAsync() {
				StartGrpcServer();
				return Task.CompletedTask;
			}

			public Task DisposeAsync() {
				return _server == null ? Task.CompletedTask : _server.ShutdownAsync();
			}
		}
	}
}
