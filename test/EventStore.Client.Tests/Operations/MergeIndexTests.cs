namespace EventStore.Client.Tests;

public class MergeIndexTests(ITestOutputHelper output, MergeIndexTests.CustomFixture fixture)
	: EventStorePermanentTests<MergeIndexTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task merge_indexes_does_not_throw() =>
		await Fixture.Operations
			.MergeIndexesAsync(userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();

	[RetryFact]
	public async Task merge_indexes_without_credentials_throws() =>
		await Fixture.Operations
			.MergeIndexesAsync()
			.ShouldThrowAsync<AccessDeniedException>();

	public class CustomFixture() : KurrentPermanentFixture(x => x.WithoutDefaultCredentials());
}
