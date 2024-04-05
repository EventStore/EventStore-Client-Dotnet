#pragma warning disable 1591

using System.Text;
using Google.Protobuf;

namespace EventStore.Client;

public partial class StreamIdentifier {
	string? _cached;

	public static implicit operator string?(StreamIdentifier? source) {
		if (source == null) return null;
		if (source._cached != null || source.StreamName.IsEmpty) return source._cached;

#if NET48
		var tmp = Encoding.UTF8.GetString(source.StreamName.ToByteArray());
#else
		var tmp = Encoding.UTF8.GetString(source.StreamName.Span);
#endif
		//this doesn't have to be thread safe, its just a cache in case the identifier is turned into a string several times
		source._cached = tmp;
		return source._cached;
	}

	public static implicit operator StreamIdentifier(string source) =>
		new() { StreamName = ByteString.CopyFromUtf8(source) };
}