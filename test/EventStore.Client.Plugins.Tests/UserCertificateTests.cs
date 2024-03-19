namespace EventStore.Client.Plugins.Tests {
	[Trait("Category", "Certificates")]
	public class UserCertificateTests(ITestOutputHelper output, EventStoreFixture fixture)
		: EventStoreTests<EventStoreFixture>(output, fixture) {
		[Fact]
		public async Task user_credentials_takes_precedence_over_user_certificates() {
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
				Enumerable.Empty<EventData>(),
				userCredentials: TestCredentials.TestBadUser
			).ShouldThrowAsync<NotAuthenticatedException>();
		}

		[Fact]
		public Task does_not_accept_certificates_with_invalid_path() {
			var certPath    = Path.Combine("invalid.crt");
			var certKeyPath = Path.Combine("invalid.key");

			var connectionString =
				$"esdb://admin:changeit@localhost:2113/?tls=true&tlsVerifyCert=true&certPath={certPath}&certKeyPath={certKeyPath}";

			Assert.Throws<InvalidSettingException>(() => EventStoreClientSettings.Create(connectionString));

			return Task.CompletedTask;
		}

		[Fact]
		public async Task append_should_be_successful_with_user_certificates() {
			var certPath    = Path.Combine(Environment.CurrentDirectory, "certs", "user-admin", "user-admin.crt");
			var certKeyPath = Path.Combine(Environment.CurrentDirectory, "certs", "user-admin", "user-admin.key");

			Assert.True(File.Exists(certPath));
			Assert.True(File.Exists(certKeyPath));

			var connectionString =
				$"esdb://localhost:2113/?tls=true&tlsVerifyCert=true&certPath={certPath}&certKeyPath={certKeyPath}";

			Fixture.Log.Information("connectionString: {connectionString}", connectionString);

			var stream = Fixture.GetStreamName();

			var settings = EventStoreClientSettings.Create(connectionString);

			var client = new EventStoreClient(settings);

			var result = await client.AppendToStreamAsync(
				stream,
				StreamState.Any,
				Enumerable.Empty<EventData>()
			);

			Assert.NotNull(result);
		}

		[Fact]
        public async Task append_with_correct_user_certificate_but_read_with_bad_user_certificate()
        {
            var connectionString = "esdb://admin:changeit@localhost:2113/?tls=true&tlsVerifyCert=true";

            var stream = Fixture.GetStreamName();

            var settings = EventStoreClientSettings.Create(connectionString);

            var client = new EventStoreClient(settings);

            var appendResult = await client.AppendToStreamAsync(
	            stream,
	            StreamState.Any,
	            Enumerable.Empty<EventData>(),
	            userCredentials: TestCredentials.UserAdminCertificate
            );

            Assert.NotNull(appendResult);

            await Fixture.Streams
	            .ReadStreamAsync(
		            Direction.Forwards,
		            stream,
		            StreamPosition.Start,
		            userCredentials: TestCredentials.BadUserCertificate
	            )
	            .ShouldThrowAsync<NotAuthenticatedException>();
        }

		[Fact]
		public async Task overriding_user_certificate_with_basic_authentication_should_work() {
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
	            userCredentials: new UserCredentials("admin", "changeit")
            );

            Assert.NotNull(appendResult);

            var readResult = await Fixture.Streams
	            .ReadStreamAsync(
		            Direction.Forwards,
		            stream,
		            StreamPosition.Start
	            ).CountAsync();
            readResult.ShouldBe(5);
		}
	}
}
