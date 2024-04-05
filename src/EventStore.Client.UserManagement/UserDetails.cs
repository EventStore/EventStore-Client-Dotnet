namespace EventStore.Client;

/// <summary>
/// Provides the details for a user.
/// </summary>
public readonly struct UserDetails : IEquatable<UserDetails> {
	/// <summary>
	/// The users login name.
	/// </summary>
	public readonly string LoginName;

	/// <summary>
	/// The full name of the user.
	/// </summary>
	public readonly string FullName;

	/// <summary>
	/// The groups the user is a member of.
	/// </summary>
	public readonly string[] Groups;

	/// <summary>
	/// The date/time the user was updated in UTC format.
	/// </summary>
	public readonly DateTimeOffset? DateLastUpdated;

	/// <summary>
	/// Whether the user disable or not.
	/// </summary>
	public readonly bool Disabled;

	/// <summary>
	/// create a new <see cref="UserDetails"/> class.
	/// </summary>
	/// <param name="loginName">The login name of the user.</param>
	/// <param name="fullName">The users full name.</param>
	/// <param name="groups">The groups this user is a member if.</param>
	/// <param name="disabled">Is this user disabled or not.</param>
	/// <param name="dateLastUpdated">The datt/time this user was last updated in UTC format.</param>
	public UserDetails(string loginName, string fullName, string[] groups, bool disabled, DateTimeOffset? dateLastUpdated) {
		if (loginName is null) throw new ArgumentNullException(nameof(loginName));

		if (fullName is null) throw new ArgumentNullException(nameof(fullName));

		if (groups is null) throw new ArgumentNullException(nameof(groups));

		LoginName       = loginName;
		FullName        = fullName;
		Groups          = groups;
		Disabled        = disabled;
		DateLastUpdated = dateLastUpdated;
	}

	/// <inheritdoc />
	public bool Equals(UserDetails other) =>
		LoginName == other.LoginName && FullName == other.FullName && Groups.SequenceEqual(other.Groups) &&
		Nullable.Equals(DateLastUpdated, other.DateLastUpdated) && Disabled == other.Disabled;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is UserDetails other && Equals(other);

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Hash.Combine(LoginName).Combine(FullName).Combine(Groups)
		.Combine(Disabled).Combine(DateLastUpdated);

	/// <summary>
	/// Compares left and right for equality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>True if left is equal to right.</returns>
	public static bool operator ==(UserDetails left, UserDetails right) => left.Equals(right);

	/// <summary>
	/// Compares left and right for inequality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>True if left is not equal to right.</returns>
	public static bool operator !=(UserDetails left, UserDetails right) => !left.Equals(right);

	/// <inheritdoc />
	public override string ToString() =>
		new {
			Disabled,
			FullName,
			LoginName,
			Groups = string.Join(",", Groups)
		}?.ToString()!;
}