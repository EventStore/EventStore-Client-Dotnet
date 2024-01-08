using System.Net.Http.Headers;
using System.Text;
using static System.Convert;

namespace EventStore.Client.Tests;

public class UserCredentialsTests {
	const string JwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9."
	                      + "eyJzdWIiOiI5OSIsIm5hbWUiOiJKb2huIFdpY2siLCJpYXQiOjE1MTYyMzkwMjJ9."
	                      + "MEdv44JIdlLh-GgqxOTZD7DHq28xJowhQFmDnT3NDIE";

	static readonly UTF8Encoding Utf8NoBom = new(false);

	static string EncodeCredentials(string username, string password) => ToBase64String(Utf8NoBom.GetBytes($"{username}:{password}"));

	[Fact]
	public void from_username_and_password() {
		var user = Fakers.Users.WithNonAsciiPassword();

		var value = new AuthenticationHeaderValue(
			Constants.Headers.BasicScheme,
			EncodeCredentials(user.LoginName, user.Password)
		);

		var basicAuthInfo = value.ToString();

		var credentials = new UserCredentials(user.LoginName, user.Password);

		credentials.Username.ShouldBe(user.LoginName);
		credentials.Password.ShouldBe(user.Password);
		credentials.ToString().ShouldBe(basicAuthInfo);
	}

	[Fact]
	public void from_bearer_token() {
		var credentials = new UserCredentials(JwtToken);

		credentials.Username.ShouldBeNull();
		credentials.Password.ShouldBeNull();
		credentials.ToString().ShouldBe($"Bearer {JwtToken}");
	}
}