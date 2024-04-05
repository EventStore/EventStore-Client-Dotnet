namespace EventStore.Client;

/// <summary>
/// A class representing default access control lists.
/// </summary>
public sealed class SystemSettings {
	/// <summary>
	/// Constructs a new <see cref="SystemSettings"/>.
	/// </summary>
	/// <param name="userStreamAcl"></param>
	/// <param name="systemStreamAcl"></param>
	public SystemSettings(StreamAcl? userStreamAcl = null, StreamAcl? systemStreamAcl = null) {
		UserStreamAcl   = userStreamAcl;
		SystemStreamAcl = systemStreamAcl;
	}

	/// <summary>
	/// Default access control list for new user streams.
	/// </summary>
	public StreamAcl? UserStreamAcl { get; }

	/// <summary>
	/// Default access control list for new system streams.
	/// </summary>
	public StreamAcl? SystemStreamAcl { get; }

	bool Equals(SystemSettings other)
		=> Equals(UserStreamAcl, other.UserStreamAcl) && Equals(SystemStreamAcl, other.SystemStreamAcl);

	/// <inheritdoc />
	public override bool Equals(object? obj)
		=> ReferenceEquals(this, obj) || (obj is SystemSettings other && Equals(other));

	/// <summary>
	/// Compares left and right for equality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>True if left is equal to right.</returns>
	public static bool operator ==(SystemSettings? left, SystemSettings? right) => Equals(left, right);

	/// <summary>
	/// Compares left and right for inequality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>True if left is not equal to right.</returns>
	public static bool operator !=(SystemSettings? left, SystemSettings? right) => !Equals(left, right);

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Hash.Combine(UserStreamAcl?.GetHashCode())
		.Combine(SystemStreamAcl?.GetHashCode());
}