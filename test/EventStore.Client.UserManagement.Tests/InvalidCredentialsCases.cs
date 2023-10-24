using System.Collections;

namespace EventStore.Client.Tests;

public class InvalidCredentialsCases : IEnumerable<object?[]> {
    public IEnumerator<object?[]> GetEnumerator() {
        yield return Fakers.Users.WithNoCredentials().WithResult(user => new object?[] { user, typeof(AccessDeniedException) });
        yield return Fakers.Users.WithInvalidCredentials(false).WithResult(user => new object?[] { user, typeof(NotAuthenticatedException) });
        yield return Fakers.Users.WithInvalidCredentials(wrongPassword: false).WithResult(user => new object?[] { user, typeof(NotAuthenticatedException) });
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}