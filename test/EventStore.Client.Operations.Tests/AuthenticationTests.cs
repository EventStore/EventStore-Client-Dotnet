namespace EventStore.Client;

public class AuthenticationTests : IClassFixture<EventStoreClientsFixture> {
    public AuthenticationTests(EventStoreClientsFixture fixture, ITestOutputHelper output) =>
        Fixture = fixture.With(f => f.CaptureLogs(output));

    EventStoreClientsFixture Fixture { get; }

    public enum CredentialsCase { None, TestUser, RootUser }
    
    public static IEnumerable<object?[]> InvalidAuthenticationCases() {
        yield return new object?[] { 2, CredentialsCase.TestUser, CredentialsCase.None };
        yield return new object?[] { 3, CredentialsCase.None, CredentialsCase.None };
        yield return new object?[] { 4, CredentialsCase.RootUser, CredentialsCase.TestUser };
        yield return new object?[] { 5, CredentialsCase.TestUser, CredentialsCase.TestUser };
        yield return new object?[] { 6, CredentialsCase.None, CredentialsCase.TestUser };
    }
    
    [Theory]
    [MemberData(nameof(InvalidAuthenticationCases))]
    public async Task system_call_with_invalid_credentials(int caseNr, CredentialsCase defaultCredentials, CredentialsCase actualCredentials) => 
        await ExecuteTest(caseNr, defaultCredentials, actualCredentials, shouldThrow: true);

    public static IEnumerable<object?[]> ValidAuthenticationCases() {
        yield return new object?[] { 1, CredentialsCase.RootUser, CredentialsCase.None };
        yield return new object?[] { 7, CredentialsCase.RootUser, CredentialsCase.RootUser };
        yield return new object?[] { 8, CredentialsCase.TestUser, CredentialsCase.RootUser };
        yield return new object?[] { 9, CredentialsCase.None, CredentialsCase.RootUser };
    }
    
    [Theory]
    [MemberData(nameof(ValidAuthenticationCases))]
    public async Task system_call_with_valid_credentials(int caseNr, CredentialsCase defaultCredentials, CredentialsCase actualCredentials) => 
        await ExecuteTest(caseNr, defaultCredentials, actualCredentials, shouldThrow: false);

    async Task ExecuteTest(int caseNr, CredentialsCase defaultCredentials, CredentialsCase actualCredentials, bool shouldThrow) {
        var testUser = await Fixture.CreateTestUser();

        var defaultUserCredentials = GetCredentials(defaultCredentials);
        var actualUserCredentials  = GetCredentials(actualCredentials);

        var settings = Fixture.GetOptions().ClientSettings;

        // var settings = new EventStoreClientSettings {
        //     Interceptors             = ogSettings.Interceptors,
        //     ConnectionName           = $"Authentication case #{caseNr} {defaultCredentials}",
        //     CreateHttpMessageHandler = ogSettings.CreateHttpMessageHandler,
        //     LoggerFactory            = ogSettings.LoggerFactory,
        //     ChannelCredentials       = ogSettings.ChannelCredentials,
        //     OperationOptions         = ogSettings.OperationOptions,
        //     ConnectivitySettings     = ogSettings.ConnectivitySettings,
        //     DefaultCredentials       = defaultUserCredentials,
        //     DefaultDeadline          = ogSettings.DefaultDeadline
        // };

        settings.DefaultCredentials = defaultUserCredentials;
        settings.ConnectionName     = $"Authentication case #{caseNr} {defaultCredentials}";

        await using var operations = new EventStoreOperationsClient(settings);

        if (shouldThrow) {
            await operations
                .SetNodePriorityAsync(1, userCredentials: actualUserCredentials)
                .ShouldThrowAsync<AccessDeniedException>();
        }
        else {
            await operations
                .SetNodePriorityAsync(1, userCredentials: actualUserCredentials)
                .ShouldNotThrowAsync();
        }

        return;

        UserCredentials? GetCredentials(CredentialsCase credentialsCase) =>
            credentialsCase switch {
                CredentialsCase.None     => null,
                CredentialsCase.TestUser => testUser.Credentials,
                CredentialsCase.RootUser => TestCredentials.Root,
                _                        => throw new ArgumentOutOfRangeException(nameof(credentialsCase), credentialsCase, null)
            };
    }
}