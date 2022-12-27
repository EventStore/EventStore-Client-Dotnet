using System;
using System.Text.Json;

namespace EventStore.Client {
	public static class ReadOnlyMemoryExtensions {
		public static Position ParsePosition(this ReadOnlyMemory<byte> json) {
			var doc = JsonDocument.Parse(json);
			var checkPoint = doc.RootElement.GetString();

			if (checkPoint == null) {
				throw new Exception("Unable to parse Position, data is missing!");
			}

			Position.TryParse(checkPoint, out var position);
			if (position.HasValue) {
				return position.Value;
			}
			
			throw new Exception("Unable to parse Position, invalid data!");
		}

		public static StreamPosition ParseStreamPosition(this ReadOnlyMemory<byte> json) {
			var doc = JsonDocument.Parse(json);
			var checkPoint = doc.RootElement.GetString();

			if (checkPoint == null) {
				throw new Exception("Unable to parse Position, data is missing!");
			}

			return StreamPosition.FromInt64(int.Parse(checkPoint));
		}
	}
}
