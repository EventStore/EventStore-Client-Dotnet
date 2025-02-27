using EventStore.Client;
using Kurrent.Client.Tests.TestNode;
using Kurrent.Client.Tests;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:Security")]
public class StreamSecurityInheritanceTests(ITestOutputHelper output, StreamSecurityInheritanceTests.CustomFixture fixture)
	: KurrentTemporaryTests<StreamSecurityInheritanceTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task acl_inheritance_is_working_properly_on_user_streams() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("user-no-acl"));
		await Fixture.AppendStream("user-no-acl", TestCredentials.TestUser1);
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("user-no-acl", TestCredentials.TestUser2));

		await Fixture.AppendStream("user-no-acl", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("user-w-diff"));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("user-w-diff", TestCredentials.TestUser1));

		await Fixture.AppendStream("user-w-diff", TestCredentials.TestUser2);
		await Fixture.AppendStream("user-w-diff", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("user-w-multiple"));
		await Fixture.AppendStream("user-w-multiple", TestCredentials.TestUser1);
		await Fixture.AppendStream("user-w-multiple", TestCredentials.TestUser2);
		await Fixture.AppendStream("user-w-multiple", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("user-w-restricted"));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("user-w-restricted", TestCredentials.TestUser1));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("user-w-restricted", TestCredentials.TestUser2));

		await Fixture.AppendStream("user-w-restricted", TestCredentials.TestAdmin);

		await Fixture.AppendStream("user-w-all", TestCredentials.TestUser1);
		await Fixture.AppendStream("user-w-all", TestCredentials.TestUser2);
		await Fixture.AppendStream("user-w-all", TestCredentials.TestAdmin);

		await Fixture.ReadEvent("user-no-acl", TestCredentials.TestUser1);
		await Fixture.ReadEvent("user-no-acl", TestCredentials.TestUser2);
		await Fixture.ReadEvent("user-no-acl", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent("user-r-restricted"));
		await Fixture.AppendStream("user-r-restricted", TestCredentials.TestUser1);
		await Fixture.ReadEvent("user-r-restricted", TestCredentials.TestUser1);
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent("user-r-restricted", TestCredentials.TestUser2));

		await Fixture.ReadEvent("user-r-restricted", TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task acl_inheritance_is_working_properly_on_user_streams_when_not_authenticated() {
		await Fixture.AppendStream("user-w-all");

		// make sure the stream exists before trying to read it without authentication
		await Fixture.AppendStream("user-no-acl", TestCredentials.TestAdmin);
		await Fixture.ReadEvent("user-no-acl");
	}

	[RetryFact]
	public async Task acl_inheritance_is_working_properly_on_system_streams() {
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("$sys-no-acl"));
		await Fixture.AppendStream("$sys-no-acl", TestCredentials.TestUser1);
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("$sys-no-acl", TestCredentials.TestUser2));

		await Fixture.AppendStream("$sys-no-acl", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("$sys-w-diff"));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("$sys-w-diff", TestCredentials.TestUser1));

		await Fixture.AppendStream("$sys-w-diff", TestCredentials.TestUser2);
		await Fixture.AppendStream("$sys-w-diff", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("$sys-w-multiple"));
		await Fixture.AppendStream("$sys-w-multiple", TestCredentials.TestUser1);
		await Fixture.AppendStream("$sys-w-multiple", TestCredentials.TestUser2);
		await Fixture.AppendStream("$sys-w-multiple", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("$sys-w-restricted"));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("$sys-w-restricted", TestCredentials.TestUser1));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream("$sys-w-restricted", TestCredentials.TestUser2));

		await Fixture.AppendStream("$sys-w-restricted", TestCredentials.TestAdmin);

		await Fixture.AppendStream("$sys-w-all", TestCredentials.TestUser1);
		await Fixture.AppendStream("$sys-w-all", TestCredentials.TestUser2);
		await Fixture.AppendStream("$sys-w-all", TestCredentials.TestAdmin);

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent("$sys-no-acl"));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent("$sys-no-acl", TestCredentials.TestUser1));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent("$sys-no-acl", TestCredentials.TestUser2));

		await Fixture.ReadEvent("$sys-no-acl", TestCredentials.TestAdmin);
	}

	[AnonymousAccess.Fact]
	public async Task acl_inheritance_is_working_properly_on_system_streams_when_not_authenticated() => await Fixture.AppendStream("$sys-w-all");

	public class CustomFixture : SecurityFixture {
		protected override async Task When() {
			var settings = new SystemSettings(
				new(writeRole: "user1"),
				new(writeRole: "user1")
			);

			await Streams.SetSystemSettingsAsync(settings, userCredentials: TestCredentials.TestAdmin);

			await Streams.SetStreamMetadataAsync(
				"user-no-acl",
				StreamState.NoStream,
				new(),
				userCredentials: TestCredentials.TestAdmin
			);

			await Streams.SetStreamMetadataAsync(
				"user-w-diff",
				StreamState.NoStream,
				new(acl: new(writeRole: "user2")),
				userCredentials: TestCredentials.TestAdmin
			);

			await Streams.SetStreamMetadataAsync(
				"user-w-multiple",
				StreamState.NoStream,
				new(acl: new(writeRoles: new[] { "user1", "user2" })),
				userCredentials: TestCredentials.TestAdmin
			);

			await Streams.SetStreamMetadataAsync(
				"user-w-restricted",
				StreamState.NoStream,
				new(acl: new(writeRoles: Array.Empty<string>())),
				userCredentials: TestCredentials.TestAdmin
			);

			await Streams.SetStreamMetadataAsync(
				"user-w-all",
				StreamState.NoStream,
				new(acl: new(writeRole: SystemRoles.All)),
				userCredentials: TestCredentials.TestAdmin
			);

			await Streams.SetStreamMetadataAsync(
				"user-r-restricted",
				StreamState.NoStream,
				new(acl: new("user1")),
				userCredentials: TestCredentials.TestAdmin
			);

			await Streams.SetStreamMetadataAsync(
				"$sys-no-acl",
				StreamState.NoStream,
				new(),
				userCredentials: TestCredentials.TestAdmin
			);

			await Streams.SetStreamMetadataAsync(
				"$sys-w-diff",
				StreamState.NoStream,
				new(acl: new(writeRole: "user2")),
				userCredentials: TestCredentials.TestAdmin
			);

			await Streams.SetStreamMetadataAsync(
				"$sys-w-multiple",
				StreamState.NoStream,
				new(acl: new(writeRoles: new[] { "user1", "user2" })),
				userCredentials: TestCredentials.TestAdmin
			);

			await Streams.SetStreamMetadataAsync(
				"$sys-w-restricted",
				StreamState.NoStream,
				new(acl: new(writeRoles: Array.Empty<string>())),
				userCredentials: TestCredentials.TestAdmin
			);

			await Streams.SetStreamMetadataAsync(
				"$sys-w-all",
				StreamState.NoStream,
				new(acl: new(writeRole: SystemRoles.All)),
				userCredentials: TestCredentials.TestAdmin
			);
		}
	}
}
