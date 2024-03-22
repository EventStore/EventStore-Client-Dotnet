namespace EventStore.Client.Plugins.Tests {
	[Trait("Category", "Certificates")]
	public class UserCertificateTests(ITestOutputHelper output, EventStoreFixture fixture)
		: EventStoreTests<EventStoreFixture>(output, fixture) {
		[Fact]
		public async Task user_credentials_takes_precedence_over_user_certificate_on_a_call() {
			var certPath    = Path.Combine(Environment.CurrentDirectory, "certs", "user-admin", "user-admin.crt");
			var certKeyPath = Path.Combine(Environment.CurrentDirectory, "certs", "user-admin", "user-admin.key");

			var connectionString =
				$"esdb://localhost:2113/?tls=true&tlsVerifyCert=true&certPath={certPath}&certKeyPath={certKeyPath}";

			var stream = Fixture.GetStreamName();

			var settings = EventStoreClientSettings.Create(connectionString);

			var client = new EventStoreClient(settings);

			var appendResult = await client.AppendToStreamAsync(
				stream,
				StreamState.Any,
				Fixture.CreateTestEvents(5),
				userCredentials: new UserCredentials("admin", "changeit"),
				userCertificate: TestCertificate.UserAdminCertificate
			);

			Assert.NotNull(appendResult);
		}

		[Fact]
		public async Task invalid_user_credentials_takes_precedence_over_admin_cert() {
			var certPath    = Path.Combine(Environment.CurrentDirectory, "certs", "user-admin", "user-admin.crt");
			var certKeyPath = Path.Combine(Environment.CurrentDirectory, "certs", "user-admin", "user-admin.key");

			var connectionString =
				$"esdb://localhost:2113/?tls=true&tlsVerifyCert=true&certPath={certPath}&certKeyPath={certKeyPath}";

			var stream = Fixture.GetStreamName();

			var settings = EventStoreClientSettings.Create(connectionString);

			var client = new EventStoreClient(settings);

			await client.AppendToStreamAsync(
				stream,
				StreamState.Any,
				Fixture.CreateTestEvents(5),
				userCredentials: TestCredentials.TestBadUser,
				userCertificate: TestCertificate.UserAdminCertificate
			).ShouldThrowAsync<NotAuthenticatedException>();
		}

		[Fact]
		public async Task valid_user_credentials_takes_precedence_over_invalid_user_cert_with_invalid_client() {
			var certPath    = Path.Combine(Environment.CurrentDirectory, "certs", "user-invalid", "user-invalid.crt");
			var certKeyPath = Path.Combine(Environment.CurrentDirectory, "certs", "user-invalid", "user-invalid.key");

			var connectionString =
				$"esdb://localhost:2113/?tls=true&tlsVerifyCert=true&certPath={certPath}&certKeyPath={certKeyPath}";

			var stream = Fixture.GetStreamName();

			var settings = EventStoreClientSettings.Create(connectionString);

			var client = new EventStoreClient(settings);

			await client.AppendToStreamAsync(
				stream,
				StreamState.Any,
				Fixture.CreateTestEvents(5),
				userCredentials: new UserCredentials("admin", "changeit"),
				userCertificate: TestCertificate.BadUserCertificate
			).ShouldThrowAsync<NotAuthenticatedException>();
		}

		[Fact]
		public async Task overriding_invalid_client_with_valid_user_credentials_throws_unauthenticated() {
			var certPath    = Path.Combine(Environment.CurrentDirectory, "certs", "user-invalid", "user-invalid.crt");
			var certKeyPath = Path.Combine(Environment.CurrentDirectory, "certs", "user-invalid", "user-invalid.key");

			var connectionString =
				$"esdb://localhost:2113/?tls=true&tlsVerifyCert=true&certPath={certPath}&certKeyPath={certKeyPath}";

			var stream = Fixture.GetStreamName();

			var settings = EventStoreClientSettings.Create(connectionString);

			var client = new EventStoreClient(settings);

			await client.AppendToStreamAsync(
				stream,
				StreamState.Any,
				Fixture.CreateTestEvents(5),
				userCredentials: new UserCredentials("admin", "changeit")
			).ShouldThrowAsync<NotAuthenticatedException>();
		}

		[Fact]
		public async Task override_call_with_invalid_user_certificate_should_throw_unauthenticated() {
			var certPath    = Path.Combine(Environment.CurrentDirectory, "certs", "user-admin", "user-admin.crt");
			var certKeyPath = Path.Combine(Environment.CurrentDirectory, "certs", "user-admin", "user-admin.key");

			var connectionString =
				$"esdb://localhost:2113/?tls=true&tlsVerifyCert=true&certPath={certPath}&certKeyPath={certKeyPath}";

			var stream = Fixture.GetStreamName();

			var settings = EventStoreClientSettings.Create(connectionString);

			var client = new EventStoreClient(settings);

			await client.AppendToStreamAsync(
				stream,
				StreamState.Any,
				Fixture.CreateTestEvents(5),
				userCertificate: TestCertificate.BadUserCertificate
			).ShouldThrowAsync<NotAuthenticatedException>();

			await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
				.ShouldThrowAsync<StreamNotFoundException>();
		}
	}
}
