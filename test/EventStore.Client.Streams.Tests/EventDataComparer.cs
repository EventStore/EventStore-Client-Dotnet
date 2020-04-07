using System.Text;

namespace EventStore.Client {
	internal static class EventDataComparer {
		public static bool Equal(EventData expected, EventRecord actual) {
			if (expected.EventId != actual.EventId)
				return false;

			if (expected.Type != actual.EventType)
				return false;

			var expectedDataString = Encoding.UTF8.GetString(expected.Data.Span);
			var expectedMetadataString = Encoding.UTF8.GetString(expected.Metadata.Span);

			var actualDataString = Encoding.UTF8.GetString(actual.Data.Span);
			var actualMetadataDataString = Encoding.UTF8.GetString(actual.Metadata.Span);

			return expectedDataString == actualDataString && expectedMetadataString == actualMetadataDataString;
		}

		public static bool Equal(EventData[] expected, EventRecord[] actual) {
			if (expected.Length != actual.Length)
				return false;

			for (var i = 0; i < expected.Length; i++) {
				if (!Equal(expected[i], actual[i]))
					return false;
			}

			return true;
		}
	}
}
