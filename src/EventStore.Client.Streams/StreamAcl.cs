namespace EventStore.Client;

/// <summary>
/// Represents an access control list for a stream
/// </summary>
public sealed class StreamAcl {
	/// <summary>
	/// Creates a new Stream Access Control List
	/// </summary>
	/// <param name="readRole">Role and user permitted to read the stream</param>
	/// <param name="writeRole">Role and user permitted to write to the stream</param>
	/// <param name="deleteRole">Role and user permitted to delete the stream</param>
	/// <param name="metaReadRole">Role and user permitted to read stream metadata</param>
	/// <param name="metaWriteRole">Role and user permitted to write stream metadata</param>
	public StreamAcl(
		string? readRole = null, string? writeRole = null, string? deleteRole = null,
		string? metaReadRole = null, string? metaWriteRole = null
	)
		: this(
			readRole == null ? null : new[] { readRole },
			writeRole == null ? null : new[] { writeRole },
			deleteRole == null ? null : new[] { deleteRole },
			metaReadRole == null ? null : new[] { metaReadRole },
			metaWriteRole == null ? null : new[] { metaWriteRole }
		) { }

	/// <summary>
	/// 
	/// </summary>
	/// <param name="readRoles">Roles and users permitted to read the stream</param>
	/// <param name="writeRoles">Roles and users permitted to write to the stream</param>
	/// <param name="deleteRoles">Roles and users permitted to delete the stream</param>
	/// <param name="metaReadRoles">Roles and users permitted to read stream metadata</param>
	/// <param name="metaWriteRoles">Roles and users permitted to write stream metadata</param>
	public StreamAcl(
		string[]? readRoles = null, string[]? writeRoles = null, string[]? deleteRoles = null,
		string[]? metaReadRoles = null, string[]? metaWriteRoles = null
	) {
		ReadRoles      = readRoles;
		WriteRoles     = writeRoles;
		DeleteRoles    = deleteRoles;
		MetaReadRoles  = metaReadRoles;
		MetaWriteRoles = metaWriteRoles;
	}

	/// <summary>
	/// Roles and users permitted to read the stream
	/// </summary>
	public string[]? ReadRoles { get; }

	/// <summary>
	/// Roles and users permitted to write to the stream
	/// </summary>
	public string[]? WriteRoles { get; }

	/// <summary>
	/// Roles and users permitted to delete the stream
	/// </summary>
	public string[]? DeleteRoles { get; }

	/// <summary>
	/// Roles and users permitted to read stream metadata
	/// </summary>
	public string[]? MetaReadRoles { get; }

	/// <summary>
	/// Roles and users permitted to write stream metadata
	/// </summary>
	public string[]? MetaWriteRoles { get; }

	bool Equals(StreamAcl other) =>
		(ReadRoles ?? []).SequenceEqual(other.ReadRoles ?? []) &&
		(WriteRoles ?? []).SequenceEqual(other.WriteRoles ?? []) &&
		(DeleteRoles ?? []).SequenceEqual(other.DeleteRoles ?? []) &&
		(MetaReadRoles ?? []).SequenceEqual(other.MetaReadRoles ?? []) &&
		(MetaWriteRoles ?? []).SequenceEqual(other.MetaWriteRoles ?? []);

	/// <inheritdoc />
	public override bool Equals(object? obj) =>
		!ReferenceEquals(null, obj) &&
		(ReferenceEquals(this, obj) || (obj.GetType() == GetType() && Equals((StreamAcl)obj)));

	/// <summary>
	/// Compares left and right for equality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>True if left is equal to right.</returns>
	public static bool operator ==(StreamAcl? left, StreamAcl? right) => Equals(left, right);

	/// <summary>
	/// Compares left and right for inequality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>True if left is not equal to right.</returns>
	public static bool operator !=(StreamAcl? left, StreamAcl? right) => !Equals(left, right);

	/// <inheritdoc />
	public override int GetHashCode() =>
		HashCode.Hash.Combine(ReadRoles).Combine(WriteRoles).Combine(DeleteRoles).Combine(MetaReadRoles)
			.Combine(MetaWriteRoles);

	/// <inheritdoc />
	public override string ToString() =>
		$"Read: {(ReadRoles == null ? "<null>" : "[" + string.Join(",", ReadRoles) + "]")}, Write: {(WriteRoles == null ? "<null>" : "[" + string.Join(",", WriteRoles) + "]")}, Delete: {(DeleteRoles == null ? "<null>" : "[" + string.Join(",", DeleteRoles) + "]")}, MetaRead: {(MetaReadRoles == null ? "<null>" : "[" + string.Join(",", MetaReadRoles) + "]")}, MetaWrite: {(MetaWriteRoles == null ? "<null>" : "[" + string.Join(",", MetaWriteRoles) + "]")}";
}