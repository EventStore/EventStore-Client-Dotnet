using System.Text;

namespace EventStore.Client {
	internal static class EventDataComparer {
		public static bool Equal(EventData expected, EventRecord actual) {
			if (expected.EventId != actual.EventId)
				return false;

			if (expected.Type != actual.EventType)
				return false;

			var expectedDataString = Encoding.UTF8.GetString(expected.Data.ToArray());
			var expectedMetadataString = Encoding.UTF8.GetString(expected.Metadata.ToArray());

			var actualDataString = Encoding.UTF8.GetString(actual.Data.ToArray());
			var actualMetadataDataString = Encoding.UTF8.GetString(actual.Metadata.ToArray());

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
