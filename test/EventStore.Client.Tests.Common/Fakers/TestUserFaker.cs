namespace EventStore.Client.Tests;

public class TestUser {
    public UserDetails      Details     { get; set; } = default!;
    public UserCredentials? Credentials { get; set; } = default!;

    public string   LoginName { get; set; } = null!;
    public string   FullName  { get; set; } = null!;
    public string[] Groups    { get; set; } = null!;
    public string   Password  { get; set; } = null!;

    public override string ToString() => $"{LoginName} Credentials({Credentials?.Username ?? "null"})";
}

public sealed class TestUserFaker : Faker<TestUser> {
    internal static TestUserFaker Instance => new();

    TestUserFaker() {
        RuleFor(x => x.LoginName, f => f.Person.UserName);
        RuleFor(x => x.FullName, f => f.Person.FullName);
        RuleFor(x => x.Groups, f => f.Lorem.Words());
        RuleFor(x => x.Password, () => PasswordGenerator.GenerateSimplePassword());
        RuleFor(x => x.Credentials, (_, user) => new(user.LoginName, user.Password));
        RuleFor(x => x.Details, (_, user) => new(user.LoginName, user.FullName, user.Groups, disabled: false, dateLastUpdated: default));
    }

    public TestUser WithValidCredentials() => Generate();

    public TestUser WithNoCredentials() =>
        Instance
            .FinishWith((_, x) => x.Credentials = null)
            .Generate();

    public TestUser WithInvalidCredentials(bool wrongLoginName = true, bool wrongPassword = true) =>
        Instance
            .FinishWith(
                (f, x) => x.Credentials = new(
                    wrongLoginName ? "wrong-username" : x.LoginName,
                    wrongPassword ? "wrong-password" : x.Password
                )
            )
            .Generate();
    
    public TestUser WithNonAsciiPassword() =>
	    Instance
		    .RuleFor(x => x.Password, () => PasswordGenerator.GeneratePassword())
		    .RuleFor(x => x.Credentials, (_, user) => new (user.LoginName, user.Password))
		    .Generate();
}

public static partial class Fakers {
    public static TestUserFaker Users => TestUserFaker.Instance;
}



