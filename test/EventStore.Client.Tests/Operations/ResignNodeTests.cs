using EventStore.Client.Tests.TestNode;
using EventStore.Client.Tests;

namespace EventStore.Client.Tests.Operations;

public class ResignNodeTests(ITestOutputHelper output, ResignNodeTests.CustomFixture fixture)
	: KurrentTemporaryTests<ResignNodeTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task resign_node_does_not_throw() =>
		await Fixture.Operations
			.ResignNodeAsync(userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();

	[RetryFact]
	public async Task resign_node_without_credentials_throws() =>
		await Fixture.Operations
			.ResignNodeAsync()
			.ShouldThrowAsync<AccessDeniedException>();

	public class CustomFixture() : KurrentTemporaryFixture(x => x.WithoutDefaultCredentials());
}
