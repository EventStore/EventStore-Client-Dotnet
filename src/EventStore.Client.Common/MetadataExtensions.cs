using Grpc.Core;

namespace EventStore.Client;

static class MetadataExtensions {
	public static bool TryGetValue(this Metadata metadata, string key, out string? value) {
		value = default;

		foreach (var entry in metadata) {
			if (entry.Key != key)
				continue;

			value = entry.Value;
			return true;
		}

		return false;
	}

	public static StreamRevision GetStreamRevision(this Metadata metadata, string key) =>
		metadata.TryGetValue(key, out var s) && ulong.TryParse(s, out var value)
			? new(value)
			: StreamRevision.None;

	public static int GetIntValueOrDefault(this Metadata metadata, string key) =>
		metadata.TryGetValue(key, out var s) && int.TryParse(s, out var value)
			? value
			: default;
}