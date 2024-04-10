using System.Net;
using System.Net.Http;
using AutoFixture;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace EventStore.Client.Tests;

public class ConnectionStringTests {
	public static IEnumerable<object?[]> ValidCases() {
		var fixture = new Fixture();
		fixture.Customize<TimeSpan>(composer => composer.FromFactory<int>(s => TimeSpan.FromSeconds(s % 60)));
		fixture.Customize<Uri>(
			composer => composer.FromFactory<DnsEndPoint>(
				e => new UriBuilder {
					Host = e.Host,
					Port = e.Port == 80 ? 81 : e.Port
				}.Uri
			)
		);

		fixture.Register<X509Certificate2>(() => null!);

		return Enumerable.Range(0, 3).SelectMany(GetTestCases);

		IEnumerable<object?[]> GetTestCases(int _) {
			var settings = new EventStoreClientSettings {
				ConnectionName       = fixture.Create<string>(),
				ConnectivitySettings = fixture.Create<EventStoreClientConnectivitySettings>(),
				OperationOptions     = fixture.Create<EventStoreClientOperationOptions>()
			};

			settings.ConnectivitySettings.Address =
				new UriBuilder(EventStoreClientConnectivitySettings.Default.ResolvedAddressOrDefault) {
					Scheme = settings.ConnectivitySettings.ResolvedAddressOrDefault.Scheme
				}.Uri;

			yield return new object?[] {
				GetConnectionString(settings),
				settings
			};

			yield return new object?[] {
				GetConnectionString(settings, MockingTone),
				settings
			};

			var ipGossipSettings = new EventStoreClientSettings {
				ConnectionName       = fixture.Create<string>(),
				ConnectivitySettings = fixture.Create<EventStoreClientConnectivitySettings>(),
				OperationOptions     = fixture.Create<EventStoreClientOperationOptions>()
			};

			ipGossipSettings.ConnectivitySettings.Address =
				new UriBuilder(EventStoreClientConnectivitySettings.Default.ResolvedAddressOrDefault) {
					Scheme = ipGossipSettings.ConnectivitySettings.ResolvedAddressOrDefault.Scheme
				}.Uri;

			ipGossipSettings.ConnectivitySettings.DnsGossipSeeds = null;

			yield return new object?[] {
				GetConnectionString(ipGossipSettings),
				ipGossipSettings
			};

			yield return new object?[] {
				GetConnectionString(ipGossipSettings, MockingTone),
				ipGossipSettings
			};

			var singleNodeSettings = new EventStoreClientSettings {
				ConnectionName       = fixture.Create<string>(),
				ConnectivitySettings = fixture.Create<EventStoreClientConnectivitySettings>(),
				OperationOptions     = fixture.Create<EventStoreClientOperationOptions>()
			};

			singleNodeSettings.ConnectivitySettings.DnsGossipSeeds = null;
			singleNodeSettings.ConnectivitySettings.IpGossipSeeds  = null;
			singleNodeSettings.ConnectivitySettings.Address = new UriBuilder(fixture.Create<Uri>()) {
				Scheme = singleNodeSettings.ConnectivitySettings.ResolvedAddressOrDefault.Scheme
			}.Uri;

			yield return new object?[] {
				GetConnectionString(singleNodeSettings),
				singleNodeSettings
			};

			yield return new object?[] {
				GetConnectionString(singleNodeSettings, MockingTone),
				singleNodeSettings
			};
		}

		static string MockingTone(string key) => new(key.Select((c, i) => i % 2 == 0 ? char.ToUpper(c) : char.ToLower(c)).ToArray());
	}

	[Theory]
	[MemberData(nameof(ValidCases))]
	public void valid_connection_string(string connectionString, EventStoreClientSettings expected) {
		var result = EventStoreClientSettings.Create(connectionString);

		Assert.Equal(expected, result, EventStoreClientSettingsEqualityComparer.Instance);
	}

	[Theory]
	[MemberData(nameof(ValidCases))]
	public void valid_connection_string_with_empty_path(string connectionString, EventStoreClientSettings expected) {
		var result = EventStoreClientSettings.Create(connectionString.Replace("?", "/?"));

		Assert.Equal(expected, result, EventStoreClientSettingsEqualityComparer.Instance);
	}

#if !GRPC_CORE
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void tls_verify_cert(bool tlsVerifyCert) {
		var       connectionString = $"esdb://localhost:2113/?tlsVerifyCert={tlsVerifyCert}";
		var       result           = EventStoreClientSettings.Create(connectionString);
		using var handler          = result.CreateHttpMessageHandler?.Invoke();
#if NET
		var socketsHandler = Assert.IsType<SocketsHttpHandler>(handler);
		if (!tlsVerifyCert) {
			Assert.NotNull(socketsHandler.SslOptions.RemoteCertificateValidationCallback);
			Assert.True(
				socketsHandler.SslOptions.RemoteCertificateValidationCallback!.Invoke(
					null!,
					default,
					default,
					default
				)
			);
		}
		else {
			Assert.Null(socketsHandler.SslOptions.RemoteCertificateValidationCallback);
		}
#else
		var socketsHandler = Assert.IsType<WinHttpHandler>(handler);
		if (!tlsVerifyCert) {
			Assert.NotNull(socketsHandler.ServerCertificateValidationCallback);
			Assert.True(socketsHandler.ServerCertificateValidationCallback!.Invoke(null!, default!,
				default!, default));
		} else {
			Assert.Null(socketsHandler.ServerCertificateValidationCallback);
		}
#endif
	}

#endif

	public static IEnumerable<object?[]> InvalidTlsCertificates() {
		yield return new object?[] { Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "path", "not", "found") };
		yield return new object?[] { Assembly.GetExecutingAssembly().Location };
	}

	[Theory]
	[MemberData(nameof(InvalidTlsCertificates))]
	public void connection_string_with_invalid_tls_certificate_should_throw(string clientCertificatePath) {
		Assert.Throws<InvalidClientCertificateException >(
			() => EventStoreClientSettings.Create(
				$"esdb://admin:changeit@localhost:2113/?tls=true&tlsVerifyCert=true&tlsCAFile={clientCertificatePath}"
			)
		);
	}

	public static IEnumerable<object?[]> InvalidClientCertificates() {
		var invalidPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "path", "not", "found");
		var validPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certs", "ca", "ca.crt");
		yield return [invalidPath, invalidPath];
		yield return [validPath, invalidPath];
	}

	[Theory]
	[MemberData(nameof(InvalidClientCertificates))]
	public void connection_string_with_invalid_client_certificate_should_throw(string certPath, string certKeyPath) {
		Assert.Throws<InvalidClientCertificateException>(
			() => EventStoreClientSettings.Create(
				$"esdb://admin:changeit@localhost:2113/?tls=true&tlsVerifyCert=true&certPath={certPath}&certKeyPath={certKeyPath}"
			)
		);
	}

	[Fact]
	public void infinite_grpc_timeouts() {
		var result = EventStoreClientSettings.Create("esdb://localhost:2113?keepAliveInterval=-1&keepAliveTimeout=-1");

		Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, result.ConnectivitySettings.KeepAliveInterval);
		Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, result.ConnectivitySettings.KeepAliveTimeout);

		using var handler = result.CreateHttpMessageHandler?.Invoke();

#if NET
		var socketsHandler = Assert.IsType<SocketsHttpHandler>(handler);
		Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, socketsHandler.KeepAlivePingTimeout);
		Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, socketsHandler.KeepAlivePingDelay);
#else
		var winHttpHandler = Assert.IsType<WinHttpHandler>(handler);
		Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, winHttpHandler.TcpKeepAliveTime);
		Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, winHttpHandler.TcpKeepAliveInterval);
#endif
	}

	[Fact]
	public void connection_string_with_no_schema() => Assert.Throws<NoSchemeException>(() => EventStoreClientSettings.Create(":so/mething/random"));

	[Theory]
	[InlineData("esdbwrong://")]
	[InlineData("wrong://")]
	[InlineData("badesdb://")]
	public void connection_string_with_invalid_scheme_should_throw(string connectionString) =>
		Assert.Throws<InvalidSchemeException>(() => EventStoreClientSettings.Create(connectionString));

	[Theory]
	[InlineData("esdb://userpass@127.0.0.1/")]
	[InlineData("esdb://user:pa:ss@127.0.0.1/")]
	[InlineData("esdb://us:er:pa:ss@127.0.0.1/")]
	public void connection_string_with_invalid_userinfo_should_throw(string connectionString) =>
		Assert.Throws<InvalidUserCredentialsException>(() => EventStoreClientSettings.Create(connectionString));

	[Theory]
	[InlineData("esdb://user:pass@127.0.0.1:abc")]
	[InlineData("esdb://user:pass@127.0.0.1:abc/")]
	[InlineData("esdb://user:pass@127.0.0.1:1234,127.0.0.2:abc,127.0.0.3:4321")]
	[InlineData("esdb://user:pass@127.0.0.1:1234,127.0.0.2:abc,127.0.0.3:4321/")]
	[InlineData("esdb://user:pass@127.0.0.1:abc:def")]
	[InlineData("esdb://user:pass@127.0.0.1:abc:def/")]
	[InlineData("esdb://user:pass@localhost:1234,127.0.0.2:abc:def,127.0.0.3:4321")]
	[InlineData("esdb://user:pass@localhost:1234,127.0.0.2:abc:def,127.0.0.3:4321/")]
	[InlineData("esdb://user:pass@localhost:1234,,127.0.0.3:4321")]
	[InlineData("esdb://user:pass@localhost:1234,,127.0.0.3:4321/")]
	public void connection_string_with_invalid_host_should_throw(string connectionString) =>
		Assert.Throws<InvalidHostException>(() => EventStoreClientSettings.Create(connectionString));

	[Theory]
	[InlineData("esdb://user:pass@127.0.0.1/test")]
	[InlineData("esdb://user:pass@127.0.0.1/maxDiscoverAttempts=10")]
	[InlineData("esdb://user:pass@127.0.0.1/hello?maxDiscoverAttempts=10")]
	public void connection_string_with_non_empty_path_should_throw(string connectionString) =>
		Assert.Throws<ConnectionStringParseException>(() => EventStoreClientSettings.Create(connectionString));

	[Theory]
	[InlineData("esdb://user:pass@127.0.0.1")]
	[InlineData("esdb://user:pass@127.0.0.1/")]
	[InlineData("esdb+discover://user:pass@127.0.0.1")]
	[InlineData("esdb+discover://user:pass@127.0.0.1/")]
	public void connection_string_with_no_key_value_pairs_specified_should_not_throw(string connectionString) =>
		EventStoreClientSettings.Create(connectionString);

	[Theory]
	[InlineData("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=12=34")]
	[InlineData("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts1234")]
	public void connection_string_with_invalid_key_value_pair_should_throw(string connectionString) =>
		Assert.Throws<InvalidKeyValuePairException>(() => EventStoreClientSettings.Create(connectionString));

	[Theory]
	[InlineData("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=1234&MaxDiscoverAttempts=10")]
	[InlineData("esdb://user:pass@127.0.0.1/?gossipTimeout=10&gossipTimeout=30")]
	public void connection_string_with_duplicate_key_should_throw(string connectionString) =>
		Assert.Throws<DuplicateKeyException>(() => EventStoreClientSettings.Create(connectionString));

	[Theory]
	[InlineData("esdb://user:pass@127.0.0.1/?unknown=1234")]
	[InlineData("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=1234&hello=test")]
	[InlineData("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=abcd")]
	[InlineData("esdb://user:pass@127.0.0.1/?discoveryInterval=abcd")]
	[InlineData("esdb://user:pass@127.0.0.1/?gossipTimeout=defg")]
	[InlineData("esdb://user:pass@127.0.0.1/?tlsVerifyCert=truee")]
	[InlineData("esdb://user:pass@127.0.0.1/?nodePreference=blabla")]
	[InlineData("esdb://user:pass@127.0.0.1/?keepAliveInterval=-2")]
	[InlineData("esdb://user:pass@127.0.0.1/?keepAliveTimeout=-2")]
	public void connection_string_with_invalid_settings_should_throw(string connectionString) =>
		Assert.Throws<InvalidSettingException>(() => EventStoreClientSettings.Create(connectionString));

	[Fact]
	public void with_default_settings() {
		var settings = EventStoreClientSettings.Create("esdb://hostname:4321/");

		Assert.Null(settings.ConnectionName);
		Assert.Equal(
			EventStoreClientConnectivitySettings.Default.ResolvedAddressOrDefault.Scheme,
			settings.ConnectivitySettings.ResolvedAddressOrDefault.Scheme
		);

		Assert.Equal(
			EventStoreClientConnectivitySettings.Default.DiscoveryInterval.TotalMilliseconds,
			settings.ConnectivitySettings.DiscoveryInterval.TotalMilliseconds
		);

		Assert.Null(EventStoreClientConnectivitySettings.Default.DnsGossipSeeds);
		Assert.Empty(EventStoreClientConnectivitySettings.Default.GossipSeeds);
		Assert.Equal(
			EventStoreClientConnectivitySettings.Default.GossipTimeout.TotalMilliseconds,
			settings.ConnectivitySettings.GossipTimeout.TotalMilliseconds
		);

		Assert.Null(EventStoreClientConnectivitySettings.Default.IpGossipSeeds);
		Assert.Equal(
			EventStoreClientConnectivitySettings.Default.MaxDiscoverAttempts,
			settings.ConnectivitySettings.MaxDiscoverAttempts
		);

		Assert.Equal(
			EventStoreClientConnectivitySettings.Default.NodePreference,
			settings.ConnectivitySettings.NodePreference
		);

		Assert.Equal(
			EventStoreClientConnectivitySettings.Default.Insecure,
			settings.ConnectivitySettings.Insecure
		);

		Assert.Equal(TimeSpan.FromSeconds(10), settings.DefaultDeadline);
		Assert.Equal(
			EventStoreClientOperationOptions.Default.ThrowOnAppendFailure,
			settings.OperationOptions.ThrowOnAppendFailure
		);

		Assert.Equal(
			EventStoreClientConnectivitySettings.Default.KeepAliveInterval,
			settings.ConnectivitySettings.KeepAliveInterval
		);

		Assert.Equal(
			EventStoreClientConnectivitySettings.Default.KeepAliveTimeout,
			settings.ConnectivitySettings.KeepAliveTimeout
		);
	}

	[Theory]
	[InlineData("esdb://localhost", true)]
	[InlineData("esdb://localhost/?tls=false", false)]
	[InlineData("esdb://localhost/?tls=true", true)]
	[InlineData("esdb://localhost1,localhost2,localhost3", true)]
	[InlineData("esdb://localhost1,localhost2,localhost3/?tls=false", false)]
	[InlineData("esdb://localhost1,localhost2,localhost3/?tls=true", true)]
	public void use_tls(string connectionString, bool expectedUseTls) {
		var result         = EventStoreClientSettings.Create(connectionString);
		var expectedScheme = expectedUseTls ? "https" : "http";
		Assert.NotEqual(expectedUseTls, result.ConnectivitySettings.Insecure);
		Assert.Equal(expectedScheme, result.ConnectivitySettings.ResolvedAddressOrDefault.Scheme);
	}

	[Theory]
	[InlineData("esdb://localhost", null, true)]
	[InlineData("esdb://localhost", true, false)]
	[InlineData("esdb://localhost", false, true)]
	[InlineData("esdb://localhost/?tls=true", null, true)]
	[InlineData("esdb://localhost/?tls=true", true, false)]
	[InlineData("esdb://localhost/?tls=true", false, true)]
	[InlineData("esdb://localhost/?tls=false", null, false)]
	[InlineData("esdb://localhost/?tls=false", true, false)]
	[InlineData("esdb://localhost/?tls=false", false, true)]
	[InlineData("esdb://localhost1,localhost2,localhost3", null, true)]
	[InlineData("esdb://localhost1,localhost2,localhost3", true, true)]
	[InlineData("esdb://localhost1,localhost2,localhost3", false, true)]
	[InlineData("esdb://localhost1,localhost2,localhost3/?tls=true", null, true)]
	[InlineData("esdb://localhost1,localhost2,localhost3/?tls=true", true, true)]
	[InlineData("esdb://localhost1,localhost2,localhost3/?tls=true", false, true)]
	[InlineData("esdb://localhost1,localhost2,localhost3/?tls=false", null, false)]
	[InlineData("esdb://localhost1,localhost2,localhost3/?tls=false", true, false)]
	[InlineData("esdb://localhost1,localhost2,localhost3/?tls=false", false, false)]
	public void allow_tls_override_for_single_node(string connectionString, bool? insecureOverride, bool expectedUseTls) {
		var result   = EventStoreClientSettings.Create(connectionString);
		var settings = result.ConnectivitySettings;

		if (insecureOverride.HasValue)
			settings.Address = new UriBuilder {
				Scheme = insecureOverride.Value ? "hTTp" : "HttpS"
			}.Uri;

		var expectedScheme = expectedUseTls ? "https" : "http";
		Assert.Equal(expectedUseTls, !settings.Insecure);
		Assert.Equal(expectedScheme, result.ConnectivitySettings.ResolvedAddressOrDefault.Scheme);
	}

	[Theory]
	[InlineData("esdb://localhost:1234", "localhost", 1234)]
	[InlineData("esdb://localhost:1234,localhost:4567", null, null)]
	[InlineData("esdb+discover://localhost:1234", null, null)]
	[InlineData("esdb+discover://localhost:1234,localhost:4567", null, null)]
	public void connection_string_with_custom_ports(string connectionString, string? expectedHost, int? expectedPort) {
		var result               = EventStoreClientSettings.Create(connectionString);
		var connectivitySettings = result.ConnectivitySettings;

		Assert.Equal(expectedHost, connectivitySettings.Address?.Host);
		Assert.Equal(expectedPort, connectivitySettings.Address?.Port);
	}

	static string GetConnectionString(
		EventStoreClientSettings settings,
		Func<string, string>? getKey = default
	) =>
		$"{GetScheme(settings)}{GetAuthority(settings)}?{GetKeyValuePairs(settings, getKey)}";

	static string GetScheme(EventStoreClientSettings settings) =>
		settings.ConnectivitySettings.IsSingleNode
			? "esdb://"
			: "esdb+discover://";

	static string GetAuthority(EventStoreClientSettings settings) =>
		settings.ConnectivitySettings.IsSingleNode
			? $"{settings.ConnectivitySettings.ResolvedAddressOrDefault.Host}:{settings.ConnectivitySettings.ResolvedAddressOrDefault.Port}"
			: string.Join(
				",",
				settings.ConnectivitySettings.GossipSeeds.Select(x => $"{x.GetHost()}:{x.GetPort()}")
			);

	static string GetKeyValuePairs(
		EventStoreClientSettings settings,
		Func<string, string>? getKey = default
	) {
		var pairs = new Dictionary<string, string?> {
			["tls"]                 = (!settings.ConnectivitySettings.Insecure).ToString(),
			["connectionName"]      = settings.ConnectionName,
			["maxDiscoverAttempts"] = settings.ConnectivitySettings.MaxDiscoverAttempts.ToString(),
			["discoveryInterval"]   = settings.ConnectivitySettings.DiscoveryInterval.TotalMilliseconds.ToString(),
			["gossipTimeout"]       = settings.ConnectivitySettings.GossipTimeout.TotalMilliseconds.ToString(),
			["nodePreference"]      = settings.ConnectivitySettings.NodePreference.ToString(),
			["keepAliveInterval"]   = settings.ConnectivitySettings.KeepAliveInterval.TotalMilliseconds.ToString(),
			["keepAliveTimeout"]    = settings.ConnectivitySettings.KeepAliveTimeout.TotalMilliseconds.ToString()
		};

		if (settings.DefaultDeadline.HasValue)
			pairs.Add(
				"defaultDeadline",
				settings.DefaultDeadline.Value.TotalMilliseconds.ToString()
			);

		if (settings.CreateHttpMessageHandler != null) {
			using var handler = settings.CreateHttpMessageHandler.Invoke();
#if NET
			if (handler is SocketsHttpHandler socketsHttpHandler &&
			    socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback != null)
				pairs.Add("tlsVerifyCert", "false");
		}
#else
			if (handler is WinHttpHandler winHttpHandler &&
			    winHttpHandler.ServerCertificateValidationCallback != null) {
				pairs.Add("tlsVerifyCert", "false");
			}
		}
#endif

		return string.Join("&", pairs.Select(pair => $"{getKey?.Invoke(pair.Key) ?? pair.Key}={pair.Value}"));
	}

	class EventStoreClientSettingsEqualityComparer : IEqualityComparer<EventStoreClientSettings> {
		public static readonly EventStoreClientSettingsEqualityComparer Instance = new();

		public bool Equals(EventStoreClientSettings? x, EventStoreClientSettings? y) {
			if (ReferenceEquals(x, y))
				return true;

			if (ReferenceEquals(x, null))
				return false;

			if (ReferenceEquals(y, null))
				return false;

			if (x.GetType() != y.GetType())
				return false;

			return x.ConnectionName == y.ConnectionName &&
			       EventStoreClientConnectivitySettingsEqualityComparer.Instance.Equals(
				       x.ConnectivitySettings,
				       y.ConnectivitySettings
			       ) &&
			       EventStoreClientOperationOptionsEqualityComparer.Instance.Equals(
				       x.OperationOptions,
				       y.OperationOptions
			       ) &&
			       Equals(x.DefaultCredentials?.ToString(), y.DefaultCredentials?.ToString());
		}

		public int GetHashCode(EventStoreClientSettings obj) =>
			HashCode.Hash
				.Combine(obj.ConnectionName)
				.Combine(EventStoreClientConnectivitySettingsEqualityComparer.Instance.GetHashCode(obj.ConnectivitySettings))
				.Combine(EventStoreClientOperationOptionsEqualityComparer.Instance.GetHashCode(obj.OperationOptions));
	}

	class EventStoreClientConnectivitySettingsEqualityComparer
		: IEqualityComparer<EventStoreClientConnectivitySettings> {
		public static readonly EventStoreClientConnectivitySettingsEqualityComparer Instance = new();

		public bool Equals(EventStoreClientConnectivitySettings? x, EventStoreClientConnectivitySettings? y) {
			if (ReferenceEquals(x, y))
				return true;

			if (ReferenceEquals(x, null))
				return false;

			if (ReferenceEquals(y, null))
				return false;

			if (x.GetType() != y.GetType())
				return false;

			return (!x.IsSingleNode || x.ResolvedAddressOrDefault.Equals(y.Address)) &&
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
					.Combine(obj.ResolvedAddressOrDefault.GetHashCode())
					.Combine(obj.MaxDiscoverAttempts)
					.Combine(obj.GossipTimeout)
					.Combine(obj.DiscoveryInterval)
					.Combine(obj.NodePreference)
					.Combine(obj.KeepAliveInterval)
					.Combine(obj.KeepAliveTimeout)
					.Combine(obj.Insecure),
				(hashcode, endpoint) => hashcode.Combine(endpoint.GetHashCode())
			);
	}

	class EventStoreClientOperationOptionsEqualityComparer
		: IEqualityComparer<EventStoreClientOperationOptions> {
		public static readonly EventStoreClientOperationOptionsEqualityComparer Instance = new();

		public bool Equals(EventStoreClientOperationOptions? x, EventStoreClientOperationOptions? y) {
			if (ReferenceEquals(x, y))
				return true;

			if (ReferenceEquals(x, null))
				return false;

			if (ReferenceEquals(y, null))
				return false;

			return x.GetType() == y.GetType();
		}

		public int GetHashCode(EventStoreClientOperationOptions obj) =>
			System.HashCode.Combine(obj.ThrowOnAppendFailure);
	}
}
