using EventStore.Client;
using EventStore.Client.Serialization;
using Kurrent.Client.Tests.Streams.Serialization;

namespace Kurrent.Client.Core.Serialization;

using static Constants.Metadata.ContentTypes;

public enum ContentType {
	Json = 1,

	// Protobuf  = 2,
	// Avro      = 3,
	Bytes = 4
}

public static class ContentTypeExtensions {
	public static ContentType FromMessageContentType(string contentType) =>
		contentType == ApplicationJson
			? ContentType.Json
			: ContentType.Bytes;

	public static string ToMessageContentType(this ContentType contentType) =>
		contentType switch {
			ContentType.Json  => ApplicationJson,
			ContentType.Bytes => ApplicationOctetStream,
			_                 => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, null)
		};
}

public class SchemaRegistry(
	IDictionary<ContentType, ISerializer> serializers,
	IMessageTypeNamingStrategy messageTypeNamingStrategy
) {
	public IMessageTypeNamingStrategy MessageTypeNamingStrategy { get; } = messageTypeNamingStrategy;

	public ISerializer GetSerializer(ContentType schemaType) =>
		serializers[schemaType];

	public static SchemaRegistry From(KurrentClientSerializationSettings settings) {
		var messageTypeRegistry = new MessageTypeRegistry();
		messageTypeRegistry.AddTypes(settings.MessageTypeMap);
		
		var messageTypeResolutionStrategy = new MessageTypeNamingStrategyWrapper(
			messageTypeRegistry,
			settings.MessageTypeResolutionStrategy ?? new DefaultMessageTypeNamingStrategy()
		);

		var serializers = new Dictionary<ContentType, ISerializer> {
			{
				ContentType.Json,
				settings.JsonSerializer ?? new SystemTextJsonSerializer()
			}, {
				ContentType.Bytes,
				settings.BytesSerializer ?? new SystemTextJsonSerializer()
			}
		};

		return new SchemaRegistry(serializers, messageTypeResolutionStrategy);
	}
}
