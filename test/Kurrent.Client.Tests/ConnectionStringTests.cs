using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using AutoFixture;
using EventStore.Client;
using HashCode = EventStore.Client.HashCode;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:Misc")]
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
			var settings = new KurrentClientSettings {
				ConnectionName       = fixture.Create<string>(),
				ConnectivitySettings = fixture.Create<KurrentClientConnectivitySettings>(),
				OperationOptions     = fixture.Create<KurrentClientOperationOptions>()
			};

			settings.ConnectivitySettings.Address =
				new UriBuilder(KurrentClientConnectivitySettings.Default.ResolvedAddressOrDefault) {
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

			var ipGossipSettings = new KurrentClientSettings {
				ConnectionName       = fixture.Create<string>(),
				ConnectivitySettings = fixture.Create<KurrentClientConnectivitySettings>(),
				OperationOptions     = fixture.Create<KurrentClientOperationOptions>()
			};

			ipGossipSettings.ConnectivitySettings.Address =
				new UriBuilder(KurrentClientConnectivitySettings.Default.ResolvedAddressOrDefault) {
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

			var singleNodeSettings = new KurrentClientSettings {
				ConnectionName       = fixture.Create<string>(),
				ConnectivitySettings = fixture.Create<KurrentClientConnectivitySettings>(),
				OperationOptions     = fixture.Create<KurrentClientOperationOptions>()
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
	public void valid_connection_string(string connectionString, KurrentClientSettings expected) {
		var result = KurrentClientSettings.Create(connectionString);

		Assert.Equal(expected, result, KurrentClientSettingsEqualityComparer.Instance);
	}

	[Theory]
	[MemberData(nameof(ValidCases))]
	public void valid_connection_string_with_empty_path(string connectionString, KurrentClientSettings expected) {
		var result = KurrentClientSettings.Create(connectionString.Replace("?", "/?"));

		Assert.Equal(expected, result, KurrentClientSettingsEqualityComparer.Instance);
	}

#if !GRPC_CORE
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void tls_verify_cert(bool tlsVerifyCert) {
		var       connectionString = $"esdb://localhost:2113/?tlsVerifyCert={tlsVerifyCert}";
		var       result           = KurrentClientSettings.Create(connectionString);
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
		} else {
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
		Assert.Throws<InvalidClientCertificateException>(
			() => KurrentClientSettings.Create($"esdb://admin:changeit@localhost:2113/?tls=true&tlsVerifyCert=true&tlsCAFile={clientCertificatePath}")
		);
	}

	public static IEnumerable<object?[]> InvalidClientCertificates() {
		var invalidPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "path", "not", "found");
		var validPath   = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certs", "ca", "ca.crt");
		yield return [invalidPath, invalidPath];
		yield return [validPath, invalidPath];
	}

	[Theory]
	[MemberData(nameof(InvalidClientCertificates))]
	public void connection_string_with_invalid_client_certificate_should_throw(string userCertFile, string userKeyFile) {
		Assert.Throws<InvalidClientCertificateException>(
			() => KurrentClientSettings.Create(
				$"esdb://admin:changeit@localhost:2113/?tls=true&tlsVerifyCert=true&userCertFile={userCertFile}&userKeyFile={userKeyFile}"
			)
		);
	}

	[RetryFact]
	public void infinite_grpc_timeouts() {
		var result = KurrentClientSettings.Create("esdb://localhost:2113?keepAliveInterval=-1&keepAliveTimeout=-1");

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

	[RetryFact]
	public void connection_string_with_no_schema() => Assert.Throws<NoSchemeException>(() => KurrentClientSettings.Create(":so/mething/random"));

	[Theory]
	[InlineData("esdbwrong://")]
	[InlineData("wrong://")]
	[InlineData("badesdb://")]
	public void connection_string_with_invalid_scheme_should_throw(string connectionString) =>
		Assert.Throws<InvalidSchemeException>(() => KurrentClientSettings.Create(connectionString));

	[Theory]
	[InlineData("esdb://userpass@127.0.0.1/")]
	[InlineData("esdb://user:pa:ss@127.0.0.1/")]
	[InlineData("esdb://us:er:pa:ss@127.0.0.1/")]
	public void connection_string_with_invalid_userinfo_should_throw(string connectionString) =>
		Assert.Throws<InvalidUserCredentialsException>(() => KurrentClientSettings.Create(connectionString));

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
		Assert.Throws<InvalidHostException>(() => KurrentClientSettings.Create(connectionString));

	[Theory]
	[InlineData("esdb://user:pass@127.0.0.1/test")]
	[InlineData("esdb://user:pass@127.0.0.1/maxDiscoverAttempts=10")]
	[InlineData("esdb://user:pass@127.0.0.1/hello?maxDiscoverAttempts=10")]
	public void connection_string_with_non_empty_path_should_throw(string connectionString) =>
		Assert.Throws<ConnectionStringParseException>(() => KurrentClientSettings.Create(connectionString));

	[Theory]
	[InlineData("esdb://user:pass@127.0.0.1")]
	[InlineData("esdb://user:pass@127.0.0.1/")]
	[InlineData("esdb+discover://user:pass@127.0.0.1")]
	[InlineData("esdb+discover://user:pass@127.0.0.1/")]
	public void connection_string_with_no_key_value_pairs_specified_should_not_throw(string connectionString) =>
		KurrentClientSettings.Create(connectionString);

	[Theory]
	[InlineData("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=12=34")]
	[InlineData("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts1234")]
	public void connection_string_with_invalid_key_value_pair_should_throw(string connectionString) =>
		Assert.Throws<InvalidKeyValuePairException>(() => KurrentClientSettings.Create(connectionString));

	[Theory]
	[InlineData("esdb://user:pass@127.0.0.1/?maxDiscoverAttempts=1234&MaxDiscoverAttempts=10")]
	[InlineData("esdb://user:pass@127.0.0.1/?gossipTimeout=10&gossipTimeout=30")]
	public void connection_string_with_duplicate_key_should_throw(string connectionString) =>
		Assert.Throws<DuplicateKeyException>(() => KurrentClientSettings.Create(connectionString));

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
		Assert.Throws<InvalidSettingException>(() => KurrentClientSettings.Create(connectionString));

	[RetryFact]
	public void with_default_settings() {
		var settings = KurrentClientSettings.Create("esdb://hostname:4321/");

		Assert.Null(settings.ConnectionName);
		Assert.Equal(
			KurrentClientConnectivitySettings.Default.ResolvedAddressOrDefault.Scheme,
			settings.ConnectivitySettings.ResolvedAddressOrDefault.Scheme
		);

		Assert.Equal(
			KurrentClientConnectivitySettings.Default.DiscoveryInterval.TotalMilliseconds,
			settings.ConnectivitySettings.DiscoveryInterval.TotalMilliseconds
		);

		Assert.Null(KurrentClientConnectivitySettings.Default.DnsGossipSeeds);
		Assert.Empty(KurrentClientConnectivitySettings.Default.GossipSeeds);
		Assert.Equal(
			KurrentClientConnectivitySettings.Default.GossipTimeout.TotalMilliseconds,
			settings.ConnectivitySettings.GossipTimeout.TotalMilliseconds
		);

		Assert.Null(KurrentClientConnectivitySettings.Default.IpGossipSeeds);
		Assert.Equal(
			KurrentClientConnectivitySettings.Default.MaxDiscoverAttempts,
			settings.ConnectivitySettings.MaxDiscoverAttempts
		);

		Assert.Equal(
			KurrentClientConnectivitySettings.Default.NodePreference,
			settings.ConnectivitySettings.NodePreference
		);

		Assert.Equal(
			KurrentClientConnectivitySettings.Default.Insecure,
			settings.ConnectivitySettings.Insecure
		);

		Assert.Equal(TimeSpan.FromSeconds(10), settings.DefaultDeadline);
		Assert.Equal(
			KurrentClientOperationOptions.Default.ThrowOnAppendFailure,
			settings.OperationOptions.ThrowOnAppendFailure
		);

		Assert.Equal(
			KurrentClientConnectivitySettings.Default.KeepAliveInterval,
			settings.ConnectivitySettings.KeepAliveInterval
		);

		Assert.Equal(
			KurrentClientConnectivitySettings.Default.KeepAliveTimeout,
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
		var result         = KurrentClientSettings.Create(connectionString);
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
		var result   = KurrentClientSettings.Create(connectionString);
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
		var result               = KurrentClientSettings.Create(connectionString);
		var connectivitySettings = result.ConnectivitySettings;

		Assert.Equal(expectedHost, connectivitySettings.Address?.Host);
		Assert.Equal(expectedPort, connectivitySettings.Address?.Port);
	}

	static string GetConnectionString(
		KurrentClientSettings settings,
		Func<string, string>? getKey = default
	) =>
		$"{GetScheme(settings)}{GetAuthority(settings)}?{GetKeyValuePairs(settings, getKey)}";

	static string GetScheme(KurrentClientSettings settings) =>
		settings.ConnectivitySettings.IsSingleNode
			? "esdb://"
			: "esdb+discover://";

	static string GetAuthority(KurrentClientSettings settings) =>
		settings.ConnectivitySettings.IsSingleNode
			? $"{settings.ConnectivitySettings.ResolvedAddressOrDefault.Host}:{settings.ConnectivitySettings.ResolvedAddressOrDefault.Port}"
			: string.Join(
				",",
				settings.ConnectivitySettings.GossipSeeds.Select(x => $"{x.GetHost()}:{x.GetPort()}")
			);

	static string GetKeyValuePairs(
		KurrentClientSettings settings,
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

	class KurrentClientSettingsEqualityComparer : IEqualityComparer<KurrentClientSettings> {
		public static readonly KurrentClientSettingsEqualityComparer Instance = new();

		public bool Equals(KurrentClientSettings? x, KurrentClientSettings? y) {
			if (ReferenceEquals(x, y))
				return true;

			if (ReferenceEquals(x, null))
				return false;

			if (ReferenceEquals(y, null))
				return false;

			if (x.GetType() != y.GetType())
				return false;

			return x.ConnectionName == y.ConnectionName &&
			       KurrentClientConnectivitySettingsEqualityComparer.Instance.Equals(
				       x.ConnectivitySettings,
				       y.ConnectivitySettings
			       ) &&
			       KurrentClientOperationOptionsEqualityComparer.Instance.Equals(
				       x.OperationOptions,
				       y.OperationOptions
			       ) &&
			       Equals(x.DefaultCredentials?.ToString(), y.DefaultCredentials?.ToString());
		}

		public int GetHashCode(KurrentClientSettings obj) =>
			HashCode.Hash
				.Combine(obj.ConnectionName)
				.Combine(KurrentClientConnectivitySettingsEqualityComparer.Instance.GetHashCode(obj.ConnectivitySettings))
				.Combine(KurrentClientOperationOptionsEqualityComparer.Instance.GetHashCode(obj.OperationOptions));
	}

	class KurrentClientConnectivitySettingsEqualityComparer
		: IEqualityComparer<KurrentClientConnectivitySettings> {
		public static readonly KurrentClientConnectivitySettingsEqualityComparer Instance = new();

		public bool Equals(KurrentClientConnectivitySettings? x, KurrentClientConnectivitySettings? y) {
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

		public int GetHashCode(KurrentClientConnectivitySettings obj) =>
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

	class KurrentClientOperationOptionsEqualityComparer
		: IEqualityComparer<KurrentClientOperationOptions> {
		public static readonly KurrentClientOperationOptionsEqualityComparer Instance = new();

		public bool Equals(KurrentClientOperationOptions? x, KurrentClientOperationOptions? y) {
			if (ReferenceEquals(x, y))
				return true;

			if (ReferenceEquals(x, null))
				return false;

			if (ReferenceEquals(y, null))
				return false;

			return x.GetType() == y.GetType();
		}

		public int GetHashCode(KurrentClientOperationOptions obj) =>
			System.HashCode.Combine(obj.ThrowOnAppendFailure);
	}
}
