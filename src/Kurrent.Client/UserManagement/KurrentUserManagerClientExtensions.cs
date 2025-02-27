namespace EventStore.Client;

/// <summary>
///  A set of extension methods for an <see cref="KurrentUserManagementClient"/>.
/// </summary>
public static class KurrentUserManagerClientExtensions {
    /// <summary>
    /// Gets the <see cref="UserDetails"/> of the internal user specified by the supplied <see cref="UserCredentials"/>.
    /// </summary>
    /// <param name="users"></param>
    /// <param name="userCredentials"></param>
    /// <param name="deadline"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task<UserDetails> GetCurrentUserAsync(
        this KurrentUserManagementClient users,
        UserCredentials userCredentials, TimeSpan? deadline = null, CancellationToken cancellationToken = default
    ) =>
        users.GetUserAsync(
            userCredentials.Username!, deadline, userCredentials,
            cancellationToken
        );
}
