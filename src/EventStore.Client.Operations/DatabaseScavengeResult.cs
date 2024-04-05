namespace EventStore.Client;

/// <summary>
/// A structure representing the result of a scavenge operation.
/// </summary>
public readonly struct DatabaseScavengeResult : IEquatable<DatabaseScavengeResult> {
	/// <summary>
	/// The ID of the scavenge operation.
	/// </summary>
	public string ScavengeId { get; }

	/// <summary>
	/// The <see cref="ScavengeResult"/> of the scavenge operation.
	/// </summary>
	public ScavengeResult Result { get; }

	/// <summary>
	/// A scavenge operation that has started.
	/// </summary>
	/// <param name="scavengeId"></param>
	/// <returns></returns>
	public static DatabaseScavengeResult Started(string scavengeId) =>
		new(scavengeId, ScavengeResult.Started);

	/// <summary>
	/// A scavenge operation that has stopped.
	/// </summary>
	/// <param name="scavengeId"></param>
	/// <returns></returns>
	public static DatabaseScavengeResult Stopped(string scavengeId) =>
		new(scavengeId, ScavengeResult.Stopped);

	/// <summary>
	/// A scavenge operation that is currently in progress.
	/// </summary>
	/// <param name="scavengeId"></param>
	/// <returns></returns>
	public static DatabaseScavengeResult InProgress(string scavengeId) =>
		new(scavengeId, ScavengeResult.InProgress);

	/// <summary>
	/// A scavenge operation whose state is unknown.
	/// </summary>
	/// <param name="scavengeId"></param>
	/// <returns></returns>
	public static DatabaseScavengeResult Unknown(string scavengeId) =>
		new(scavengeId, ScavengeResult.Unknown);

	DatabaseScavengeResult(string scavengeId, ScavengeResult result) {
		ScavengeId = scavengeId;
		Result     = result;
	}

	/// <inheritdoc />
	public bool Equals(DatabaseScavengeResult other) => ScavengeId == other.ScavengeId && Result == other.Result;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is DatabaseScavengeResult other && Equals(other);

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Hash.Combine(ScavengeId).Combine(Result);

	/// <summary>
	/// Compares left and right for equality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>True if left is equal to right.</returns>
	public static bool operator ==(DatabaseScavengeResult left, DatabaseScavengeResult right) => left.Equals(right);

	/// <summary>
	/// Compares left and right for inequality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>True if left is not equal to right.</returns>
	public static bool operator !=(DatabaseScavengeResult left, DatabaseScavengeResult right) =>
		!left.Equals(right);
}