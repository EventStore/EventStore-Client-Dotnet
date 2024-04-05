using System.Text.Json;

namespace EventStore.Client;

/// <summary>
/// A structure representing a stream's custom metadata with strongly typed properties
/// for system values and a dictionary-like interface for custom values.
/// </summary>
public readonly struct StreamMetadata : IEquatable<StreamMetadata> {
	/// <summary>
	/// The optional maximum age of events allowed in the stream.
	/// </summary>
	public TimeSpan? MaxAge { get; }

	/// <summary>
	/// The optional <see cref="StreamPosition"/> from which previous events can be scavenged.
	/// This is used to implement soft-deletion of streams.
	/// </summary>

	public StreamPosition? TruncateBefore { get; }

	/// <summary>
	/// The optional amount of time for which the stream head is cacheable.
	/// </summary>
	public TimeSpan? CacheControl { get; }

	/// <summary>
	/// The optional <see cref="StreamAcl"/> for the stream.
	/// </summary>
	public StreamAcl? Acl { get; }

	/// <summary>
	/// The optional maximum number of events allowed in the stream.
	/// </summary>
	public int? MaxCount { get; }

	/// <summary>
	/// The optional <see cref="JsonDocument"/> of user provided metadata.
	/// </summary>
	public JsonDocument? CustomMetadata { get; }

	/// <summary>
	/// Constructs a new <see cref="StreamMetadata"/>.
	/// </summary>
	/// <param name="maxCount"></param>
	/// <param name="maxAge"></param>
	/// <param name="truncateBefore"></param>
	/// <param name="cacheControl"></param>
	/// <param name="acl"></param>
	/// <param name="customMetadata"></param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public StreamMetadata(
		int? maxCount = null,
		TimeSpan? maxAge = null,
		StreamPosition? truncateBefore = null,
		TimeSpan? cacheControl = null,
		StreamAcl? acl = null,
		JsonDocument? customMetadata = null
	) : this() {
		if (maxCount <= 0) throw new ArgumentOutOfRangeException(nameof(maxCount));

		if (maxAge <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maxAge));

		if (cacheControl <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(cacheControl));

		MaxAge         = maxAge;
		TruncateBefore = truncateBefore;
		CacheControl   = cacheControl;
		Acl            = acl;
		MaxCount       = maxCount;
		CustomMetadata = customMetadata ?? JsonDocument.Parse("{}");
	}

	/// <inheritdoc />
	public bool Equals(StreamMetadata other) => Nullable.Equals(MaxAge, other.MaxAge) &&
	                                            Nullable.Equals(TruncateBefore, other.TruncateBefore) &&
	                                            Nullable.Equals(CacheControl, other.CacheControl) &&
	                                            Equals(Acl, other.Acl) && MaxCount == other.MaxCount &&
	                                            string.Equals(
		                                            CustomMetadata?.RootElement.GetRawText(),
		                                            other.CustomMetadata?.RootElement.GetRawText()
	                                            );

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is StreamMetadata other && other.Equals(this);

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Hash.Combine(MaxAge).Combine(TruncateBefore).Combine(CacheControl)
		.Combine(Acl?.GetHashCode()).Combine(MaxCount);

	/// <summary>
	/// Compares left and right for equality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>True if left is equal to right.</returns>
	public static bool operator ==(StreamMetadata left, StreamMetadata right) => Equals(left, right);

	/// <summary>
	/// Compares left and right for inequality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>True if left is not equal to right.</returns>
	public static bool operator !=(StreamMetadata left, StreamMetadata right) => !Equals(left, right);
}