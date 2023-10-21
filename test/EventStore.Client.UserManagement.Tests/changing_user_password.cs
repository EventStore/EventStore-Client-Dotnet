using System.Net.Http.Headers;
using System.Text;

namespace EventStore.Client.Tests; 

public class changing_user_password : IClassFixture<EventStoreInsecureClientsFixture> {
	public changing_user_password(EventStoreInsecureClientsFixture fixture, ITestOutputHelper output) => 
            Fixture = fixture.With(f => f.CaptureLogs(output));

	EventStoreInsecureClientsFixture Fixture { get; }
    
    public static IEnumerable<object?[]> NullInputCases() {
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { null, x.Password, x.Password, "loginName" });
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, null, x.Password, "currentPassword" });
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, x.Password, null, "newPassword" });
	}

	[Theory, MemberData(nameof(NullInputCases))]
	public async Task with_null_input_throws(string loginName, string currentPassword, string newPassword, string paramName) {
		var ex = await Fixture.Users
			.ChangePasswordAsync(loginName, currentPassword, newPassword, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentNullException>();
		
		ex.ParamName.ShouldBe(paramName);
	}

    public static IEnumerable<object?[]> EmptyInputCases() {
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { string.Empty, x.Password, x.Password, "loginName" });
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, string.Empty, x.Password, "currentPassword" });
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, x.Password, string.Empty, "newPassword" });
	}

	[Theory, MemberData(nameof(EmptyInputCases))]
	public async Task with_empty_input_throws(string loginName, string currentPassword, string newPassword, string paramName) {
		var ex = await Fixture.Users
			.ChangePasswordAsync(loginName, currentPassword, newPassword, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentOutOfRangeException>();

		ex.ParamName.ShouldBe(paramName);
	}

	[Theory(Skip = "This can't be right"), ClassData(typeof(InvalidCredentialsCases))]
	public async Task with_user_with_insufficient_credentials_throws(string loginName, UserCredentials userCredentials) {
		await Fixture.Users.CreateUserAsync(loginName, "Full Name", Array.Empty<string>(), "password", userCredentials: TestCredentials.Root);
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.Users.ChangePasswordAsync(loginName, "password", "newPassword", userCredentials: userCredentials));
	}

	[Fact]
	public async Task when_the_current_password_is_wrong_throws() {
        var user = await Fixture.CreateTestUser();

		await Fixture.Users
			.ChangePasswordAsync(user.LoginName, "wrong-password", "new-password", userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<AccessDeniedException>();
	}

	[Fact]
	public async Task with_correct_credentials() {
        var user = await Fixture.CreateTestUser();
		
		await Fixture.Users
			.ChangePasswordAsync(user.LoginName, user.Password, "new-password", userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();
	}

	[Fact]
	public async Task with_own_credentials() {
        var user = await Fixture.CreateTestUser();

		await Fixture.Users
			.ChangePasswordAsync(user.LoginName, user.Password, "new-password", userCredentials: user.Credentials)
			.ShouldNotThrowAsync();
	}

     static readonly UTF8Encoding UTF8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    // static readonly UTF8Encoding UTF8Normal = new UTF8Encoding();
    
    [Fact]
    public async Task with_own_credentials_wtf() {
        var user = Fakers.Users.Generate();

        // var encodedNoBomBytes = UTF8NoBom.GetBytes($"{user.LoginName}:{user.Password}");
        // var encodedNoBom      = Convert.ToBase64String(encodedNoBomBytes);
        // var decodedNoBomBytes = Convert.FromBase64String(encodedNoBom);
        // var decodedNoBom      = UTF8NoBom.GetString(decodedNoBomBytes);
        //
        // var encodedBytes = UTF8Normal.GetBytes($"{user.LoginName}:{user.Password}");
        // var encoded      = Convert.ToBase64String(encodedBytes);
        // var decodedBytes = Convert.FromBase64String(encoded);
        // var decoded      = UTF8Normal.GetString(decodedBytes);
        //
        //
        //var loginName = Guid.NewGuid().ToString();
        
        await Fixture.Users.CreateUserAsync(
            user.LoginName, "Full Name", Array.Empty<string>(),
            user.Password, userCredentials: TestCredentials.Root
        );
        //
        // await Fixture.Users.ChangePasswordAsync(
        //     user.LoginName, user.Password, "newPassword",
        //     userCredentials: new UserCredentials(user.LoginName, user.Password)
        // );

        var Authorization = new AuthenticationHeaderValue(
            Constants.Headers.BasicScheme,
            Convert.ToBase64String(UTF8NoBom.GetBytes($"{user.LoginName}:{user.Password}"))
        );
        
        var creds = GetBasicAuth(Authorization);
        
        return;

        static (string? Username, string? Password) GetBasicAuth(AuthenticationHeaderValue header) {
            if (header.Parameter == null || header.Scheme != Constants.Headers.BasicScheme) {
                return (null, null);
            }

            var credentials = UTF8NoBom.GetString(Convert.FromBase64String(header.Parameter)).AsSpan();
            
            var passwordStart = credentials.IndexOf(':') + 1;

            var password = credentials[passwordStart..].ToString();
            var username = credentials[..password.Length].ToString();

            return (username, password);
        }
    }
    
    [Fact]
    public async Task with_own_credentials_old() {
        var loginName = Guid.NewGuid().ToString();
        await Fixture.Users.CreateUserAsync(
            loginName, "Full Name", Array.Empty<string>(),
            "password", userCredentials: TestCredentials.Root
        );

        await Fixture.Users.ChangePasswordAsync(
            loginName, "password", "newPassword",
            userCredentials: new UserCredentials(loginName, "password")
        );
    }
}
