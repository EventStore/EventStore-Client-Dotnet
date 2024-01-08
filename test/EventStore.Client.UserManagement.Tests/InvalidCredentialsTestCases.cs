using System.Collections;

namespace EventStore.Client.Tests;

public abstract record InvalidCredentialsTestCase(TestUser User, Type ExpectedException);

public class InvalidCredentialsTestCases : IEnumerable<object?[]> {
	public IEnumerator<object?[]> GetEnumerator() {
		yield return new object?[] { new MissingCredentials() };
		yield return new object?[] { new WrongUsername() };
		yield return new object?[] { new WrongPassword() };
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public record MissingCredentials()
		: InvalidCredentialsTestCase(Fakers.Users.WithNoCredentials(), typeof(AccessDeniedException)) {
		public override string ToString() => nameof(MissingCredentials);
	}

	public record WrongUsername()
		: InvalidCredentialsTestCase(Fakers.Users.WithInvalidCredentials(false), typeof(NotAuthenticatedException)) {
		public override string ToString() => nameof(WrongUsername);
	}

	public record WrongPassword()
		: InvalidCredentialsTestCase(Fakers.Users.WithInvalidCredentials(wrongPassword: false), typeof(NotAuthenticatedException)) {
		public override string ToString() => nameof(WrongPassword);
	}
}