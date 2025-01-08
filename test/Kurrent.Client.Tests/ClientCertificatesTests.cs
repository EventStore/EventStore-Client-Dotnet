using EventStore.Client;
using Humanizer;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:Misc")]
[Trait("Category", "Target:Plugins")]
[Trait("Category", "Type:UserCertificate")]
public class ClientCertificateTests(ITestOutputHelper output, KurrentPermanentFixture fixture)
	: KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[SupportsPlugins.Theory(EventStoreRepository.Commercial, "This server version does not support plugins"), BadClientCertificatesTestCases]
	async Task bad_certificates_combinations_should_return_authentication_error(string userCertFile, string userKeyFile, string tlsCaFile) {
		var stream           = Fixture.GetStreamName();
		var seedEvents       = Fixture.CreateTestEvents();
		var connectionString = $"esdb://localhost:2113/?tls=true&userCertFile={userCertFile}&userKeyFile={userKeyFile}&tlsCaFile={tlsCaFile}";

		var settings = KurrentClientSettings.Create(connectionString);
		settings.ConnectivitySettings.TlsVerifyCert.ShouldBeTrue();

		await using var client = new KurrentClient(settings);

		await client.AppendToStreamAsync(stream, StreamState.NoStream, seedEvents).ShouldThrowAsync<NotAuthenticatedException>();
	}

	[SupportsPlugins.Theory(EventStoreRepository.Commercial, "This server version does not support plugins"), ValidClientCertificatesTestCases]
	async Task valid_certificates_combinations_should_write_to_stream(string userCertFile, string userKeyFile, string tlsCaFile) {
		var stream           = Fixture.GetStreamName();
		var seedEvents       = Fixture.CreateTestEvents();
		var connectionString = $"esdb://localhost:2113/?userCertFile={userCertFile}&userKeyFile={userKeyFile}&tlsCaFile={tlsCaFile}";

		var settings = KurrentClientSettings.Create(connectionString);
		settings.ConnectivitySettings.TlsVerifyCert.ShouldBeTrue();

		await using var client = new KurrentClient(settings);

		var result = await client.AppendToStreamAsync(stream, StreamState.NoStream, seedEvents);
		result.ShouldNotBeNull();
	}

	[SupportsPlugins.Theory(EventStoreRepository.Commercial, "This server version does not support plugins"), BadClientCertificatesTestCases]
	async Task basic_authentication_should_take_precedence(string userCertFile, string userKeyFile, string tlsCaFile) {
		var stream           = Fixture.GetStreamName();
		var seedEvents       = Fixture.CreateTestEvents();
		var connectionString = $"esdb://admin:changeit@localhost:2113/?userCertFile={userCertFile}&userKeyFile={userKeyFile}&tlsCaFile={tlsCaFile}";

		var settings = KurrentClientSettings.Create(connectionString);
		settings.ConnectivitySettings.TlsVerifyCert.ShouldBeTrue();

		await using var client = new KurrentClient(settings);

		var result = await client.AppendToStreamAsync(stream, StreamState.NoStream, seedEvents);
		result.ShouldNotBeNull();
	}

	class BadClientCertificatesTestCases : TestCaseGenerator<BadClientCertificatesTestCases> {
		protected override IEnumerable<object[]> Data() {
			yield return [Certificates.Invalid.CertAbsolute, Certificates.Invalid.KeyAbsolute, Certificates.TlsCa.Absolute];
			yield return [Certificates.Invalid.CertRelative, Certificates.Invalid.KeyRelative, Certificates.TlsCa.Absolute];
			yield return [Certificates.Invalid.CertAbsolute, Certificates.Invalid.KeyAbsolute, Certificates.TlsCa.Relative];
			yield return [Certificates.Invalid.CertRelative, Certificates.Invalid.KeyRelative, Certificates.TlsCa.Relative];
		}
	}

	class ValidClientCertificatesTestCases : TestCaseGenerator<ValidClientCertificatesTestCases> {
		protected override IEnumerable<object[]> Data() {
			yield return [Certificates.Admin.CertAbsolute, Certificates.Admin.KeyAbsolute, Certificates.TlsCa.Absolute];
			yield return [Certificates.Admin.CertRelative, Certificates.Admin.KeyRelative, Certificates.TlsCa.Absolute];
			yield return [Certificates.Admin.CertAbsolute, Certificates.Admin.KeyAbsolute, Certificates.TlsCa.Relative];
			yield return [Certificates.Admin.CertRelative, Certificates.Admin.KeyRelative, Certificates.TlsCa.Relative];
		}
	}
}

public enum EventStoreRepository {
	Commercial = 1
}

[PublicAPI]
public class SupportsPlugins {
	public class TheoryAttribute(EventStoreRepository repository, string skipMessage) : Xunit.TheoryAttribute {
		public override string? Skip {
			get => !GlobalEnvironment.DockerImage.Contains(repository.Humanize().ToLower()) ? skipMessage : null;
			set => throw new NotSupportedException();
		}
	}
}
