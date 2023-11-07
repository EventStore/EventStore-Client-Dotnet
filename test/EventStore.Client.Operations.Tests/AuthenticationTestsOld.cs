namespace EventStore.Client;

public class AuthenticationTestsOld : IClassFixture<InsecureClientTestFixture> {
	public AuthenticationTestsOld(ITestOutputHelper output, InsecureClientTestFixture fixture) => Fixture = fixture.With(x => x.CaptureTestRun(output));

	InsecureClientTestFixture Fixture { get; }
		
	private static readonly Dictionary<string, UserCredentials> _credentials =
		new Dictionary<string, UserCredentials> {
			{ nameof(TestCredentials.Root), TestCredentials.Root },
			{ nameof(TestCredentials.TestUser1), TestCredentials.TestUser1 },
		};

	public static IEnumerable<object?[]> AuthenticationCases() {
		var root     = nameof(TestCredentials.Root);
		var testUser = nameof(TestCredentials.TestUser1);
			
		var shouldFail    = false;
		var shouldSucceed = true;
			
		// no user credentials
		yield return new object?[] {1, root,     null, shouldSucceed};
		yield return new object?[] {2, testUser, null, shouldFail};
		yield return new object?[] {3, null,     null, shouldFail};
			
		// unprivileged user credentials
		yield return new object?[] {4, root,      testUser, shouldFail};
		yield return new object?[] {5, testUser,  testUser, shouldFail};
		yield return new object?[] {6, null,      testUser, shouldFail};
			
		// root user credentials
		yield return new object?[] {7, root,     root, shouldSucceed};
		yield return new object?[] {8, testUser, root, shouldSucceed};
		yield return new object?[] {9, null,     root, shouldSucceed};
	}

	[Theory, MemberData(nameof(AuthenticationCases))]
	public async Task system_call_with_credentials_combination(int caseNr, string? defaultUser, string? user, bool succeeds) {
		await Fixture.Users.CreateUserWithRetry(
				loginName: TestCredentials.TestUser1.Username!,
				fullName: nameof(TestCredentials.TestUser1),
				groups: Array.Empty<string>(),
				password: TestCredentials.TestUser1.Password!,
				userCredentials: TestCredentials.Root
			)
			.WithTimeout(TimeSpan.FromMilliseconds(1000));		
		
		var settings = Fixture.ClientSettings;
		
		settings.DefaultCredentials = defaultUser != null ? _credentials[defaultUser] : null;
		settings.ConnectionName     = $"Authentication case #{caseNr} {defaultUser}";
			
		await using var client = new EventStoreOperationsClient(settings);

		var result = await Record.ExceptionAsync(() =>
			client.SetNodePriorityAsync(1, userCredentials: user != null ? _credentials[user] : null));
			
		if (succeeds) {
			Assert.Null(result);
			return;
		}

		Assert.NotNull(result);
	}
		
	// public class Fixture : EventStoreClientFixture {
	// 	protected override async Task Given() {
	// 		var userManagementClient = new EventStoreUserManagementClient(Settings);
	// 		await userManagementClient.WarmUpAsync();
	//
	// 		await userManagementClient.CreateUserWithRetry(
	// 				loginName: TestCredentials.TestUser1.Username!,
	// 				fullName: nameof(TestCredentials.TestUser1),
	// 				groups: Array.Empty<string>(),
	// 				password: TestCredentials.TestUser1.Password!,
	// 				userCredentials: TestCredentials.Root)
	// 			.WithTimeout(TimeSpan.FromMilliseconds(1000));				
	// 	}
	// 		
	// 	protected override Task When() => Task.CompletedTask;
	// }
}