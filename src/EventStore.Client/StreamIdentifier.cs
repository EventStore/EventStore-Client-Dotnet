using System.Text;
using Google.Protobuf;

namespace EventStore.Client {
#pragma warning disable 1591
	public partial class StreamIdentifier {
		private string? _cached;

		public static implicit operator string?(StreamIdentifier? source) {
			if (source == null) {
				return null;
			}
			if (source._cached != null || source.StreamName.IsEmpty) return source._cached;

			var tmp = Encoding.UTF8.GetString(source.StreamName.Span);
			//this doesn't have to be thread safe, its just a cache in case the identifier is turned into a string several times
			source._cached = tmp;
			return source._cached;
		}

		public static implicit operator StreamIdentifier(string source) =>
			new() {StreamName = ByteString.CopyFromUtf8(source)};
	}
}
