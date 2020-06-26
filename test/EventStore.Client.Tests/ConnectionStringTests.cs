using System;
using System.Net;
using Xunit;

namespace EventStore.Client {
	public class ConnectionStringTests {
		[Fact]
		public void connection_string_with_no_schema() {
			Assert.Throws<NoSchemeException>(() => {
				EventStoreClientSettings.Create(":so/mething/random");
			});
		}

		[Fact]
		public void connection_string_with_invalid_scheme_should_throw() {
			Assert.Throws<InvalidSchemeException>(() => {
				EventStoreClientSettings.Create("esdbwrong://");
			});

			Assert.Throws<InvalidSchemeException>(() => {
				EventStoreClientSettings.Create("wrong://");
			});

			Assert.Throws<InvalidSchemeException>(() => {
				EventStoreClientSettings.Create("badesdb://");
			});
		}

		[Fact]
		public void connection_string_with_invalid_userinfo_should_throw() {
			Assert.Throws<InvalidUserCredentialsException>(() => {
				EventStoreClientSettings.Create("esdb://userpass@127.0.0.1/");
			});

			Assert.Throws<InvalidUserCredentialsException>(() => {
				EventStoreClientSettings.Create("esdb://user:pa:ss@127.0.0.1/");
			});

			Assert.Throws<InvalidUserCredentialsException>(() => {
				EventStoreClientSettings.Create("esdb://us:er:pa:ss@127.0.0.1/");
			});
		}

		[Fact]
		public void connection_string_without_forward_slash_after_host_should_throw() {
			Assert.Throws<ConnectionStringParseException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1");
			});

			Assert.Throws<ConnectionStringParseException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1:1234");
			});
		}

		[Fact]
		public void connection_string_with_invalid_host_should_throw() {
			Assert.Throws<InvalidHostException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1:abc/");
			});

			Assert.Throws<InvalidHostException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1:1234,127.0.0.2:abc,127.0.0.3:4321/");
			});

			Assert.Throws<InvalidHostException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1:abc:def/");
			});

			Assert.Throws<InvalidHostException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@localhost:1234,127.0.0.2:abc:def,127.0.0.3:4321/");
			});

			Assert.Throws<InvalidHostException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@localhost:1234,,127.0.0.3:4321/");
			});
		}

		[Fact]
		public void connection_string_with_invalid_key_value_pair_should_throw() {
			Assert.Throws<InvalidKeyValuePairException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=12=34");
			});

			Assert.Throws<InvalidKeyValuePairException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts1234");
			});
		}

		[Fact]
		public void connection_string_with_invalid_settings_should_throw() {
			Assert.Throws<InvalidSettingException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?unknown=1234");
			});

			Assert.Throws<InvalidSettingException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=1234&hello=test");
			});

			Assert.Throws<InvalidSettingException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=abcd");
			});

			Assert.Throws<InvalidSettingException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?discoveryInterval=abcd");
			});

			Assert.Throws<InvalidSettingException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?gossipTimeout=defg");
			});

			Assert.Throws<InvalidSettingException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?tlsVerifyCert=truee");
			});

			Assert.Throws<InvalidSettingException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?nodePreference=blabla");
			});
		}

		[Fact]
		public void with_different_node_preferences() {
			Assert.Equal(NodePreference.Leader, EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?nodePreference=leader").ConnectivitySettings.NodePreference);
			Assert.Equal(NodePreference.Follower, EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?nodePreference=Follower").ConnectivitySettings.NodePreference);
			Assert.Equal(NodePreference.Random, EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?nodePreference=rAndom").ConnectivitySettings.NodePreference);
			Assert.Equal(NodePreference.ReadOnlyReplica, EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?nodePreference=ReadOnlyReplica").ConnectivitySettings.NodePreference);

			Assert.Throws<InvalidSettingException>(() => {
				EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?nodePreference=invalid");
			});
		}

		[Fact]
		public void with_valid_single_node_connection_string() {
			EventStoreClientSettings settings;

			settings = EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=13&DiscoveryInterval=37&gossipTimeout=33&nOdEPrEfErence=FoLLoWer&tlsVerifyCert=false");
			Assert.Equal("user", settings.DefaultCredentials.Username);
			Assert.Equal("pass", settings.DefaultCredentials.Password);
			Assert.Equal("https://127.0.0.1:2113/",settings.ConnectivitySettings.Address.ToString());
			Assert.Empty(settings.ConnectivitySettings.GossipSeeds);
			Assert.Null(settings.ConnectivitySettings.IpGossipSeeds);
			Assert.Null(settings.ConnectivitySettings.DnsGossipSeeds);
			Assert.Equal(13, settings.ConnectivitySettings.MaxDiscoverAttempts);
			Assert.Equal(37, settings.ConnectivitySettings.DiscoveryInterval.TotalSeconds);
			Assert.Equal(33, settings.ConnectivitySettings.GossipTimeout.TotalSeconds);
			Assert.Equal(NodePreference.Follower, settings.ConnectivitySettings.NodePreference);
			Assert.NotNull(settings.CreateHttpMessageHandler);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1/?connectionName=test&maxDiscoverAttempts=13&DiscoveryInterval=37&nOdEPrEfErence=FoLLoWer&tls=true&tlsVerifyCert=true&operationTimeout=330&throwOnAppendFailure=faLse");
			Assert.Null(settings.DefaultCredentials);
			Assert.Equal("test", settings.ConnectionName);
			Assert.Equal("https://127.0.0.1:2113/",settings.ConnectivitySettings.Address.ToString());
			Assert.Empty(settings.ConnectivitySettings.GossipSeeds);
			Assert.Null(settings.ConnectivitySettings.IpGossipSeeds);
			Assert.Null(settings.ConnectivitySettings.DnsGossipSeeds);
			Assert.True(settings.ConnectivitySettings.GossipOverHttps);
			Assert.Equal(13, settings.ConnectivitySettings.MaxDiscoverAttempts);
			Assert.Equal(37, settings.ConnectivitySettings.DiscoveryInterval.TotalSeconds);
			Assert.Equal(NodePreference.Follower, settings.ConnectivitySettings.NodePreference);
			Assert.Null(settings.CreateHttpMessageHandler);
			Assert.Equal(330, settings.OperationOptions.TimeoutAfter.Value.TotalSeconds);
			Assert.False(settings.OperationOptions.ThrowOnAppendFailure);

			settings = EventStoreClientSettings.Create("esdb://hostname:4321/?tls=false");
			Assert.Null(settings.DefaultCredentials);
			Assert.Equal("http://hostname:4321/",settings.ConnectivitySettings.Address.ToString());
			Assert.Empty(settings.ConnectivitySettings.GossipSeeds);
			Assert.Null(settings.ConnectivitySettings.IpGossipSeeds);
			Assert.Null(settings.ConnectivitySettings.DnsGossipSeeds);
			Assert.True(settings.ConnectivitySettings.GossipOverHttps);
			Assert.Null(settings.CreateHttpMessageHandler);
		}

		[Fact]
		public void with_default_settings() {
			EventStoreClientSettings settings;
			settings = EventStoreClientSettings.Create("esdb://hostname:4321/");

			Assert.Null(settings.ConnectionName);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.Address.Scheme, settings.ConnectivitySettings.Address.Scheme);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.DiscoveryInterval.TotalSeconds, settings.ConnectivitySettings.DiscoveryInterval.TotalSeconds);
			Assert.Null(EventStoreClientConnectivitySettings.Default.DnsGossipSeeds);
			Assert.Empty(EventStoreClientConnectivitySettings.Default.GossipSeeds);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.GossipTimeout.TotalSeconds, settings.ConnectivitySettings.GossipTimeout.TotalSeconds);
			Assert.Null(EventStoreClientConnectivitySettings.Default.IpGossipSeeds);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.MaxDiscoverAttempts, settings.ConnectivitySettings.MaxDiscoverAttempts);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.NodePreference, settings.ConnectivitySettings.NodePreference);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.GossipOverHttps, settings.ConnectivitySettings.GossipOverHttps);
			Assert.Equal(EventStoreClientOperationOptions.Default.TimeoutAfter.Value.TotalSeconds, settings.OperationOptions.TimeoutAfter.Value.TotalSeconds);
			Assert.Equal(EventStoreClientOperationOptions.Default.ThrowOnAppendFailure, settings.OperationOptions.ThrowOnAppendFailure);
		}

		[Fact]
		public void with_valid_cluster_connection_string() {
			EventStoreClientSettings settings;

			settings = EventStoreClientSettings.Create("esdb://user:pass@127.0.0.1,127.0.0.2:3321,127.0.0.3/?maxDiscoverAttempts=13&DiscoveryInterval=37&nOdEPrEfErence=FoLLoWer&tlsVerifyCert=false");
			Assert.Equal("user", settings.DefaultCredentials.Username);
			Assert.Equal("pass", settings.DefaultCredentials.Password);
			Assert.NotEmpty(settings.ConnectivitySettings.GossipSeeds);
			Assert.NotNull(settings.ConnectivitySettings.IpGossipSeeds);
			Assert.Null(settings.ConnectivitySettings.DnsGossipSeeds);
			Assert.True(settings.ConnectivitySettings.GossipOverHttps);
			Assert.True(settings.ConnectivitySettings.IpGossipSeeds.Length == 3 &&
			            Equals(settings.ConnectivitySettings.IpGossipSeeds[0].Address, IPAddress.Parse("127.0.0.1")) &&
			            Equals(settings.ConnectivitySettings.IpGossipSeeds[0].Port, 2113) &&
			            Equals(settings.ConnectivitySettings.IpGossipSeeds[1].Address, IPAddress.Parse("127.0.0.2")) &&
			            Equals(settings.ConnectivitySettings.IpGossipSeeds[1].Port, 3321) &&
			            Equals(settings.ConnectivitySettings.IpGossipSeeds[2].Address, IPAddress.Parse("127.0.0.3")) &&
			            Equals(settings.ConnectivitySettings.IpGossipSeeds[2].Port, 2113));
			Assert.Equal(13, settings.ConnectivitySettings.MaxDiscoverAttempts);
			Assert.Equal(37, settings.ConnectivitySettings.DiscoveryInterval.TotalSeconds);
			Assert.Equal(NodePreference.Follower, settings.ConnectivitySettings.NodePreference);
			Assert.NotNull(settings.CreateHttpMessageHandler);


			settings = EventStoreClientSettings.Create("esdb://user:pass@host1,host2:3321,127.0.0.3/?tls=false&maxDiscoverAttempts=13&DiscoveryInterval=37&nOdEPrEfErence=FoLLoWer&tlsVerifyCert=false");
			Assert.Equal("user", settings.DefaultCredentials.Username);
			Assert.Equal("pass", settings.DefaultCredentials.Password);
			Assert.NotEmpty(settings.ConnectivitySettings.GossipSeeds);
			Assert.Null(settings.ConnectivitySettings.IpGossipSeeds);
			Assert.NotNull(settings.ConnectivitySettings.DnsGossipSeeds);
			Assert.False(settings.ConnectivitySettings.GossipOverHttps);
			Assert.True(settings.ConnectivitySettings.DnsGossipSeeds.Length == 3 &&
			            Equals(settings.ConnectivitySettings.DnsGossipSeeds[0].Host, "host1") &&
			            Equals(settings.ConnectivitySettings.DnsGossipSeeds[0].Port, 2113) &&
			            Equals(settings.ConnectivitySettings.DnsGossipSeeds[1].Host, "host2") &&
			            Equals(settings.ConnectivitySettings.DnsGossipSeeds[1].Port, 3321) &&
			            Equals(settings.ConnectivitySettings.DnsGossipSeeds[2].Host, "127.0.0.3") &&
			            Equals(settings.ConnectivitySettings.DnsGossipSeeds[2].Port, 2113));
			Assert.Equal(13, settings.ConnectivitySettings.MaxDiscoverAttempts);
			Assert.Equal(37, settings.ConnectivitySettings.DiscoveryInterval.TotalSeconds);
			Assert.Equal(NodePreference.Follower, settings.ConnectivitySettings.NodePreference);
			Assert.NotNull(settings.CreateHttpMessageHandler);
		}

		[Fact]
		public void with_different_tls_settings() {
			EventStoreClientSettings settings;

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1/");
			Assert.Equal(Uri.UriSchemeHttps, settings.ConnectivitySettings.Address.Scheme);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1/?tls=true");
			Assert.Equal(Uri.UriSchemeHttps, settings.ConnectivitySettings.Address.Scheme);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1/?tls=FaLsE");
			Assert.Equal(Uri.UriSchemeHttp, settings.ConnectivitySettings.Address.Scheme);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1,127.0.0.2:3321,127.0.0.3/");
			Assert.True(settings.ConnectivitySettings.GossipOverHttps);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1,127.0.0.2:3321,127.0.0.3/?tls=true");
			Assert.True(settings.ConnectivitySettings.GossipOverHttps);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1,127.0.0.2:3321,127.0.0.3/?tls=fAlSe");
			Assert.False(settings.ConnectivitySettings.GossipOverHttps);
		}

		[Fact]
		public void with_different_tls_verify_cert_settings() {
			EventStoreClientSettings settings;

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1/");
			Assert.Null(settings.CreateHttpMessageHandler);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1/?tlsVerifyCert=TrUe");
			Assert.Null(settings.CreateHttpMessageHandler);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1/?tlsVerifyCert=FaLsE");
			Assert.NotNull(settings.CreateHttpMessageHandler);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1,127.0.0.2:3321,127.0.0.3/");
			Assert.Null(settings.CreateHttpMessageHandler);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1,127.0.0.2:3321,127.0.0.3/?tlsVerifyCert=true");
			Assert.Null(settings.CreateHttpMessageHandler);

			settings = EventStoreClientSettings.Create("esdb://127.0.0.1,127.0.0.2:3321,127.0.0.3/?tlsVerifyCert=false");
			Assert.NotNull(settings.CreateHttpMessageHandler);
		}

	}
}
