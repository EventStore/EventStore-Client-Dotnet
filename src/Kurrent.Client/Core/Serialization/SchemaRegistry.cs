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

// TODO: We need to discuss how to include the full Schema Registry code here
public class SchemaRegistry(
	IDictionary<ContentType, ISerializer> serializers,
	IMessageTypeResolutionStrategy messageTypeResolutionStrategy
) {
	public IMessageTypeResolutionStrategy MessageTypeResolutionStrategy { get; } = messageTypeResolutionStrategy;

	public ISerializer GetSerializer(ContentType schemaType) =>
		serializers[schemaType];

	public static SchemaRegistry From(KurrentClientSerializationSettings settings) {
		var messageTypeResolutionStrategy = new MessageTypeResolutionStrategyWrapper(
			MessageTypeMapper.Instance,
			settings.MessageTypeResolutionStrategy ?? new DefaultMessageTypeResolutionStrategy()
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
