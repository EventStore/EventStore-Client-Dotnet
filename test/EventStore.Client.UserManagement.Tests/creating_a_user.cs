namespace EventStore.Client.Tests;

public class creating_a_user : EventStoreFixture {
    public creating_a_user(ITestOutputHelper output) : base(output) { }
	
	public static IEnumerable<object?[]> NullInputCases() {
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { null, x.FullName, x.Groups, x.Password, "loginName" });
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, null, x.Groups, x.Password, "fullName" });
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, x.FullName, null, x.Password, "groups" });
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, x.FullName, x.Groups, null, "password" });
	}

	[Theory, MemberData(nameof(NullInputCases))]
	public async Task with_null_input_throws(string loginName, string fullName, string[] groups, string password, string paramName) {
		var ex = await Fixture.Users
			.CreateUserAsync(loginName, fullName, groups, password, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentNullException>();
	
		ex.ParamName.ShouldBe(paramName);
	}
    
    public static IEnumerable<object?[]> EmptyInputCases() {
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { string.Empty, x.FullName, x.Groups, x.Password, "loginName" });
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, string.Empty, x.Groups, x.Password, "fullName" });
        yield return Fakers.Users.Generate().WithResult(x => new object?[] { x.LoginName, x.FullName, x.Groups, string.Empty, "password" });
	}
    
	[Theory, MemberData(nameof(EmptyInputCases))]
	public async Task with_empty_input_throws(string loginName, string fullName, string[] groups, string password, string paramName) {
		var ex = await Fixture.Users
			.CreateUserAsync(loginName, fullName, groups, password, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ArgumentOutOfRangeException>();

		ex.ParamName.ShouldBe(paramName);
	}
	
	[Fact]
	public async Task with_password_containing_ascii_chars() {
		var user = Fakers.Users.Generate();

		await Fixture.Users
			.CreateUserAsync(user.LoginName, user.FullName, user.Groups, user.Password, userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();
	}
	
	[Theory, ClassData(typeof(InvalidCredentialsCases))]
	public async Task with_user_with_insufficient_credentials_throws(TestUser user, Type expectedException) {
        await Fixture.Users
            .CreateUserAsync(user.LoginName, user.FullName, user.Groups, user.Password, userCredentials: user.Credentials)
            .ShouldThrowAsync(expectedException);
	}

	[Fact]
	public async Task can_be_read() {
		var user = Fakers.Users.Generate();

		await Fixture.Users
            .CreateUserAsync(
                user.LoginName,
                user.FullName,
                user.Groups,
                user.Password,
                userCredentials: TestCredentials.Root
            )
            .ShouldNotThrowAsync();

		var actual = await Fixture.Users.GetUserAsync(user.LoginName, userCredentials: TestCredentials.Root);

		var expected = new UserDetails(
            user.Details.LoginName,
            user.Details.FullName,
            user.Details.Groups, 
			user.Details.Disabled,
			actual.DateLastUpdated
		);
		
		actual.ShouldBeEquivalentTo(expected);
	}
}
