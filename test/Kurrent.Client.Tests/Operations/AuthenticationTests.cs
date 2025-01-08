using EventStore.Client;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:Operations")]
public class AuthenticationTests(ITestOutputHelper output, AuthenticationTests.CustomFixture fixture)
	: KurrentPermanentTests<AuthenticationTests.CustomFixture>(output, fixture) {
	public enum CredentialsCase { None, TestUser, RootUser }

	public static IEnumerable<object?[]> InvalidAuthenticationCases() {
		yield return [2, CredentialsCase.TestUser, CredentialsCase.None];
		yield return [3, CredentialsCase.None, CredentialsCase.None];
		yield return [4, CredentialsCase.RootUser, CredentialsCase.TestUser];
		yield return [5, CredentialsCase.TestUser, CredentialsCase.TestUser];
		yield return [6, CredentialsCase.None, CredentialsCase.TestUser];
	}

	[Theory]
	[MemberData(nameof(InvalidAuthenticationCases))]
	public async Task system_call_with_invalid_credentials(int caseNr, CredentialsCase defaultCredentials, CredentialsCase actualCredentials) =>
		await ExecuteTest(caseNr, defaultCredentials, actualCredentials, true);

	public static IEnumerable<object?[]> ValidAuthenticationCases() {
		yield return [1, CredentialsCase.RootUser, CredentialsCase.None];
		yield return [7, CredentialsCase.RootUser, CredentialsCase.RootUser];
		yield return [8, CredentialsCase.TestUser, CredentialsCase.RootUser];
		yield return [9, CredentialsCase.None, CredentialsCase.RootUser];
	}

	[Theory]
	[MemberData(nameof(ValidAuthenticationCases))]
	public async Task system_call_with_valid_credentials(int caseNr, CredentialsCase defaultCredentials, CredentialsCase actualCredentials) =>
		await ExecuteTest(caseNr, defaultCredentials, actualCredentials, false);

	async Task ExecuteTest(int caseNr, CredentialsCase defaultCredentials, CredentialsCase actualCredentials, bool shouldThrow) {
		var testUser = await Fixture.CreateTestUser();

		var defaultUserCredentials = GetCredentials(defaultCredentials);
		var actualUserCredentials  = GetCredentials(actualCredentials);

		var settings = Fixture.ClientSettings;

		settings.DefaultCredentials = defaultUserCredentials;
		settings.ConnectionName     = $"Authentication case #{caseNr} {defaultCredentials}";

		await using var operations = new KurrentOperationsClient(settings);

		if (shouldThrow)
			await operations
				.SetNodePriorityAsync(1, userCredentials: actualUserCredentials)
				.ShouldThrowAsync<AccessDeniedException>();
		else
			await operations
				.SetNodePriorityAsync(1, userCredentials: actualUserCredentials)
				.ShouldNotThrowAsync();

		return;

		UserCredentials? GetCredentials(CredentialsCase credentialsCase) =>
			credentialsCase switch {
				CredentialsCase.None     => null,
				CredentialsCase.TestUser => testUser.Credentials,
				CredentialsCase.RootUser => TestCredentials.Root,
				_                        => throw new ArgumentOutOfRangeException(nameof(credentialsCase), credentialsCase, null)
			};
	}

	public class CustomFixture() : KurrentPermanentFixture(x => x.WithoutDefaultCredentials());
}
