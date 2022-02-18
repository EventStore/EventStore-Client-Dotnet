using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
#if !GRPC_CORE
using System.Net.Http;
#endif
using AutoFixture;
using Xunit;

namespace EventStore.Client {
	public class ConnectionStringTests {
		public static IEnumerable<object[]> ValidCases() {
			var fixture = new Fixture();
			fixture.Customize<TimeSpan>(composer => composer.FromFactory<int>(s => TimeSpan.FromSeconds(s % 60)));
			fixture.Customize<Uri>(composer => composer.FromFactory<DnsEndPoint>(e => new UriBuilder {
				Host = e.Host,
				Port = e.Port == 80 ? 81 : e.Port
			}.Uri));

			return Enumerable.Range(0, 3).SelectMany(GetTestCases);

			IEnumerable<object[]> GetTestCases(int _) {
				var settings = new EventStoreClientSettings {
					ConnectionName = fixture.Create<string>(),
					ConnectivitySettings = fixture.Create<EventStoreClientConnectivitySettings>(),
					OperationOptions = fixture.Create<EventStoreClientOperationOptions>()
				};

				settings.ConnectivitySettings.Address =
					new UriBuilder(EventStoreClientConnectivitySettings.Default.Address) {
						Scheme = settings.ConnectivitySettings.Insecure ? Uri.UriSchemeHttp : Uri.UriSchemeHttps
					}.Uri;

				yield return new object[] {
					GetConnectionString(settings),
					settings
				};

				yield return new object[] {
					GetConnectionString(settings, MockingTone),
					settings
				};

				var ipGossipSettings = new EventStoreClientSettings {
					ConnectionName = fixture.Create<string>(),
					ConnectivitySettings = fixture.Create<EventStoreClientConnectivitySettings>(),
					OperationOptions = fixture.Create<EventStoreClientOperationOptions>()
				};

				ipGossipSettings.ConnectivitySettings.Address =
					new UriBuilder(EventStoreClientConnectivitySettings.Default.Address) {
						Scheme = ipGossipSettings.ConnectivitySettings.Insecure ? Uri.UriSchemeHttp : Uri.UriSchemeHttps
					}.Uri;

				ipGossipSettings.ConnectivitySettings.DnsGossipSeeds = null;

				yield return new object[] {
					GetConnectionString(ipGossipSettings),
					ipGossipSettings
				};

				yield return new object[] {
					GetConnectionString(ipGossipSettings, MockingTone),
					ipGossipSettings
				};

				var singleNodeSettings = new EventStoreClientSettings {
					ConnectionName = fixture.Create<string>(),
					ConnectivitySettings = fixture.Create<EventStoreClientConnectivitySettings>(),
					OperationOptions = fixture.Create<EventStoreClientOperationOptions>()
				};
				singleNodeSettings.ConnectivitySettings.DnsGossipSeeds = null;
				singleNodeSettings.ConnectivitySettings.IpGossipSeeds = null;
				singleNodeSettings.ConnectivitySettings.Address = new UriBuilder(fixture.Create<Uri>()) {
					Scheme = singleNodeSettings.ConnectivitySettings.Insecure ? Uri.UriSchemeHttp : Uri.UriSchemeHttps
				}.Uri;

				yield return new object[] {
					GetConnectionString(singleNodeSettings),
					singleNodeSettings
				};

				yield return new object[] {
					GetConnectionString(singleNodeSettings, MockingTone),
					singleNodeSettings
				};
			}

			static string MockingTone(string key) =>
				new string(key.Select((c, i) => i % 2 == 0 ? char.ToUpper(c) : char.ToLower(c)).ToArray());
		}

		[Theory, MemberData(nameof(ValidCases))]
		public void valid_connection_string(string connectionString, EventStoreClientSettings expected) {
			var result = EventStoreClientSettings.Create(connectionString);

			Assert.Equal(expected, result, EventStoreClientSettingsEqualityComparer.Instance);
		}

		[Theory, MemberData(nameof(ValidCases))]
		public void valid_connection_string_with_empty_path(string connectionString, EventStoreClientSettings expected) {
			var result = EventStoreClientSettings.Create(connectionString.Replace("?", "/?"));

			Assert.Equal(expected, result, EventStoreClientSettingsEqualityComparer.Instance);
		}

#if !GRPC_CORE
		[Theory, InlineData(false), InlineData(true)]
		public void tls_verify_cert(bool tlsVerifyCert) {
			var connectionString = $"esdb://localhost:2113/?tlsVerifyCert={tlsVerifyCert}";
			var result = EventStoreClientSettings.Create(connectionString);
			using var handler = result.CreateHttpMessageHandler?.Invoke();
			var socketsHandler = Assert.IsType<SocketsHttpHandler>(handler);
			if (!tlsVerifyCert) {
				Assert.NotNull(socketsHandler.SslOptions.RemoteCertificateValidationCallback);
				Assert.True(socketsHandler.SslOptions.RemoteCertificateValidationCallback.Invoke(null!, default,
					default, default));
			} else {
				Assert.Null(socketsHandler.SslOptions.RemoteCertificateValidationCallback);
			}
		}

#endif

		[Fact]
		public void infinite_grpc_timeouts() {
			var result =
				EventStoreClientSettings.Create("esdb://localhost:2113?keepAliveInterval=-1&keepAliveTimeout=-1");

			Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, result.ConnectivitySettings.KeepAliveInterval);
			Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, result.ConnectivitySettings.KeepAliveTimeout);

#if !GRPC_CORE
			using var handler = result.CreateHttpMessageHandler?.Invoke();
			var socketsHandler = Assert.IsType<SocketsHttpHandler>(handler);
			Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, socketsHandler.KeepAlivePingTimeout);
			Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, socketsHandler.KeepAlivePingDelay);
#endif
		}

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
		 InlineData("esdb://user:pass@127.0.0.1/?keepAliveInterval=-2"),
		 InlineData("esdb://user:pass@127.0.0.1/?keepAliveTimeout=-2")]
		public void connection_string_with_invalid_settings_should_throw(string connectionString) {
			Assert.Throws<InvalidSettingException>(() => EventStoreClientSettings.Create(connectionString));
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
			Assert.Equal(TimeSpan.FromSeconds(10), settings.DefaultDeadline);
			Assert.Equal(EventStoreClientOperationOptions.Default.ThrowOnAppendFailure,
				settings.OperationOptions.ThrowOnAppendFailure);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.KeepAliveInterval,
				settings.ConnectivitySettings.KeepAliveInterval);
			Assert.Equal(EventStoreClientConnectivitySettings.Default.KeepAliveTimeout,
				settings.ConnectivitySettings.KeepAliveTimeout);
		}

		private static string GetConnectionString(EventStoreClientSettings settings,
			Func<string, string> getKey = default)
			=> $"{GetScheme(settings)}{GetAuthority(settings)}?{GetKeyValuePairs(settings, getKey)}";

		private static string GetScheme(EventStoreClientSettings settings) => settings.ConnectivitySettings.IsSingleNode
			? "esdb://"
			: "esdb+discover://";

		private static string GetAuthority(EventStoreClientSettings settings) =>
			settings.ConnectivitySettings.IsSingleNode
				? $"{settings.ConnectivitySettings.Address.Host}:{settings.ConnectivitySettings.Address.Port}"
				: string.Join(",",
					settings.ConnectivitySettings.GossipSeeds.Select(x => $"{x.GetHost()}:{x.GetPort()}"));

		private static string GetKeyValuePairs(EventStoreClientSettings settings,
			Func<string, string> getKey = default) {
			var pairs = new Dictionary<string, string> {
				["tls"] = (!settings.ConnectivitySettings.Insecure).ToString(),
				["connectionName"] = settings.ConnectionName,
				["maxDiscoverAttempts"] = settings.ConnectivitySettings.MaxDiscoverAttempts.ToString(),
				["discoveryInterval"] = settings.ConnectivitySettings.DiscoveryInterval.TotalMilliseconds.ToString(),
				["gossipTimeout"] = settings.ConnectivitySettings.GossipTimeout.TotalMilliseconds.ToString(),
				["nodePreference"] = settings.ConnectivitySettings.NodePreference.ToString(),
				["keepAliveInterval"] = settings.ConnectivitySettings.KeepAliveInterval.TotalMilliseconds.ToString(),
				["keepAliveTimeout"] = settings.ConnectivitySettings.KeepAliveTimeout.TotalMilliseconds.ToString(),
			};

			if (settings.DefaultDeadline.HasValue) {
				pairs.Add("defaultDeadline",
					settings.DefaultDeadline.Value.TotalMilliseconds.ToString());
			}

#if !GRPC_CORE
			if (settings.CreateHttpMessageHandler != null) {
				using var handler = settings.CreateHttpMessageHandler.Invoke();
				if (handler is SocketsHttpHandler socketsHttpHandler &&
				    socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback != null) {
					pairs.Add("tlsVerifyCert", "false");
				}
			}
#endif


			return string.Join("&", pairs.Select(pair => $"{getKey?.Invoke(pair.Key) ?? pair.Key}={pair.Value}"));
		}

		private class EventStoreClientSettingsEqualityComparer : IEqualityComparer<EventStoreClientSettings> {
			public static readonly EventStoreClientSettingsEqualityComparer Instance =
				new EventStoreClientSettingsEqualityComparer();

			public bool Equals(EventStoreClientSettings x, EventStoreClientSettings y) {
				if (ReferenceEquals(x, y)) return true;
				if (ReferenceEquals(x, null)) return false;
				if (ReferenceEquals(y, null)) return false;
				if (x.GetType() != y.GetType()) return false;
				return x.ConnectionName == y.ConnectionName &&
				       EventStoreClientConnectivitySettingsEqualityComparer.Instance.Equals(x.ConnectivitySettings,
					       y.ConnectivitySettings) &&
				       EventStoreClientOperationOptionsEqualityComparer.Instance.Equals(x.OperationOptions,
					       y.OperationOptions) &&
				       Equals(x.DefaultCredentials?.ToString(), y.DefaultCredentials?.ToString());
			}

			public int GetHashCode(EventStoreClientSettings obj) => HashCode.Hash
				.Combine(obj.ConnectionName)
				.Combine(EventStoreClientConnectivitySettingsEqualityComparer.Instance.GetHashCode(
					obj.ConnectivitySettings))
				.Combine(EventStoreClientOperationOptionsEqualityComparer.Instance.GetHashCode(obj.OperationOptions));
		}

		private class EventStoreClientConnectivitySettingsEqualityComparer
			: IEqualityComparer<EventStoreClientConnectivitySettings> {
			public static readonly EventStoreClientConnectivitySettingsEqualityComparer Instance =
				new EventStoreClientConnectivitySettingsEqualityComparer();

			public bool Equals(EventStoreClientConnectivitySettings x, EventStoreClientConnectivitySettings y) {
				if (ReferenceEquals(x, y)) return true;
				if (ReferenceEquals(x, null)) return false;
				if (ReferenceEquals(y, null)) return false;
				if (x.GetType() != y.GetType()) return false;
				return (!x.IsSingleNode || x.Address.Equals(y.Address)) &&
				       x.MaxDiscoverAttempts == y.MaxDiscoverAttempts &&
				       x.GossipSeeds.SequenceEqual(y.GossipSeeds) &&
				       x.GossipTimeout.Equals(y.GossipTimeout) &&
				       x.DiscoveryInterval.Equals(y.DiscoveryInterval) &&
				       x.NodePreference == y.NodePreference &&
				       x.KeepAliveInterval.Equals(y.KeepAliveInterval) &&
				       x.KeepAliveTimeout.Equals(y.KeepAliveTimeout) &&
				       x.Insecure == y.Insecure;
			}

			public int GetHashCode(EventStoreClientConnectivitySettings obj) =>
				obj.GossipSeeds.Aggregate(
					HashCode.Hash
						.Combine(obj.Address.GetHashCode())
						.Combine(obj.MaxDiscoverAttempts)
						.Combine(obj.GossipTimeout)
						.Combine(obj.DiscoveryInterval)
						.Combine(obj.NodePreference)
						.Combine(obj.KeepAliveInterval)
						.Combine(obj.KeepAliveTimeout)
						.Combine(obj.Insecure), (hashcode, endpoint) => hashcode.Combine(endpoint.GetHashCode()));
		}

		private class EventStoreClientOperationOptionsEqualityComparer
			: IEqualityComparer<EventStoreClientOperationOptions> {
			public static readonly EventStoreClientOperationOptionsEqualityComparer Instance = new();

			public bool Equals(EventStoreClientOperationOptions x, EventStoreClientOperationOptions y) {
				if (ReferenceEquals(x, y)) return true;
				if (ReferenceEquals(x, null)) return false;
				if (ReferenceEquals(y, null)) return false;
				return x.GetType() == y.GetType();
			}

			public int GetHashCode(EventStoreClientOperationOptions obj) =>
				System.HashCode.Combine(obj.ThrowOnAppendFailure);
		}
	}
}
