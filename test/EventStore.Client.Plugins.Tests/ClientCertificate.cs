namespace EventStore.Client.Plugins.Tests;

[Trait("Category", "Target:Plugins")]
[Trait("Category", "Type:UserCertificate")]
public class ClientCertificate(ITestOutputHelper output, EventStoreFixture fixture)
	: EventStoreTests<EventStoreFixture>(output, fixture) {
	public static IEnumerable<object[]> TlsCertPaths =>
		new List<object[]> {
			new object[] { Path.Combine("certs", "ca", "ca.crt") },
			new object[] { Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certs", "ca", "ca.crt") }
		};

	public static IEnumerable<object[]> AdminClientCertPaths =>
		new List<object[]> {
			new object[] {
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certs", "user-admin", "user-admin.crt"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certs", "user-admin", "user-admin.key")
			},
			new object[] {
				Path.Combine("certs", "user-admin", "user-admin.crt"),
				Path.Combine("certs", "user-admin", "user-admin.key")
			}
		};

	public static IEnumerable<object[]> BadClientCertPaths =>
		new List<object[]> {
			new object[] {
				Path.Combine("certs", "user-invalid", "user-invalid.crt"),
				Path.Combine("certs", "user-invalid", "user-invalid.key")
			},
			new object[] {
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certs", "user-invalid", "user-invalid.crt"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certs", "user-invalid", "user-invalid.key")
			}
		};

	[Theory]
	[MemberData(nameof(TlsCertPaths))]
	async Task append_with_different_tls_cert_path(string certificateFilePath) {
		await AppendWithCertificate($"esdb://admin:changeit@localhost:2113/?tls=true&tlsVerifyCert=true&tlsCAFile={certificateFilePath}");
	}

	[Theory]
	[MemberData(nameof(AdminClientCertPaths))]
	async Task append_with_admin_client_certificate(string certPath, string certKeyPath) {
		Fixture.Log.Information("CertPath: {certPath}, CertKeyPath: {certKeyPath}", certPath, certKeyPath);
		await AppendWithCertificate($"esdb://localhost:2113/?tls=true&tlsVerifyCert=true&certPath={certPath}&certKeyPath={certKeyPath}");
	}

	[Theory]
	[MemberData(nameof(BadClientCertPaths))]
	async Task append_with_bad_client_certificate(string certPath, string certKeyPath) {
		await AssertAppendFailsWithCertificate(
			$"esdb://localhost:2113/?tls=true&tlsVerifyCert=true&certPath={certPath}&certKeyPath={certKeyPath}",
			typeof(NotAuthenticatedException)
		);
	}

	[Theory]
	[MemberData(nameof(BadClientCertPaths))]
	async Task user_credentials_takes_precedence_over_client_certificates(string certPath, string certKeyPath) {
		await AppendWithCertificate($"esdb://admin:changeit@localhost:2113/?tls=true&tlsVerifyCert=true&certPath={certPath}&certKeyPath={certKeyPath}");
	}

	async Task AppendWithCertificate(string connectionString) {
		var settings = EventStoreClientSettings.Create(connectionString);

		await using var client = new EventStoreClient(settings);

		var appendResult = await client.AppendToStreamAsync(
			Fixture.GetStreamName(),
			StreamState.Any,
			Fixture.CreateTestEvents(1)
		);

		appendResult.ShouldNotBeNull();
	}

	async Task AssertAppendFailsWithCertificate(string connectionString, Type expectedExceptionType) {
		var settings = EventStoreClientSettings.Create(connectionString);

		await using var client = new EventStoreClient(settings);

		await client.AppendToStreamAsync(
			Fixture.GetStreamName(),
			StreamState.Any,
			Fixture.CreateTestEvents(1)
		).ShouldThrowAsync(expectedExceptionType);
	}
}
