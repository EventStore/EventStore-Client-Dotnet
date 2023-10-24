namespace EventStore.Client.Tests; 

public class deleting_a_user : EventStoreFixture {
    public deleting_a_user(ITestOutputHelper output) 
        : base(output, x => x.WithoutDefaultCredentials()) { }
        
    [Fact]
    public async Task with_null_input_throws() {
        var ex = await Fixture.Users
            .DeleteUserAsync(null!, userCredentials: TestCredentials.Root)
            .ShouldThrowAsync<ArgumentNullException>();

        ex.ParamName.ShouldBe("loginName");
    }

    [Fact]
    public async Task with_empty_input_throws() {
        var ex =await Fixture.Users
            .DeleteUserAsync(string.Empty, userCredentials: TestCredentials.Root)
            .ShouldThrowAsync<ArgumentOutOfRangeException>();
            
        ex.ParamName.ShouldBe("loginName");
    }

    [Theory, ClassData(typeof(InvalidCredentialsCases))]
    public async Task with_user_with_insufficient_credentials_throws(TestUser user, Type expectedException) {
        await Fixture.Users.CreateUserAsync(
            user.LoginName, user.FullName, user.Groups,
            user.Password, userCredentials: TestCredentials.Root
        );

        await Fixture.Users
            .DeleteUserAsync(user.LoginName, userCredentials: user.Credentials)
            .ShouldThrowAsync(expectedException);
    }

    [Fact]
    public async Task cannot_be_read() {
        var user = await Fixture.CreateTestUser();

        await Fixture.Users.DeleteUserAsync(user.LoginName, userCredentials: TestCredentials.Root);

        var ex = await Fixture.Users
            .GetUserAsync(user.LoginName, userCredentials: TestCredentials.Root)
            .ShouldThrowAsync<UserNotFoundException>();

        ex.LoginName.ShouldBe(user.LoginName);
    }

    [Fact]
    public async Task a_second_time_throws() {
        var user = await Fixture.CreateTestUser();

        await Fixture.Users.DeleteUserAsync(user.LoginName, userCredentials: TestCredentials.Root);

        var ex = await Fixture.Users
            .DeleteUserAsync(user.LoginName, userCredentials: TestCredentials.Root)
            .ShouldThrowAsync<UserNotFoundException>();

        ex.LoginName.ShouldBe(user.LoginName);
    }
}