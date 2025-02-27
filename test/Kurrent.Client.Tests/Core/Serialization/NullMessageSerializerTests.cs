using System.Text;
using EventStore.Client;
using Kurrent.Client.Core.Serialization;

namespace Kurrent.Client.Tests.Core.Serialization;

public class NullMessageSerializerTests {
	[Fact]
	public void Serialize_ThrowsException() {
		// Given
		var serializer = NullMessageSerializer.Instance;
		var message    = Message.From(new object());
		var context    = new MessageSerializationContext("test-stream", ContentType.Json);

		// When & Assert
		Assert.Throws<InvalidOperationException>(() => serializer.Serialize(message, context));
	}

	[Fact]
	public void TryDeserialize_ReturnsFalse() {
		// Given
		var serializer = NullMessageSerializer.Instance;
		var eventRecord = CreateTestEventRecord();

		// When
		var result = serializer.TryDeserialize(eventRecord, out var message);

		// Then
		Assert.False(result);
		Assert.Null(message);
	}

	static EventRecord CreateTestEventRecord() =>
		new(
			Uuid.NewUuid().ToString(),
			Uuid.NewUuid(),
			StreamPosition.FromInt64(0), 
			new Position(1, 1),
			new Dictionary<string, string> {
				{ Constants.Metadata.Type, "test-event" },
				{ Constants.Metadata.Created, DateTime.UtcNow.ToTicksSinceEpoch().ToString() },
				{ Constants.Metadata.ContentType, Constants.Metadata.ContentTypes.ApplicationJson }
			},
			"""{"x":1}"""u8.ToArray(),
			"""{"x":2}"""u8.ToArray()
		);
}
