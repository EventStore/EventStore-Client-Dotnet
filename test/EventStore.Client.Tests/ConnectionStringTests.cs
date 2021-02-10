using System;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace EventStore.Client {
	public class ConnectionStringTests {
		[Fact]
		public void connection_string_with_no_schema() {
			Assert.Throws<NoSchemeException>(() => EventStoreClientSettings.Create(":so/mething/random"));
		}

		[Theory,
		 InlineData("esdbwrong://"),
		 InlineData("wrong://"),
		 InlineData("badesdb://")]
		public void connection_string_with_invalid_scheme_should_throw(string connectionString) {
			Assert.Throws<InvalidSchemeException>(() => EventStoreClientSettings.Create(connectionString));
		}

		[Theory,
		 InlineData("esdb://userpass@127.0.0.1/"),
		 InlineData("esdb://user:pa:ss@127.0.0.1/"),
		 InlineData("esdb://us:er:pa:ss@127.0.0.1/")]
		public void connection_string_with_invalid_userinfo_should_throw(string connectionString) {
			Assert.Throws<InvalidUserCredentialsException>(() => EventStoreClientSettings.Create(connectionString));
		}

		[Theory,
		 InlineData("esdb://user:pass@127.0.0.1:abc"),
		 InlineData("esdb://user:pass@127.0.0.1:abc/"),
		 InlineData("esdb://user:pass@127.0.0.1:1234,127.0.0.2:abc,127.0.0.3:4321"),
		 InlineData("esdb://user:pass@127.0.0.1:1234,127.0.0.2:abc,127.0.0.3:4321/"),
		 InlineData("esdb://user:pass@127.0.0.1:abc:def"),
		 InlineData("esdb://user:pass@127.0.0.1:abc:def/"),
		 InlineData("esdb://user:pass@localhost:1234,127.0.0.2:abc:def,127.0.0.3:4321"),
		 InlineData("esdb://user:pass@localhost:1234,127.0.0.2:abc:def,127.0.0.3:4321/"),
		 InlineData("esdb://user:pass@localhost:1234,,127.0.0.3:4321"),
		 InlineData("esdb://user:pass@localhost:1234,,127.0.0.3:4321/")]
		public void connection_string_with_invalid_host_should_throw(string connectionString) {
			Assert.Throws<InvalidHostException>(() => EventStoreClientSettings.Create(connectionString));
		}

		[Theory,
		 InlineData("esdb://user:pass@127.0.0.1"),
		 InlineData("esdb://user:pass@127.0.0.1:1234"),
		 InlineData("esdb://user:pass@127.0.0.1/"),
		 InlineData("esdb://user:pass@127.0.0.1?maxDiscoverAttempts=10"),
		 InlineData("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=10")]
		public void connection_string_with_empty_path_after_host_should_not_throw(string connectionString) {
			EventStoreClientSettings.Create(connectionString);
		}

		[Theory,
		 InlineData("esdb://user:pass@127.0.0.1/test"),
		 InlineData("esdb://user:pass@127.0.0.1/maxDiscoverAttempts=10"),
		 InlineData("esdb://user:pass@127.0.0.1/hello?maxDiscoverAttempts=10")]
		public void connection_string_with_non_empty_path_should_throw(string connectionString) {
			Assert.Throws<ConnectionStringParseException>(() => EventStoreClientSettings.Create(connectionString));
		}

		[Theory,
		 InlineData("esdb://user:pass@127.0.0.1"),
		 InlineData("esdb://user:pass@127.0.0.1/"),
		 InlineData("esdb+discover://user:pass@127.0.0.1"),
		 InlineData("esdb+discover://user:pass@127.0.0.1/")]
		public void connection_string_with_no_key_value_pairs_specified_should_not_throw(string connectionString) {
			EventStoreClientSettings.Create(connectionString);
		}

		[Theory,
		 InlineData("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=12=34"),
		 InlineData("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts1234")]
		public void connection_string_with_invalid_key_value_pair_should_throw(string connectionString) {
			Assert.Throws<InvalidKeyValuePairException>(() => EventStoreClientSettings.Create(connectionString));
		}

		[Theory,
		 InlineData("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=1234&MaxDiscoverAttempts=10"),
		 InlineData("esdb://user:pass@127.0.0.1/?gossipTimeout=10&gossipTimeout=30")]
		public void connection_string_with_duplicate_key_should_throw(string connectionString) {
			Assert.Throws<DuplicateKeyException>(() => EventStoreClientSettings.Create(connectionString));
		}

		[Theory,
		 InlineData("esdb://user:pass@127.0.0.1/?unknown=1234"),
		 InlineData("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=1234&hello=test"),
		 InlineData("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=abcd"),
		 InlineData("esdb://user:pass@127.0.0.1/?discoveryInterval=abcd"),
		 InlineData("esdb://user:pass@127.0.0.1/?gossipTimeout=defg"),
		 InlineData("esdb://user:pass@127.0.0.1/?tlsVerifyCert=truee"),
		 InlineData("esdb://user:pass@127.0.0.1/?nodePreference=blabla"),
		 InlineData("esdb://user:pass@127.0.0.1/?keepAlive=blabla")]
		public void connection_string_with_invalid_settings_should_throw(string connectionString) {
			Assert.Throws<InvalidSettingException>(() => EventStoreClientSettings.Create(connectionString));
		}

		[Theory,
		 InlineData("leader", NodePreference.Leader),
		 InlineData("Follower", NodePreference.Follower),
		 InlineData("rAndom", NodePreference.Random),
		 InlineData("ReadOnlyReplica", NodePreference.ReadOnlyReplica)]
		public void with_different_node_preferences(string nodePreference, NodePreference expected) {
			Assert.Equal(expected,
				EventStoreClientSettings.Create($"esdb://user:pass@127.0.0.1/?nodePreference={nodePreference}")
					.ConnectivitySettings.NodePreference);
		}

		[Fact]
		public void with_valid_single_node_connection_string() {
			var settings = EventStoreClientSettings.Create(
				"esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=13&DiscoveryInterval=37&gossipTimeout=33&nOdEPrEfErence=FoLLoWer&tlsVerifyCert=false&keepAlive=10");
			Assert.Equal("user", settings.DefaultCredentials.Username);
			Assert.Equal("pass", settings.DefaultCredentials.Password);
			Assert.Equal("https://127.0.0.1:2113/", settings.ConnectivitySettings.Address.ToString());
			Assert.Empty(settings.ConnectivitySettings.GossipSeeds);
			Assert.Null(settings.ConnectivitySettings.IpGossipSeeds);
			Assert.Null(settings.ConnectivitySettings.DnsGossipSeeds);
			Assert.Equal(13, settings.ConnectivitySettings.MaxDiscoverAttempts);
			Assert.Equal(37, settings.ConnectivitySettings.DiscoveryInterval.TotalMilliseconds);
			Assert.Equal(33, settings.ConnectivitySettings.GossipTimeout.TotalMilliseconds);
			Assert.Equal(NodePreference.Follower, settings.ConnectivitySettings.NodePreference);
			Assert.Equal(TimeSpan.FromMilliseconds(10), settings.ConnectivitySettings.KeepAlive);
#if !GRPC_CORE
			Assert.NotNull(settings.CreateHttpMessageHandler);
#endif
			settings = EventStoreClientSettings.Create(
				"esdb://127.0.0.1?connectionName=test&maxDiscoverAttempts=13&DiscoveryInterval=37&nOdEPrEfErence=FoLLoWer&tls=true&tlsVerifyCert=true&operationTimeout=330&throwOnAppendFailure=faLse&KEepAlive=10");
			Assert.Null(settings.DefaultCredentials);
			Assert.Equal("test", settings.ConnectionName);
			Assert.Equal("https://127.0.0.1:2113/", settings.ConnectivitySettings.Address.ToString());
			Assert.Empty(settings.ConnectivitySettings.GossipSeeds);
			Assert.Null(settings.ConnectivitySettings.IpGossipSeeds);
			Assert.Null(settings.ConnectivitySettings.DnsGossipSeeds);
			Assert.False(settings.ConnectivitySettings.Insecure);
			Assert.Equal(13, settings.ConnectivitySettings.MaxDiscoverAttempts);
			Assert.Equal(37, settings.ConnectivitySettings.DiscoveryInterval.TotalMilliseconds);
			Assert.Equal(NodePreference.Follower, settings.ConnectivitySettings.NodePreference);
			Assert.Equal(TimeSpan.FromMilliseconds(10), settings.ConnectivitySettings.KeepAlive);
#if !GRPC_CORE
			Assert.NotNull(settings.CreateHttpMessageHandler);
#endif

			Assert.Equal(330, settings.OperationOptions.TimeoutAfter.Value.TotalMilliseconds);
			Assert.False(settings.OperationOptions.ThrowOnAppendFailure);

			settings = EventStoreClientSettings.Create("esdb://hostname:4321/?tls=false&keepAlive=-1");
			Assert.Null(settings.DefaultCredentials);
			Assert.Equal("http://hostname:4321/", settings.ConnectivitySettings.Address.ToString());
			Assert.Empty(settings.ConnectivitySettings.GossipSeeds);
			Assert.Null(settings.ConnectivitySettings.IpGossipSeeds);
			Assert.Null(settings.ConnectivitySettings.DnsGossipSeeds);
			Assert.True(settings.ConnectivitySettings.Insecure);
			Assert.Null(settings.ConnectivitySettings.KeepAlive);
#if !GRPC_CORE
			Assert.NotNull(settings.CreateHttpMessageHandler);
#endif
		}

		[Fact]
		public void with_default_settings() {
			var settings = EventStoreClientSettings.Create("esdb://hostname:4321/");

			Assert.Null(settings.ConnectionName);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.Address.Scheme,
				settings.ConnectivitySettings.Address.Scheme);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.DiscoveryInterval.TotalMilliseconds,
				settings.ConnectivitySettings.DiscoveryInterval.TotalMilliseconds);
			Assert.Null(EventStoreClientConnectivitySettings.Default.DnsGossipSeeds);
			Assert.Empty(EventStoreClientConnectivitySettings.Default.GossipSeeds);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.GossipTimeout.TotalMilliseconds,
				settings.ConnectivitySettings.GossipTimeout.TotalMilliseconds);
			Assert.Null(EventStoreClientConnectivitySettings.Default.IpGossipSeeds);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.MaxDiscoverAttempts,
				settings.ConnectivitySettings.MaxDiscoverAttempts);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.NodePreference,
				settings.ConnectivitySettings.NodePreference);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.Insecure,
				settings.ConnectivitySettings.Insecure);
			Assert.Equal(EventStoreClientOperationOptions.Default.TimeoutAfter!.Value.TotalMilliseconds,
				settings.OperationOptions.TimeoutAfter!.Value.TotalMilliseconds);
			Assert.Equal(EventStoreClientOperationOptions.Default.ThrowOnAppendFailure,
				settings.OperationOptions.ThrowOnAppendFailure);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.KeepAlive,
				settings.ConnectivitySettings.KeepAlive);
		}

		[Fact]
		public void with_valid_cluster_connection_string() {
			var settings = EventStoreClientSettings.Create(
				"esdb://user:pass@127.0.0.1,127.0.0.2:3321,127.0.0.3/?maxDiscoverAttempts=13&DiscoveryInterval=37&nOdEPrEfErence=FoLLoWer&tlsVerifyCert=false&KEepAlive=10");
			Assert.Equal("user", settings.DefaultCredentials.Username);
			Assert.Equal("pass", settings.DefaultCredentials.Password);
			Assert.NotEmpty(settings.ConnectivitySettings.GossipSeeds);
			Assert.NotNull(settings.ConnectivitySettings.IpGossipSeeds);
			Assert.Null(settings.ConnectivitySettings.DnsGossipSeeds);
			Assert.False(settings.ConnectivitySettings.Insecure);
			Assert.True(settings.ConnectivitySettings.IpGossipSeeds.Length == 3 &&
			            Equals(settings.ConnectivitySettings.IpGossipSeeds[0].Address, IPAddress.Parse("127.0.0.1")) &&
			            Equals(settings.ConnectivitySettings.IpGossipSeeds[0].Port, 2113) &&
			            Equals(settings.ConnectivitySettings.IpGossipSeeds[1].Address, IPAddress.Parse("127.0.0.2")) &&
			            Equals(settings.ConnectivitySettings.IpGossipSeeds[1].Port, 3321) &&
			            Equals(settings.ConnectivitySettings.IpGossipSeeds[2].Address, IPAddress.Parse("127.0.0.3")) &&
			            Equals(settings.ConnectivitySettings.IpGossipSeeds[2].Port, 2113));
			Assert.Equal(13, settings.ConnectivitySettings.MaxDiscoverAttempts);
			Assert.Equal(37, settings.ConnectivitySettings.DiscoveryInterval.TotalMilliseconds);
			Assert.Equal(NodePreference.Follower, settings.ConnectivitySettings.NodePreference);
			Assert.Equal(TimeSpan.FromMilliseconds(10), settings.ConnectivitySettings.KeepAlive);
#if !GRPC_CORE
			Assert.NotNull(settings.CreateHttpMessageHandler);
#endif


			settings = EventStoreClientSettings.Create(
				"esdb://user:pass@host1,host2:3321,127.0.0.3/?tls=false&maxDiscoverAttempts=13&DiscoveryInterval=37&nOdEPrEfErence=FoLLoWer&tlsVerifyCert=false&KEepAlive=10");
			Assert.Equal("user", settings.DefaultCredentials.Username);
			Assert.Equal("pass", settings.DefaultCredentials.Password);
			Assert.NotEmpty(settings.ConnectivitySettings.GossipSeeds);
			Assert.Null(settings.ConnectivitySettings.IpGossipSeeds);
			Assert.NotNull(settings.ConnectivitySettings.DnsGossipSeeds);
			Assert.True(settings.ConnectivitySettings.Insecure);
			Assert.True(settings.ConnectivitySettings.DnsGossipSeeds.Length == 3 &&
			            Equals(settings.ConnectivitySettings.DnsGossipSeeds[0].Host, "host1") &&
			            Equals(settings.ConnectivitySettings.DnsGossipSeeds[0].Port, 2113) &&
			            Equals(settings.ConnectivitySettings.DnsGossipSeeds[1].Host, "host2") &&
			            Equals(settings.ConnectivitySettings.DnsGossipSeeds[1].Port, 3321) &&
			            Equals(settings.ConnectivitySettings.DnsGossipSeeds[2].Host, "127.0.0.3") &&
			            Equals(settings.ConnectivitySettings.DnsGossipSeeds[2].Port, 2113));
			Assert.Equal(13, settings.ConnectivitySettings.MaxDiscoverAttempts);
			Assert.Equal(37, settings.ConnectivitySettings.DiscoveryInterval.TotalMilliseconds);
			Assert.Equal(NodePreference.Follower, settings.ConnectivitySettings.NodePreference);
			Assert.Equal(TimeSpan.FromMilliseconds(10), settings.ConnectivitySettings.KeepAlive);
#if !GRPC_CORE
			Assert.NotNull(settings.CreateHttpMessageHandler);
#endif
		}

		[Fact]
		public void with_different_tls_settings() {
			EventStoreClientSettings settings;

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1/");
			Assert.Equal(Uri.UriSchemeHttps, settings.ConnectivitySettings.Address.Scheme);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1?tls=true");
			Assert.Equal(Uri.UriSchemeHttps, settings.ConnectivitySettings.Address.Scheme);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1/?tls=FaLsE");
			Assert.Equal(Uri.UriSchemeHttp, settings.ConnectivitySettings.Address.Scheme);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1,127.0.0.2:3321,127.0.0.3/");
			Assert.False(settings.ConnectivitySettings.Insecure);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1,127.0.0.2:3321,127.0.0.3?tls=true");
			Assert.False(settings.ConnectivitySettings.Insecure);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1,127.0.0.2:3321,127.0.0.3/?tls=fAlSe");
			Assert.True(settings.ConnectivitySettings.Insecure);
		}

#if !GRPC_CORE
		[Fact]
		public void with_different_tls_verify_cert_settings() {
			EventStoreClientSettings settings;

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1/");
			Assert.NotNull(settings.CreateHttpMessageHandler);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1/?tlsVerifyCert=TrUe");
			Assert.NotNull(settings.CreateHttpMessageHandler);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1/?tlsVerifyCert=FaLsE");
			Assert.NotNull(settings.CreateHttpMessageHandler);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1,127.0.0.2:3321,127.0.0.3/");
			Assert.NotNull(settings.CreateHttpMessageHandler);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1,127.0.0.2:3321,127.0.0.3/?tlsVerifyCert=true");
			Assert.NotNull(settings.CreateHttpMessageHandler);

			settings = EventStoreClientSettings.Create(
				"esdb://127.0.0.1,127.0.0.2:3321,127.0.0.3/?tlsVerifyCert=false");
			Assert.NotNull(settings.CreateHttpMessageHandler);
		}
#endif

		public static IEnumerable<object[]> DiscoverSchemeCases() {
			yield return new object[] {
				"esdb+discover://hostname:4321", new EndPoint[] {
					new DnsEndPoint("hostname", 4321)
				}
			};
			yield return new object[] {
				"esdb+discover://hostname:4321/", new EndPoint[] {
					new DnsEndPoint("hostname", 4321)
				}
			};
			yield return new object[] {
				"esdb+discover://hostname0:4321,hostname1:4321,hostname2:4321/", new EndPoint[] {
					new DnsEndPoint("hostname0", 4321),
					new DnsEndPoint("hostname1", 4321),
					new DnsEndPoint("hostname2", 4321)
				}
			};
		}

		[Theory, MemberData(nameof(DiscoverSchemeCases))]
		public void with_esdb_discover_scheme(string connectionString, EndPoint[] expected) {
			var settings = EventStoreClientSettings.Create(connectionString);
			Assert.Equal(expected, settings.ConnectivitySettings.GossipSeeds);
		}
	}
}
